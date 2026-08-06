# EMCORE Identity & Access — Comprehensive API Endpoint Inventory

## 1. Gateway Routing Overview
The centralized EMCORE API Gateway (`Emcore.ApiGateway`) acts as the terminating edge Reverse Proxy (powered by YARP). All external traffic originating from public web clients, mobile apps, or enterprise integrations is forwarded to the internal Identity & Access host at `http://127.0.0.1:5101/` via the approved external path groups:
- `/api/v1/auth/{**catch-all}`
- `/api/v1/identity/{**catch-all}`
- `/.well-known/jwks.json`

## 2. API Endpoint Inventory Matrix
All endpoints accept and yield `application/json` payloads. Error responses conform strictly to **RFC 7807 (Problem Details for HTTP APIs)**.

| HTTP Method | External Endpoint Path | Internal Handler Routing | Auth Required | Idempotency Supported | Description & Lifecycle Impact |
| :--- | :--- | :--- | :---: | :---: | :--- |
| `POST` | `/api/v1/auth/register` | `IdentityApplicationService.RegisterAsync` | No | Yes (`X-Idempotency-Key`) | Registers account with email or mobile. Issues OTP challenge and outbox event. |
| `POST` | `/api/v1/auth/verification/email/send` | `SendEmailVerificationAsync` | No | Yes | Generates and sends a new SHA-256 hashed 2-hour email verification OTP. |
| `POST` | `/api/v1/auth/verification/email/confirm`| `ConfirmEmailVerificationAsync` | No | No | Validates email OTP token. Marks email verified and activates account status. |
| `POST` | `/api/v1/auth/verification/mobile/send`| `SendMobileVerificationAsync` | No | Yes | Generates and sends a new SHA-256 hashed 2-hour mobile verification OTP. |
| `POST` | `/api/v1/auth/verification/mobile/confirm`| `ConfirmMobileVerificationAsync`| No | No | Validates mobile OTP token. Marks mobile verified and activates account status. |
| `POST` | `/api/v1/auth/login` | `LoginAsync` | No | No | Authenticates credentials, evaluates lockout counters, issues JWT access token + refresh token. |
| `POST` | `/api/v1/auth/token/refresh` | `RefreshAsync` | No | No | Rotates refresh token within family. Detects token reuse and revokes compromised sessions. |
| `POST` | `/api/v1/auth/logout` | `LogoutAsync` | Yes (Bearer/ID)| No | Revokes current user session and invalidates its active refresh token family. |
| `POST` | `/api/v1/auth/logout-all` | `LogoutAllAsync` | Yes (Bearer/ID)| No | Terminates all active user sessions across all devices and issues audit revocation events. |
| `POST` | `/api/v1/auth/password/forgot` | `ForgotPasswordAsync` | No | Yes | Initiates recovery procedure. Always returns generic response to deter enumeration. |
| `POST` | `/api/v1/auth/password/reset` | `ResetPasswordAsync` | No | No | Validates recovery token, resets PBKDF2 hash, and revokes all active sessions. |
| `POST` | `/api/v1/auth/password/change` | `ChangePasswordAsync` | Yes (Bearer/ID)| No | Verifies old password, applies new password hash, and logs password modification audit event.|
| `GET` | `/api/v1/auth/sessions` | `GetSessionsAsync` | Yes (Bearer/ID)| No | Returns list of all active and historically revoked user sessions with device metadata. |
| `DELETE`| `/api/v1/auth/sessions/{sessionId}` | `RevokeSessionAsync` | Yes (Bearer/ID)| No | Terminates a specific active session ID owned by the authenticated user identity. |
| `GET` | `/api/v1/auth/account/status` | `GetAccountStatusAsync` | Yes (Bearer/ID)| No | Returns detailed user status including verification flags, failure counts, and lockout timestamps. |
| `GET` | `/api/v1/identity/me` | `GetCurrentIdentityAsync` | Yes (Bearer/ID)| No | Returns standard authenticated identity claims and verification parameters for client context. |
| `GET` | `/.well-known/jwks.json` | `IJwksService.GetJwksJson` | No | No | Returns public RSA key verification metadata (`RS256`, `kid`, modulus, exponent). |

## 3. Standard Request/Response Payloads & Problem Details Schema

### 3.1 Successful Registration (`201 Created`)
**Request**:
```json
{
  "email": "executive.user@emcore.platform",
  "mobile": "+15550001122",
  "password": "HighSecurityPassword!2026"
}
```
**Response (`201 Created`)**:
```json
{
  "userId": "01JM6ZY2B6K8C3QY4M0W1V789A",
  "email": "executive.user@emcore.platform",
  "mobile": "+15550001122",
  "status": "PendingVerification"
}
```

### 3.2 RFC 7807 Problem Details Error Formatting (`409 Conflict`)
When a duplicate account registration attempt occurs, the API rejects execution and emits an RFC 7807 compliant error payload:
```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://emcore.platform/errors/409",
  "title": "Conflict",
  "status": 409,
  "detail": "An account with this email address or mobile number already exists."
}
```

### 3.3 Enterprise Header Propagation
The API layer automatically extracts, inspects, and reflects standard enterprise tracing headers across every response:
- `X-Request-Id`: Unique per-request transaction identifier.
- `X-Correlation-Id`: Distributed trace identifier across multi-hop microservice workflows.
- `X-Idempotency-Key`: Client-provided deduplication token ensuring zero accidental duplicate operations.
