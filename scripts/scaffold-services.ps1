$ErrorActionPreference = "Stop"
$dir = "c:\DEV\API PROJECT\STOCKOUT"

$json = Get-Content "$dir\service-inventory.json" | ConvertFrom-Json

New-Item -Path "$dir\services" -ItemType Directory -Force | Out-Null

foreach ($service in $json.services) {
    $key = $service.serviceKey
    $ns = $service.namespace
    $port = $service.localApiPort
    $dbName = $service.databaseLogicalName
    
    $svcDir = "$dir\services\$key"
    $srcDir = "$svcDir\src"
    $testsDir = "$svcDir\tests"
    $dbDir = "$svcDir\database"
    
    New-Item -Path $srcDir, $testsDir, $dbDir -ItemType Directory -Force | Out-Null
    
    # DB Placeholders
    $dbFolders = @("migrations", "schemas", "tables", "types", "functions", "procedures", "indexes", "seed")
    foreach ($f in $dbFolders) {
        New-Item -Path "$dbDir\$f" -ItemType Directory -Force | Out-Null
        New-Item -Path "$dbDir\$f\.gitkeep" -ItemType File -Force | Out-Null
    }
    
    # Generate 8 projects
    $projects = @(
        @{Name="Domain"; Path="$srcDir"; Type="classlib"},
        @{Name="Application"; Path="$srcDir"; Type="classlib"},
        @{Name="Infrastructure"; Path="$srcDir"; Type="classlib"},
        @{Name="Contracts"; Path="$srcDir"; Type="classlib"},
        @{Name="Api"; Path="$srcDir"; Type="webapi"},
        @{Name="Worker"; Path="$srcDir"; Type="worker"},
        @{Name="UnitTests"; Path="$testsDir"; Type="xunit"},
        @{Name="ArchitectureTests"; Path="$testsDir"; Type="xunit"},
        @{Name="IntegrationTests"; Path="$testsDir"; Type="xunit"}
    )
    
    foreach ($p in $projects) {
        $pName = "$ns.$($p.Name)"
        $pDir = "$($p.Path)\$pName"
        dotnet new $p.Type -n $pName -o $pDir --force
        Remove-Item "$pDir\Class1.cs" -Force -ErrorAction SilentlyContinue
        Remove-Item "$pDir\WeatherForecast.cs" -Force -ErrorAction SilentlyContinue
        
        dotnet sln "$dir\Emcore.Platform.slnx" add "$pDir\$pName.csproj"

        # Remove Version attributes from PackageReferences (due to CPM)
        $xml = [xml](Get-Content "$pDir\$pName.csproj")
        $modified = $false
        if ($xml.Project.ItemGroup) {
            foreach ($ig in @($xml.Project.ItemGroup)) {
                if ($ig.PackageReference) {
                    foreach ($pkg in @($ig.PackageReference)) {
                        if ($null -ne $pkg.Version) {
                            $pkg.RemoveAttribute("Version")
                            $modified = $true
                        }
                    }
                }
            }
        }
        if ($modified) {
            $xml.Save("$pDir\$pName.csproj")
        }
    }

    # Setup project references according to rules
    dotnet add "$srcDir\$ns.Application\$ns.Application.csproj" reference "$srcDir\$ns.Domain\$ns.Domain.csproj"
    
    dotnet add "$srcDir\$ns.Infrastructure\$ns.Infrastructure.csproj" reference "$srcDir\$ns.Domain\$ns.Domain.csproj"
    dotnet add "$srcDir\$ns.Infrastructure\$ns.Infrastructure.csproj" reference "$srcDir\$ns.Application\$ns.Application.csproj"
    
    dotnet add "$srcDir\$ns.Api\$ns.Api.csproj" reference "$srcDir\$ns.Application\$ns.Application.csproj"
    dotnet add "$srcDir\$ns.Api\$ns.Api.csproj" reference "$srcDir\$ns.Contracts\$ns.Contracts.csproj"
    dotnet add "$srcDir\$ns.Api\$ns.Api.csproj" reference "$srcDir\$ns.Infrastructure\$ns.Infrastructure.csproj"
    
    dotnet add "$srcDir\$ns.Worker\$ns.Worker.csproj" reference "$srcDir\$ns.Application\$ns.Application.csproj"
    dotnet add "$srcDir\$ns.Worker\$ns.Worker.csproj" reference "$srcDir\$ns.Contracts\$ns.Contracts.csproj"
    dotnet add "$srcDir\$ns.Worker\$ns.Worker.csproj" reference "$srcDir\$ns.Infrastructure\$ns.Infrastructure.csproj"
    
    # Test references
    dotnet add "$testsDir\$ns.UnitTests\$ns.UnitTests.csproj" reference "$srcDir\$ns.Domain\$ns.Domain.csproj"
    dotnet add "$testsDir\$ns.UnitTests\$ns.UnitTests.csproj" reference "$srcDir\$ns.Application\$ns.Application.csproj"
    
    dotnet add "$testsDir\$ns.ArchitectureTests\$ns.ArchitectureTests.csproj" reference "$srcDir\$ns.Domain\$ns.Domain.csproj"
    dotnet add "$testsDir\$ns.ArchitectureTests\$ns.ArchitectureTests.csproj" reference "$srcDir\$ns.Application\$ns.Application.csproj"
    dotnet add "$testsDir\$ns.ArchitectureTests\$ns.ArchitectureTests.csproj" reference "$srcDir\$ns.Infrastructure\$ns.Infrastructure.csproj"
    dotnet add "$testsDir\$ns.ArchitectureTests\$ns.ArchitectureTests.csproj" reference "$srcDir\$ns.Contracts\$ns.Contracts.csproj"
    dotnet add "$testsDir\$ns.ArchitectureTests\$ns.ArchitectureTests.csproj" reference "$srcDir\$ns.Api\$ns.Api.csproj"
    dotnet add "$testsDir\$ns.ArchitectureTests\$ns.ArchitectureTests.csproj" reference "$srcDir\$ns.Worker\$ns.Worker.csproj"
    
    # Add building blocks references
    dotnet add "$srcDir\$ns.Application\$ns.Application.csproj" reference "$dir\building-blocks\Emcore.BuildingBlocks.Core\Emcore.BuildingBlocks.Core.csproj"
    dotnet add "$srcDir\$ns.Infrastructure\$ns.Infrastructure.csproj" reference "$dir\building-blocks\Emcore.BuildingBlocks.Data\Emcore.BuildingBlocks.Data.csproj"
    dotnet add "$srcDir\$ns.Infrastructure\$ns.Infrastructure.csproj" reference "$dir\building-blocks\Emcore.BuildingBlocks.Messaging\Emcore.BuildingBlocks.Messaging.csproj"
    dotnet add "$srcDir\$ns.Infrastructure\$ns.Infrastructure.csproj" reference "$dir\building-blocks\Emcore.BuildingBlocks.Caching\Emcore.BuildingBlocks.Caching.csproj"
    dotnet add "$srcDir\$ns.Api\$ns.Api.csproj" reference "$dir\building-blocks\Emcore.BuildingBlocks.Api\Emcore.BuildingBlocks.Api.csproj"
    
    # Create required classes/abstractions per service
    
    # Domain
    New-Item -Path "$srcDir\$ns.Domain\Abstractions" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Domain\Entities" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Domain\ValueObjects" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Domain\Enums" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Domain\Events" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Domain\Errors" -ItemType Directory -Force | Out-Null
    
    Set-Content -Path "$srcDir\$ns.Domain\AssemblyReference.cs" -Value "namespace $ns.Domain; public static class AssemblyReference { public static readonly System.Reflection.Assembly Assembly = typeof(AssemblyReference).Assembly; }" -Encoding utf8
    
    # Application
    New-Item -Path "$srcDir\$ns.Application\Abstractions" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Application\Behaviors" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Application\Commands" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Application\Queries" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Application\Validation" -ItemType Directory -Force | Out-Null
    
    Set-Content -Path "$srcDir\$ns.Application\AssemblyReference.cs" -Value "namespace $ns.Application; public static class AssemblyReference { public static readonly System.Reflection.Assembly Assembly = typeof(AssemblyReference).Assembly; }" -Encoding utf8
    Set-Content -Path "$srcDir\$ns.Application\DependencyInjection.cs" -Value "namespace $ns.Application; public static class DependencyInjection { public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddApplication(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) => services; }" -Encoding utf8
    $appMarkerName = "I$($key.Replace('-',''))ApplicationMarker"
    Set-Content -Path "$srcDir\$ns.Application\$appMarkerName.cs" -Value "namespace $ns.Application; public interface $appMarkerName { }" -Encoding utf8
    
    # Infrastructure
    New-Item -Path "$srcDir\$ns.Infrastructure\Persistence\Connections" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Infrastructure\Persistence\StoredProcedures" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Infrastructure\Persistence\Repositories" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Infrastructure\Messaging" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Infrastructure\Caching" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Infrastructure\Search" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Infrastructure\Storage" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Infrastructure\Integrations" -ItemType Directory -Force | Out-Null
    
    Set-Content -Path "$srcDir\$ns.Infrastructure\AssemblyReference.cs" -Value "namespace $ns.Infrastructure; public static class AssemblyReference { public static readonly System.Reflection.Assembly Assembly = typeof(AssemblyReference).Assembly; }" -Encoding utf8
    Set-Content -Path "$srcDir\$ns.Infrastructure\DependencyInjection.cs" -Value "namespace $ns.Infrastructure; public static class DependencyInjection { public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddInfrastructure(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration) => services; }" -Encoding utf8
    $svcName = $ns.Split('.')[1]
    Set-Content -Path "$srcDir\$ns.Infrastructure\$($svcName)InfrastructureOptions.cs" -Value "namespace $ns.Infrastructure; public class $($svcName)InfrastructureOptions { }" -Encoding utf8
    Set-Content -Path "$srcDir\$ns.Infrastructure\Persistence\README.md" -Value "# Persistence" -Encoding utf8
    Set-Content -Path "$srcDir\$ns.Infrastructure\Messaging\README.md" -Value "# Messaging" -Encoding utf8
    Set-Content -Path "$srcDir\$ns.Infrastructure\Integrations\README.md" -Value "# Integrations" -Encoding utf8
    
    # Contracts
    New-Item -Path "$srcDir\$ns.Contracts\Api" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Contracts\Events" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Contracts\Webhooks" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Contracts\Realtime" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Contracts\Errors" -ItemType Directory -Force | Out-Null
    
    Set-Content -Path "$srcDir\$ns.Contracts\AssemblyReference.cs" -Value "namespace $ns.Contracts; public static class AssemblyReference { public static readonly System.Reflection.Assembly Assembly = typeof(AssemblyReference).Assembly; }" -Encoding utf8
    Set-Content -Path "$srcDir\$ns.Contracts\SystemVersionResponse.cs" -Value "namespace $ns.Contracts; public record SystemVersionResponse(string ServiceName, string Version, string Environment);" -Encoding utf8
    Set-Content -Path "$srcDir\$ns.Contracts\I$($svcName)ContractsMarker.cs" -Value "namespace $ns.Contracts; public interface I$($svcName)ContractsMarker { }" -Encoding utf8
    
    # Api
    New-Item -Path "$srcDir\$ns.Api\Endpoints\System" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Api\Middleware" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Api\OpenApi" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Api\Configuration" -ItemType Directory -Force | Out-Null

    # Appsettings (Api)
    $settingsJson = @{
        Service = @{
            Name = "emcore-$key-api"
            Version = "0.1.0"
            Environment = "Local"
        }
        Database = @{
            Enabled = $false
            ConnectionString = $null
            CommandTimeoutSeconds = 30
        }
        Messaging = @{
            Enabled = $false
            Host = "localhost"
            VirtualHost = "/"
            Username = $null
            Password = $null
        }
        Redis = @{
            Enabled = $false
            ConnectionString = $null
        }
        OpenSearch = @{
            Enabled = $false
            Endpoint = $null
        }
        ObjectStorage = @{
            Enabled = $false
            ServiceUrl = $null
            Bucket = $null
            AccessKey = $null
            SecretKey = $null
        }
        Telemetry = @{
            Enabled = $true
            OtlpEndpoint = $null
        }
    } | ConvertTo-Json -Depth 10

    Set-Content -Path "$srcDir\$ns.Api\appsettings.json" -Value $settingsJson -Encoding utf8
    Set-Content -Path "$srcDir\$ns.Api\appsettings.Local.json" -Value "{}" -Encoding utf8
    Set-Content -Path "$srcDir\$ns.Api\appsettings.Development.json" -Value "{}" -Encoding utf8
    Set-Content -Path "$srcDir\$ns.Api\appsettings.Integration.json" -Value "{}" -Encoding utf8
    
    # Api Program.cs (Health endpoints only)
    $program = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));
