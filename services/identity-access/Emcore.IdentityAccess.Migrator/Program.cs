using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Dapper;

namespace Emcore.IdentityAccess.Migrator;

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

        var connectionString = config.GetConnectionString("IdentityDatabase") ?? config["ConnectionStrings__IdentityDatabase"];

        bool isList = args.Contains("--list");
        bool isValidate = args.Contains("--validate");
        bool isDryRun = args.Contains("--dry-run");
        bool isApply = args.Contains("--apply");

        if (!isList && !isValidate && !isDryRun && !isApply)
        {
            Console.WriteLine("Usage: Emcore.IdentityAccess.Migrator [ --list | --validate | --dry-run | --apply ]");
            return 0;
        }

        var scripts = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Migrations", "Versioned"), "*.sql")
            .OrderBy(f => f)
            .ToList();

        // If no connection string is provided, or if offline flags are passed, we print offline metadata.
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("dummy"))
        {
            if (isList)
            {
                foreach (var script in scripts)
                {
                    Console.WriteLine($"[PENDING] {Path.GetFileName(script)}");
                }
                return 0;
            }
            if (isValidate)
            {
                foreach (var script in scripts)
                {
                    var content = await File.ReadAllTextAsync(script);
                    Console.WriteLine($"[VALID] {Path.GetFileName(script)} (Checksum: {ComputeChecksum(content)})");
                }
                return 0;
            }
            if (isDryRun)
            {
                foreach (var script in scripts)
                {
                    Console.WriteLine($"Will apply {Path.GetFileName(script)}...");
                }
                return 0;
            }
            Console.Error.WriteLine("Error: Connection string not provided for --apply.");
            return 1;
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await EnsureHistoryTableAsync(connection);

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
