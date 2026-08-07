# EMCORE Identity & Access — Operator Runtime & Deployment Manual

## 1. Runtime Environment Configuration Matrix
The Identity & Access API and background relay worker rely on explicit environment variables or standard .NET configuration providers (JSON settings, UserSecrets, Azure AppConfiguration) to govern database connectivity and security features.

| Environment Variable / Configuration Key | Data Type | Default Value | Operational Purpose |
| :--- | :---: | :---: | :--- |
| `ASPNETCORE_ENVIRONMENT` | `string` | `Production` | Controls ASP.NET Core hosting execution profiles and diagnostic middleware verbosity. |
| `ConnectionStrings:IdentityDatabase` (or `ConnectionStrings__IdentityDatabase`) | `string` | `Empty / InMemory` | Authoritative ADO.NET / SqlClient connection string pointing exclusively to `EMCORE_IDENTITY_DB`. |
| `Database:Enabled` | `bool` | `true` | Emergency circuit-breaker flag. If set to `false`, all API database invocations terminate immediately with `503 Service Unavailable`. |
| `Outbox:Enabled` | `bool` | `true` | Toggles background outbox transactional message polling and event serialization. |
| `Outbox:PollingIntervalSeconds` | `int` | `5` | Defines polling cadence in seconds for `PR_IDENTITY_GET_PENDING_OUTBOX` background executions. |
| `RabbitMq:Enabled` | `bool` | `true` | Controls whether polled outbox events are dispatched across MassTransit / RabbitMQ exchanges. |
| `Cleanup:IntervalHours` | `int` | `1` | Execution frequency in hours for automated database maintenance security cleaning. |
| `Cleanup:RetentionHours` | `int` | `24` | Historical grace retention duration in hours before expired verification OTPs and consumed challenges are permanently purged. |

## 2. Background Task Operations & Maintenance
The standalone Windows background service (`Emcore.IdentityAccess.Worker`) executes two non-blocking daemon background workflows:

### 2.1 Outbox Event Relay (`RabbitMqOutboxRelayWorker`)
- **Execution Workflow**: Wakes up every 5 seconds (configurable) -> opens short-lived SQL connection -> calls `dbo.PR_IDENTITY_GET_PENDING_OUTBOX` with a batch limit of 50 -> relays event payloads to broker exchanges -> marks records published via `dbo.PR_IDENTITY_MARK_OUTBOX_PUBLISHED`.
- **Fault Handling**: Upon delivery failure, logs warning and invokes `dbo.PR_IDENTITY_MARK_OUTBOX_FAILED`, recording error metadata and incrementing retry attempt tallies without stalling subsequent unrelated batch items.

### 2.2 Security Data Cleanup Daemon (`IdentitySecurityDataCleanupWorker`)
- **Execution Workflow**: Wakes up once every hour -> calls `dbo.PR_IDENTITY_CLEANUP_EXPIRED_SECURITY_DATA` with configured retention duration (default 24 hours).
- **Concurrency & Growth Control**: The stored procedure operates over bounded DELETE batches with explicit transaction checks, guaranteeing zero database lock contention against concurrent high-traffic authentication logins or registrations.

## 3. Operational Troubleshooting Guide

### Issue A: API Returns `503 Service Unavailable` on All Requests
- **Symptom**: Client receives HTTP 503 with Problem Details title `"Database not configured"`.
- **Resolution**: Verify that environment variable `Database__Enabled` is set to `true` inside IIS application settings or system environment variables, and verify that `ConnectionStrings__IdentityDatabase` contains a valid, reachable SQL Server connection string.

### Issue B: User Account Experiencing Unexpected Authentication Lockouts
- **Symptom**: Login attempts return HTTP 403 Forbidden with title `"Account Locked"`.
- **Resolution**: An automated security protective lockout was triggered after 5 failed attempts. Advise user to await expiration of the 15-minute cool-down timer or instruct them to invoke the `/api/v1/auth/password/forgot` automated self-service password reset routine, which clears lockout restrictions upon completion.

### Issue C: Outbox Events Accumulating Under Pending Status in Database
- **Symptom**: Table `IDENTITY_OUTBOX` reflects growing rows with `IsPublished = 0`.
- **Resolution**: Inspect the status of Windows Service `EmcoreIdentityRelayWorker` using PowerShell (`Get-Service -Name EmcoreIdentityRelayWorker`). If stopped, start service via `Start-Service EmcoreIdentityRelayWorker`. Check event viewer logs for RabbitMQ network connectivity exceptions or broker authentication failures.