app.MapGet("/api/v1/system/version", () => Results.Ok(new $ns.Contracts.SystemVersionResponse("emcore-$key-api", "0.1.0", builder.Environment.EnvironmentName)));

app.Run();
"@
    Set-Content -Path "$srcDir\$ns.Api\Program.cs" -Value $program -Encoding utf8

    # Worker
    New-Item -Path "$srcDir\$ns.Worker\HostedServices" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Worker\Consumers" -ItemType Directory -Force | Out-Null
    New-Item -Path "$srcDir\$ns.Worker\Configuration" -ItemType Directory -Force | Out-Null

    # Appsettings (Worker)
    $settingsJsonWorker = $settingsJson.Replace("-api", "-worker")
    Set-Content -Path "$srcDir\$ns.Worker\appsettings.json" -Value $settingsJsonWorker -Encoding utf8
    Set-Content -Path "$srcDir\$ns.Worker\appsettings.Local.json" -Value "{}" -Encoding utf8
    Set-Content -Path "$srcDir\$ns.Worker\appsettings.Development.json" -Value "{}" -Encoding utf8
    Set-Content -Path "$srcDir\$ns.Worker\appsettings.Integration.json" -Value "{}" -Encoding utf8

    # Worker Program.cs
    $workerProgram = @"
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices(services =>
{
    services.AddHostedService<HeartbeatService>();
});

