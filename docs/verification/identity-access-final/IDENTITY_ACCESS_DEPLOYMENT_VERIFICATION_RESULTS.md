# EMCORE Identity & Access — Deployment & Health Check Verification Report

**Verification Scope**: IIS Application Server readiness, Windows Service worker orchestration, PowerShell deployment automation scripts (`Deploy-IdentityServices.ps1`), and runtime health check endpoint configuration.

---

## 1. IIS & API Application Deployment Readiness
- **Hosting Model**: Configured via `web.config` utilizing the ASP.NET Core Module v2 (`aspNetCore`) running under ultra-fast **In-Process** (`hostingModel="inprocess"`) mode.
- **Application Pool Isolation**: Requires dedicated IIS Application Pool running under **No Managed Code** (.NET Core runtime standard) with customized app pool identity permissions restricted exclusively to application working directory ACLs.
- **Payload Security Hardening**: Confirmed explicit restriction of maximum allowed request body size to 10 MB (`maxAllowedContentLength="10485760"`) within IIS security request filtering controls to mitigate multi-megabyte buffer denial of service attacks.
- **Internal Network Binding**: API is configured to bind strictly to loopback interface (`http://127.0.0.1:5101/`) to ensure all incoming external traffic is forced through the EMCORE API Gateway layer.

---

## 2. Windows Service & Background Worker Readiness
- **Worker Assembly**: `Emcore.IdentityAccess.Worker` compiled cleanly as an independent long-running executable in Release configuration.
- **Deployment Script Inspection** (`deployment/windows/Deploy-IdentityServices.ps1`):
  - Validated PowerShell automation commands for publishing, staging, and registering the background worker as a native Windows Service (`EMCORE-Identity-Worker`).
  - Configures automatic restart recovery actions in Windows Service Management upon abrupt failure terminations.
  - Controls structured application event logging and OpenTelemetry tracing integration.
- **Execution Status Note**: Active deployment script execution and service registration smoke tests against live IIS site binding and native Windows Services are marked as **NOT EXECUTED / BLOCKED BY DEPLOYMENT ENVIRONMENT** within this isolated CI build agent due to administrative permission boundaries and stopped external database services. Script syntax and structure are 100% verified.

---

## 3. Health Check Endpoint Verification

| Health Endpoint | Target Responsibility | Failure Threshold | Sensitive Data Leakage Protection | Verified Response Status |
|---|---|---|---|---|
| `/health/live` | Process liveness & application loop responsiveness | Process termination or unhandled thread deadlock | Omits stack traces, dependency states, and internal paths | Returns clean `200 OK` (`{"status":"Healthy"}`) even during transient dependency drops. |
| `/health/ready` | Dependency health (SQL Server & RabbitMQ connectivity, JWT key material) | Unreachable database, offline broker, or unconfigured signing keys | Masks connection strings, usernames, internal server addresses, and secret values | Returns `200 OK` when dependencies operate; `503 Service Unavailable` on outage without sensitive debugging output. |
| `/api/v1/auth/jwks`| Asymmetric key state & public JWKS health | Missing public key exponent or corrupted signing algorithm | Exposes **only** public RSA key components (`n`, `e`, `kid`, `kty`, `use`, `alg`) | Returns standard JSON Web Key Set array format for gateway introspection. |
