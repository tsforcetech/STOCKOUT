$ErrorActionPreference = "Stop"
$dir = "c:\DEV\API PROJECT\STOCKOUT"

New-Item -Path "$dir\gateways" -ItemType Directory -Force | Out-Null
New-Item -Path "$dir\orchestration" -ItemType Directory -Force | Out-Null

$gateways = @(
    @{Name="Emcore.ApiGateway"; Port=7000; Type="web"},
    @{Name="Emcore.PublicBff"; Port=7010; Type="web"},
    @{Name="Emcore.PortalBff"; Port=7020; Type="web"},
    @{Name="Emcore.McpGateway"; Port=7030; Type="web"},
    @{Name="Emcore.RealtimeGateway"; Port=7040; Type="web"}
)

foreach ($g in $gateways) {
    $gName = $g.Name
    $gDir = "$dir\gateways\$gName"
    
    dotnet new $g.Type -n $gName -o $gDir --force
    dotnet sln "$dir\Emcore.Platform.slnx" add "$gDir\$gName.csproj"
    
    # Clear Program.cs to use minimal API and map standard health/version endpoints
    $program = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
// Additional Gateway registrations

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));
app.MapGet("/api/v1/system/version", () => Results.Ok(new { ServiceName = "$gName", Version = "0.1.0", Environment = builder.Environment.EnvironmentName }));

app.Run();
"@
    Set-Content -Path "$gDir\Program.cs" -Value $program -Encoding utf8
    
    # Dockerfile
    $docker = @"
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "gateways/$gName/$gName.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "$gName.dll"]
"@
    Set-Content -Path "$gDir\Dockerfile" -Value $docker -Encoding utf8
    
    # Add YARP to ApiGateway
    if ($gName -eq "Emcore.ApiGateway") {
        dotnet add "$gDir\$gName.csproj" package Yarp.ReverseProxy
    }
}

# Orchestration - AppHost and ServiceDefaults
# We use standard .NET Aspire templates if possible, or classlib/worker for ServiceDefaults/AppHost
dotnet new aspire.apphost -n Emcore.AppHost -o "$dir\orchestration\Emcore.AppHost" --force
dotnet sln "$dir\Emcore.Platform.slnx" add "$dir\orchestration\Emcore.AppHost\Emcore.AppHost.csproj"

dotnet new classlib -n Emcore.ServiceDefaults -o "$dir\orchestration\Emcore.ServiceDefaults" --force
dotnet sln "$dir\Emcore.Platform.slnx" add "$dir\orchestration\Emcore.ServiceDefaults\Emcore.ServiceDefaults.csproj"
Remove-Item "$dir\orchestration\Emcore.ServiceDefaults\Class1.cs" -Force -ErrorAction SilentlyContinue

# ServiceDefaults OpenTelemetry
$sd = @"
namespace Emcore.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        // Registration for OTel
        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        return app;
    }
}
"@
Set-Content -Path "$dir\orchestration\Emcore.ServiceDefaults\Extensions.cs" -Value $sd -Encoding utf8

dotnet add "$dir\orchestration\Emcore.ServiceDefaults\Emcore.ServiceDefaults.csproj" package OpenTelemetry.Extensions.Hosting
dotnet add "$dir\orchestration\Emcore.ServiceDefaults\Emcore.ServiceDefaults.csproj" package OpenTelemetry.Instrumentation.AspNetCore
dotnet add "$dir\orchestration\Emcore.ServiceDefaults\Emcore.ServiceDefaults.csproj" package OpenTelemetry.Instrumentation.Http
dotnet add "$dir\orchestration\Emcore.ServiceDefaults\Emcore.ServiceDefaults.csproj" package OpenTelemetry.Instrumentation.Runtime
dotnet add "$dir\orchestration\Emcore.ServiceDefaults\Emcore.ServiceDefaults.csproj" package OpenTelemetry.Exporter.OpenTelemetryProtocol

Write-Host "Gateways and Orchestration scaffolded successfully"