var host = builder.Build();
await host.RunAsync();

public class HeartbeatService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken);
        }
    }
}
"@
    Set-Content -Path "$srcDir\$ns.Worker\Program.cs" -Value $workerProgram -Encoding utf8
    
    # Manifest
    $manifestJson = @{
        serviceKey = $key
        namespace = $ns
        apiProject = "$ns.Api"
        workerProject = "$ns.Worker"
        localApiPort = $port
        databaseLogicalName = $dbName
        databaseProvisioning = "DEFERRED"
        storedProcedures = "DEFERRED"
        messagingTopology = "DEFERRED"
        deploymentTarget = "AWS_ECS_FARGATE"
    } | ConvertTo-Json
    Set-Content -Path "$svcDir\service-manifest.json" -Value $manifestJson -Encoding utf8
    
    # Dockerfiles
    $apiDocker = @"
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "services/$key/src/$ns.Api/$ns.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "$ns.Api.dll"]
"@
    $workerDocker = @"
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "services/$key/src/$ns.Worker/$ns.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "$ns.Worker.dll"]
"@
    Set-Content -Path "$svcDir\Dockerfile.Api" -Value $apiDocker -Encoding utf8
    Set-Content -Path "$svcDir\Dockerfile.Worker" -Value $workerDocker -Encoding utf8
    Set-Content -Path "$svcDir\README.md" -Value "# $ns" -Encoding utf8
    
    Write-Host "Scaffolded $ns"
}

