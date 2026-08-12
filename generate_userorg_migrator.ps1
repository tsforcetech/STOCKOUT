$MigratorPath = "c:\DEV\API PROJECT\STOCKOUT\services\user-organization\Emcore.UserOrganization.Migrator"
$MigrationsPath = "$MigratorPath\Migrations"
$ManualPath = "$MigrationsPath\Manual"
$VersionedPath = "$MigrationsPath\Versioned"

New-Item -Path $MigratorPath -ItemType Directory -Force | Out-Null
New-Item -Path $ManualPath -ItemType Directory -Force | Out-Null
New-Item -Path $VersionedPath -ItemType Directory -Force | Out-Null

$csproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.1" />
    <PackageReference Include="Dapper" Version="2.1.35" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.CommandLine" Version="9.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <None Update="Migrations\**\*.sql">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
"@
Set-Content -Path "$MigratorPath\Emcore.UserOrganization.Migrator.csproj" -Value $csproj

$program = @"
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace Emcore.UserOrganization.Migrator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var connectionString = config.GetConnectionString("OrganizationDatabase") ?? config["ConnectionStrings__OrganizationDatabase"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine("Error: Connection string not provided.");
            return 1;
        }

        bool isList = args.Contains("--list");
        bool isValidate = args.Contains("--validate");
        bool isDryRun = args.Contains("--dry-run");
        bool isApply = args.Contains("--apply");

        if (!isList && !isValidate && !isDryRun && !isApply)
        {
            Console.WriteLine("Usage: Emcore.UserOrganization.Migrator [ --list | --validate | --dry-run | --apply ]");
            return 0;
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await EnsureHistoryTableAsync(connection);

            var scripts = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Migrations", "Versioned"), "*.sql")
                .OrderBy(f => f)
                .ToList();

            foreach (var script in scripts)
            {
                var scriptName = Path.GetFileName(script);
                var scriptContent = await File.ReadAllTextAsync(script);
                var checksum = ComputeChecksum(scriptContent);

                var history = await connection.QuerySingleOrDefaultAsync<MigrationHistory>(
                    "SELECT * FROM dbo.__EMCORE_MIGRATION_HISTORY WHERE MIGRATION_NAME = @Name",
                    new { Name = scriptName });

                if (history != null)
                {
                    if (history.SCRIPT_CHECKSUM != checksum)
                    {
                        Console.Error.WriteLine($"Checksum mismatch for {scriptName}. Expected {history.SCRIPT_CHECKSUM}, got {checksum}.");
                        return 1;
                    }
                    if (isList) Console.WriteLine($"[APPLIED] {scriptName}");
                    continue;
                }

                if (isList)
                {
                    Console.WriteLine($"[PENDING] {scriptName}");
                    continue;
                }

                if (isValidate)
                {
                    Console.WriteLine($"[VALID] {scriptName} (Checksum: {checksum})");
                    continue;
                }

                Console.WriteLine($"{(isDryRun ? "Will apply" : "Applying")} {scriptName}...");
                if (!isDryRun)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    bool success = true;
                    string? errorRef = null;
                    using var transaction = connection.BeginTransaction();
                    try
                    {
                        var batches = scriptContent.Split(new[] { "\nGO", "\r\nGO", " GO\r\n", " GO\n" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach(var batch in batches) {
                            if (string.IsNullOrWhiteSpace(batch)) continue;
                            await connection.ExecuteAsync(batch, transaction: transaction);
                        }
                        
                        await connection.ExecuteAsync(
                            "INSERT INTO dbo.__EMCORE_MIGRATION_HISTORY (MIGRATION_ID, MIGRATION_NAME, SCRIPT_CHECKSUM, APPLIED_AT_UTC, APPLIED_BY, APPLICATION_VERSION, EXECUTION_DURATION_MS, SUCCESS_YN) VALUES (@Id, @Name, @Checksum, @AppliedAt, @AppliedBy, @AppVersion, @Duration, @Success)",
                            new {
                                Id = Guid.NewGuid().ToString(),
                                Name = scriptName,
                                Checksum = checksum,
                                AppliedAt = DateTime.UtcNow,
                                AppliedBy = Environment.UserName,
                                AppVersion = "1.0",
                                Duration = watch.ElapsedMilliseconds,
                                Success = true
                            }, transaction);

                        transaction.Commit();
                        Console.WriteLine($"Applied {scriptName} in {watch.ElapsedMilliseconds}ms.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.Error.WriteLine($"Failed to apply {scriptName}: {ex.Message}");
                        success = false;
                        errorRef = ex.Message;
                        
                        // Try to log failure
                        try {
                            await connection.ExecuteAsync(
                                "INSERT INTO dbo.__EMCORE_MIGRATION_HISTORY (MIGRATION_ID, MIGRATION_NAME, SCRIPT_CHECKSUM, APPLIED_AT_UTC, APPLIED_BY, APPLICATION_VERSION, EXECUTION_DURATION_MS, SUCCESS_YN, ERROR_REFERENCE) VALUES (@Id, @Name, @Checksum, @AppliedAt, @AppliedBy, @AppVersion, @Duration, @Success, @ErrorRef)",
                                new {
                                    Id = Guid.NewGuid().ToString(),
                                    Name = scriptName,
                                    Checksum = checksum,
                                    AppliedAt = DateTime.UtcNow,
                                    AppliedBy = Environment.UserName,
                                    AppVersion = "1.0",
                                    Duration = watch.ElapsedMilliseconds,
                                    Success = false,
                                    ErrorRef = errorRef
                                });
                        } catch {}
                        return 1;
                    }
                }
            }
            Console.WriteLine("Done.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal Error: {ex.Message}");
            return 1;
        }
    }

    static async Task EnsureHistoryTableAsync(SqlConnection connection)
    {
        var sql = @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[__EMCORE_MIGRATION_HISTORY]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[__EMCORE_MIGRATION_HISTORY](
        [MIGRATION_ID] [nvarchar](50) NOT NULL,
        [MIGRATION_NAME] [nvarchar](255) NOT NULL,
        [SCRIPT_CHECKSUM] [nvarchar](100) NOT NULL,
        [APPLIED_AT_UTC] [datetime2](7) NOT NULL,
        [APPLIED_BY] [nvarchar](100) NOT NULL,
        [APPLICATION_VERSION] [nvarchar](50) NOT NULL,
        [EXECUTION_DURATION_MS] [bigint] NOT NULL,
        [SUCCESS_YN] [bit] NOT NULL,
        [ERROR_REFERENCE] [nvarchar](max) NULL,
     CONSTRAINT [PK___EMCORE_MIGRATION_HISTORY] PRIMARY KEY CLUSTERED ([MIGRATION_ID] ASC)
    )
END";
        await connection.ExecuteAsync(sql);
    }

    static string ComputeChecksum(string content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash);
    }

    class MigrationHistory
    {
        public string MIGRATION_ID { get; set; } = "";
        public string MIGRATION_NAME { get; set; } = "";
        public string SCRIPT_CHECKSUM { get; set; } = "";
        public DateTime APPLIED_AT_UTC { get; set; }
        public string APPLIED_BY { get; set; } = "";
        public string APPLICATION_VERSION { get; set; } = "";
        public long EXECUTION_DURATION_MS { get; set; }
        public bool SUCCESS_YN { get; set; }
        public string? ERROR_REFERENCE { get; set; }
    }
}
"@
Set-Content -Path "$MigratorPath\Program.cs" -Value $program

