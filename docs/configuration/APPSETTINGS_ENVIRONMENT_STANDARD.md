# EMCORE Appsettings Environment Standard

## Configuration Philosophy

The EMCORE platform enforces a standardized application configuration model that avoids relying on numerous operational environment variables. The system should derive almost all runtime settings from tightly versioned ppsettings.json files based on a single environment selector.

### Only External Selector

The **ONLY** environment variable required to be set externally on the hosting environment is:

ASPNETCORE_ENVIRONMENT

### Supported Values

- Development
- Production

## Loading Model

The standard configuration loading precedence uses default ASP.NET Core behaviors.

### Development Environment
When ASPNETCORE_ENVIRONMENT=Development, the application loads:
1. ppsettings.json
2. ppsettings.Development.json

### Production Environment
When ASPNETCORE_ENVIRONMENT=Production, the application loads:
1. ppsettings.json
2. ppsettings.Production.json

## Deployment Examples

### IIS Example
For server environments like IIS, the environment selector can be provided through a generated web.config:

<environmentVariables>
  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
</environmentVariables>

### Migrator Example
To run the Emcore.IdentityAccess.Migrator manually on a target environment, rely on the ASPNETCORE_ENVIRONMENT variable rather than explicitly passing connection string variables.

**PowerShell (Development):**

$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project Emcore.IdentityAccess.Migrator --validate --dry-run

**PowerShell (Production):**

$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet run --project Emcore.IdentityAccess.Migrator --validate --dry-run

## Secret Handling

Real secrets such as Production database passwords, JWT signing keys, and third-party API credentials MUST NOT be committed to source control.

Instead, the ppsettings.Production.json file committed to the repository should contain safe placeholders.

During deployment, these placeholder files will be overwritten or populated with the actual secrets on the target server.

## Production Fail-Closed Policy

The application must continue to fail securely on startup (fail-closed) if critical Production configuration values are missing or invalid. Do not relax required validation attributes, ValidateOnStart directives, or explicitly required connection string guards simply to permit a process to start without proper configuration.

## Projects Covered

This configuration standard covers all Web APIs, BFFs, Gateways, Workers, and Migrators in the EMCORE repository that execute as host applications, with one strict exception.

### Excluded Projects

- Emcore.ApiGateway: This project is explicitly excluded from these standardization changes to prevent disruption to its existing, working deployment. Its environment handling remains unchanged.
