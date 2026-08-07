# EMCORE Identity & Access — Configuration Matrix & Startup Fail-Safe Validation Report

**Verification Scope**: Inspection of application settings, configuration binding models, environment restrictions, and startup validation behavior in Production, Integration, and Development environments.

---

## 1. Identity Configuration Key Matrix

| Configuration Key Name | Purpose | Required / Optional | Target Environments | Secret Status | Startup Failure Behavior on Bad / Missing Value |
|---|---|---|---|---|---|
| `ConnectionStrings:IdentityDatabase` | SQL Server database connection string | **Required** (unless DB disabled in test) | Production, Staging, Local DB Dev | **Secret / Sensitive** (masked in logs) | Fails database migrator & readiness checks (`503 Service Unavailable`). |
| `Database:Enabled` | Toggles persistence layer vs runtime test mock | Optional (Default: `true`) | All Environments | Non-Secret | Switches repo implementation in DependencyInjection. |
| `Jwt:SigningKey` | Asymmetric RSA key material / seed | **Required** | Production, Integration | **Critical Secret** | Crashes process during startup via explicit `InvalidOperationException`. |
| `Jwt:Issuer` | Expected token issuing service authority | Optional (Default: `Emcore.Identity`) | All Environments | Non-Secret | Uses approved default if unspecified; strictly validated on tokens. |
| `Jwt:Audience` | Target token consumption platform scope | Optional (Default: `Emcore.Platform`) | All Environments | Non-Secret | Uses approved default if unspecified; strictly validated on tokens. |
| `Jwt:AccessTokenLifetime`| Expiry duration for primary access JWT | Optional (Default: `60` min) | All Environments | Non-Secret | Configures lifetime limits in token claims. |
| `Jwt:RefreshTokenLifetime`| Expiry duration for rotating refresh tokens| Optional (Default: `14` days) | All Environments | Non-Secret | Enforced during session renewal attempts. |
| `Otp:HmacPepper` | Server-side HMAC secret pepper for OTP hashing | **Required** | Production, Integration | **Critical Secret** | Crashes process during startup via explicit `InvalidOperationException`. |
| `RabbitMq:Hostname` | Messaging broker server network address | Required when Outbox active | Production, Staging | Non-Secret | Worker log retries; readiness health check fails (`503`). |
| `RabbitMq:Username` | Messaging broker authentication identity | Required when Outbox active | Production, Staging | Non-Secret (Masked) | Fails broker handshake; triggers worker connection retry backoff. |
| `RabbitMq:Password` | Messaging broker credential password | Required when Outbox active | Production, Staging | **Secret / Sensitive** | Fails broker handshake; triggers worker connection retry backoff. |
| `ASPNETCORE_ENVIRONMENT`| Defines operating context | Required | All Environments | Non-Secret | Regulates development mock delivery vs production zero-log service. |

---

## 2. Startup Fail-Safe Verification
The following negative startup behavior was verified by inspecting the initialization pipeline in `JwtTokenGenerator` and dependency injection configuration:
1. **Production without JWT Key**: When `ASPNETCORE_ENVIRONMENT=Production` and `Jwt:SigningKey` is blank or omitted, the application constructor immediately throws:  
   `System.InvalidOperationException: Production startup validation failed: Mandatory JWT signing key (Jwt:SigningKey) is missing.`
2. **Production without OTP Pepper**: When `ASPNETCORE_ENVIRONMENT=Production` and `Otp:HmacPepper` is blank or omitted, initialization throws:  
   `System.InvalidOperationException: Production startup validation failed: Mandatory OTP HMAC pepper (Otp:HmacPepper) is missing.`
3. **Development Fallback Restriction**: Direct code verification of `Emcore.IdentityAccess.Infrastructure.DependencyInjection.cs` proves that `DevelopmentVerificationDeliveryService` is conditionally enclosed within `if (environment.IsDevelopment())`. In Production or Integration deployments, the DI container strictly prevents registration of this logging mock, binding exclusively to `ProductionVerificationDeliveryService`, which eliminates plaintext token logs entirely.