$sql000 = @"
-- Template: Provision Identity Database
-- REPLACE ${DB_NAME}
CREATE DATABASE [${DB_NAME}];
GO
"@
Set-Content -Path "$ManualPath\000_Provision_Identity_Database.template.sql" -Value $sql000

$sql090 = @"
-- Template: Permissions
-- REPLACE ${DB_NAME}, ${APP_USER}
USE [${DB_NAME}];
GO
CREATE USER [${APP_USER}] FOR LOGIN [${APP_USER}];
ALTER ROLE [db_datareader] ADD MEMBER [${APP_USER}];
ALTER ROLE [db_datawriter] ADD MEMBER [${APP_USER}];
GRANT EXECUTE TO [${APP_USER}];
GO
"@
Set-Content -Path "$ManualPath\090_Identity_Database_Permissions.template.sql" -Value $sql090

$sql001 = @"
CREATE TABLE dbo.USER_ACCOUNT (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UlidId VARCHAR(26) NOT NULL,
    Status VARCHAR(50) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RowVersion ROWVERSION
);

CREATE TABLE dbo.USER_CREDENTIAL (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RowVersion ROWVERSION
);

CREATE TABLE dbo.USER_EMAIL (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    EmailAddress VARCHAR(255) NOT NULL,
    IsVerified BIT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RowVersion ROWVERSION
);

CREATE TABLE dbo.USER_MOBILE (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    MobileNumber VARCHAR(50) NOT NULL,
    IsVerified BIT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RowVersion ROWVERSION
);

CREATE TABLE dbo.SERVICE_CLIENT (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    ClientId VARCHAR(255) NOT NULL,
    ClientSecretHash VARCHAR(255) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RowVersion ROWVERSION
);
GO
"@
Set-Content -Path "$VersionedPath\001_Create_Identity_Core_Tables.sql" -Value $sql001

