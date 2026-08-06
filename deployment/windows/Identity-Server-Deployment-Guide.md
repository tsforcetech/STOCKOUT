# EMCORE Identity & Access Windows Server Deployment Guide

This guide describes the operational deployment procedure for the **Emcore.IdentityAccess** vertical slice on Windows Server running IIS and Windows Services.

## 1. System Requirements
- **Operating System**: Windows Server 2022 or Windows Server 2019
- **Web Server**: Internet Information Services (IIS) 10+ with ASP.NET Core Module v2 (ANCM)
- **Runtime**: .NET 10.0 Hosting Bundle and .NET 10.0 Windows Desktop/Runtime
- **Database**: Microsoft SQL Server 2022 / Azure SQL with automated migration permissions

## 2. Deployment Architecture
The deployment targets two physical execution units:
1. **API Host (`Emcore.IdentityAccess.Api.exe`)**: Hosted inside IIS using standard In-Process hosting via `web.config`. Listens on `http://127.0.0.1:5101/` for upstream reverse proxy routing from `Emcore.ApiGateway`.
2. **Background Relay Worker (`Emcore.IdentityAccess.Worker.exe`)**: Installed as an isolated background Windows Service running under `NT AUTHORITY\NetworkService` with automatic failure restart policies.

```mermaid
graph TD
    GW["EMCORE API Gateway (YARP)"] -->|"/api/v1/auth/* & /api/v1/identity/*"| IIS["IIS: EmcoreIdentityApi (5101)"]
    IIS -->|Stored Procedures| SQL[(EMCORE_IDENTITY_DB)]
    Worker["Windows Service: EmcoreIdentityRelayWorker"] -->|Poll Outbox (PR_IDENTITY_GET_PENDING_OUTBOX)| SQL
    Worker -->|Publish Events| RMQ["RabbitMQ / Broker"]
    Worker -->|Hourly Cleanup| SQL
```

## 3. Automated Installation via PowerShell
An automated deployment script is provided at `deployment/windows/Deploy-IdentityServices.ps1`.

### Step-by-Step Execution:
1. Publish release binaries using the DotNet CLI:
   ```powershell
   dotnet publish services/identity-access/src/Emcore.IdentityAccess.Api/Emcore.IdentityAccess.Api.csproj -c Release -o C:\Inetpub\Emcore\Identity\api --self-contained false
   dotnet publish services/identity-access/src/Emcore.IdentityAccess.Worker/Emcore.IdentityAccess.Worker.csproj -c Release -o C:\Inetpub\Emcore\Identity\worker --self-contained false
   ```
2. Open an elevated PowerShell session as Administrator and execute:
   ```powershell
   & "C:\DEV\API PROJECT\STOCKOUT\deployment\windows\Deploy-IdentityServices.ps1" -DeploymentPath "C:\Inetpub\Emcore\Identity" -ApiPort 5101
   ```
3. Verify Application Pool and Service status:
   ```powershell
   Get-WebAppPoolState -Name "EmcoreIdentityAppPool"
   Get-Service -Name "EmcoreIdentityRelayWorker"
   ```

## 4. Configuration & Security Hardening
- **Connection Strings & Secrets**: Configure SQL Server credentials securely using environment variables or Windows Protected Machine Secret store (`UserSecrets` / Azure KeyVault). Never store plaintext passwords in shared configuration files.
- **Strict Startup Secret Validation**: In Production (`ASPNETCORE_ENVIRONMENT=Production`), the application enforces mandatory startup secret checks. If required key material (`Jwt:SigningKey` or `Otp:HmacPepper`) is missing or invalid, the service fails to boot immediately (`InvalidOperationException`).
- **Standardized API Error Responses (RFC 7807)**: All API error returns (including unhandled exceptions, validation failures, and authorization checks) are uniformly serialized as `application/problem+json` Problem Details without exposing sensitive internal stack traces.
- **IIS Security & Headers**: The provided `web.config` enforces strict request filtering (maximum 10MB payload size), applies defensive security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`), and drops revealing response headers (`X-Powered-By`).
- **Service Failure Recovery**: The Windows Service is explicitly configured via `sc.exe failure` to automatically restart in case of unexpected SQL connection loss or system faults after 5, 10, and 30 seconds.

## 5. Post-Deployment Verification
Run automated validation against live endpoints:
```powershell
Invoke-RestMethod -Uri "http://127.0.0.1:5101/health/ready" -Method Get
Invoke-RestMethod -Uri "http://127.0.0.1:5101/.well-known/jwks.json" -Method Get
```
Both requests must return HTTP `200 OK`.