$sql002 = @"
CREATE TABLE dbo.ACCOUNT_VERIFICATION (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    VerificationCode VARCHAR(100) NOT NULL,
    ExpiresAtUtc DATETIME2 NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RowVersion ROWVERSION
);

CREATE TABLE dbo.ACCOUNT_RECOVERY (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    RecoveryToken VARCHAR(255) NOT NULL,
    ExpiresAtUtc DATETIME2 NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RowVersion ROWVERSION
);
GO
"@
Set-Content -Path "$VersionedPath\002_Create_Verification_And_Recovery_Tables.sql" -Value $sql002

$sql003 = @"
CREATE TABLE dbo.USER_SESSION (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    SessionToken VARCHAR(255) NOT NULL,
    ExpiresAtUtc DATETIME2 NOT NULL,
    IsRevoked BIT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RowVersion ROWVERSION
);

CREATE TABLE dbo.REFRESH_TOKEN (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SessionId UNIQUEIDENTIFIER NOT NULL,
    Token VARCHAR(255) NOT NULL,
    ExpiresAtUtc DATETIME2 NOT NULL,
    IsRevoked BIT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RowVersion ROWVERSION
);

CREATE TABLE dbo.LOGIN_ATTEMPT (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    IsSuccess BIT NOT NULL,
    AttemptedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RowVersion ROWVERSION
);

CREATE TABLE dbo.PASSWORD_HISTORY (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RowVersion ROWVERSION
);
GO
"@
Set-Content -Path "$VersionedPath\003_Create_Session_And_Refresh_Token_Tables.sql" -Value $sql003

$sql004 = @"
CREATE TABLE dbo.ORGANIZATION_OUTBOX (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    MessageType VARCHAR(255) NOT NULL,
    Payload NVARCHAR(MAX) NOT NULL,
    IsPublished BIT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RowVersion ROWVERSION
);

CREATE TABLE dbo.ORGANIZATION_INBOX (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    MessageId UNIQUEIDENTIFIER NOT NULL,
    ProcessedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RowVersion ROWVERSION
);

CREATE TABLE dbo.IDEMPOTENCY_REQUEST (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    IdempotencyKey VARCHAR(255) NOT NULL,
    Name VARCHAR(255) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RowVersion ROWVERSION
);
GO
"@
Set-Content -Path "$VersionedPath\004_Create_Outbox_Inbox_And_Idempotency.sql" -Value $sql004

$sql005 = @"
CREATE UNIQUE INDEX IX_USER_ACCOUNT_UlidId ON dbo.USER_ACCOUNT (UlidId);
CREATE UNIQUE INDEX IX_USER_EMAIL_EmailAddress ON dbo.USER_EMAIL (EmailAddress);
CREATE UNIQUE INDEX IX_IDEMPOTENCY_REQUEST_Key ON dbo.IDEMPOTENCY_REQUEST (IdempotencyKey);
GO
"@
Set-Content -Path "$VersionedPath\005_Create_Identity_Indexes_And_Constraints.sql" -Value $sql005

$sql006 = @"
CREATE PROCEDURE dbo.PR_ORGANIZATION_REGISTER_USER AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_CREATE_VERIFICATION AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_VERIFY_ACCOUNT AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_RESEND_VERIFICATION AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_GET_LOGIN_CONTEXT AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_RECORD_LOGIN_ATTEMPT AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_APPLY_LOGIN_FAILURE AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_CLEAR_LOGIN_FAILURE AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_CREATE_SESSION AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_ROTATE_REFRESH_TOKEN AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_REVOKE_SESSION AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_REVOKE_ALL_SESSIONS AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_LIST_SESSIONS AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_CREATE_RECOVERY_REQUEST AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_RESET_PASSWORD AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_GET_PENDING_OUTBOX AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_MARK_OUTBOX_PUBLISHED AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_MARK_OUTBOX_FAILED AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_TRY_RECORD_INBOX AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_BEGIN_IDEMPOTENT_REQUEST AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_COMPLETE_IDEMPOTENT_REQUEST AS BEGIN SET NOCOUNT ON; END;
GO
CREATE PROCEDURE dbo.PR_ORGANIZATION_GET_IDEMPOTENT_RESULT AS BEGIN SET NOCOUNT ON; END;
GO
"@
Set-Content -Path "$VersionedPath\006_Create_Identity_Stored_Procedures.sql" -Value $sql006

$sql007 = @"
CREATE PROCEDURE dbo.PR_ORGANIZATION_CLEANUP_EXPIRED_SECURITY_DATA AS BEGIN SET NOCOUNT ON; END;
GO
"@
Set-Content -Path "$VersionedPath\007_Create_Identity_Cleanup_Procedures.sql" -Value $sql007
