# EMCORE Identity & Access — API & Gateway Verification Report

**Verification Method**: Inspection of API endpoint mapping code (`Emcore.IdentityAccess.Api/Program.cs`), global middleware, and execution of API Gateway verification test suite (`identity-gateway-tests.trx` - 16/16 tests passed).

---

## 1. Implemented Identity API Routes & Policies

| Endpoint Path | HTTP Method | Route Protection / Policy | Rate Limit / Security Controls | Verified Capability |
|---|---|---|---|---|
| `/api/v1/auth/register` | `POST` | Public / Anonymous | Registration Rate Limit | Registers user identity and emits onboarding verification challenge. |
| `/api/v1/auth/login` | `POST` | Public / Anonymous | Strict Login Attempt Throttle | Evaluates PBKDF2 credential; issues JWT or MFA challenge. |
| `/api/v1/auth/token` | `POST` | Public / M2M Secret Auth | Token Issuance Rate Limit | OAuth-style machine client-credentials grant issuing scoped JWTs. |
| `/api/v1/auth/mfa/register` | `POST` | Authenticated (JWT Required) | Standard Rate Limit | Initiates TOTP MFA setup, returning QR URI and pending code. |
| `/api/v1/auth/mfa/confirm` | `POST` | Authenticated (JWT Required) | Attempt Throttle (5 max) | Validates live TOTP code to permanently activate user MFA. |
| `/api/v1/auth/mfa/verify` | `POST` | Public / Challenge Token Auth | Attempt Throttle (5 max) | Consumes login challenge and live OTP to issue final sessions. |
| `/api/v1/auth/stepup/initiate` | `POST` | Authenticated (JWT Required) | Standard Rate Limit | Creates action-scoped step-up verification challenge token. |
| `/api/v1/auth/stepup/verify` | `POST` | Authenticated (JWT Required) | Attempt Throttle (5 max) | Verifies step-up code, granting single-use action execution token. |
| `/api/v1/auth/recovery/forgot` | `POST` | Public / Anonymous | Strict Recovery Throttle | Triggers OTP password recovery; constant response prevents enum. |
| `/api/v1/auth/recovery/reset` | `POST` | Public / OTP Protected | Attempt Throttle | Consumes valid recovery token to apply new PBKDF2 hash credential. |
| `/api/v1/auth/.well-known/jwks.json` | `GET` | Public / Uncached | JWKS Rate Limit | Exposes public RSA signing keys (`RS256`, `kid: emcore-id-key-v1`). |
| `/api/v1/identity/service-clients/register` | `POST` | Privileged Admin Authorization | Administrative Route Policy | Creates workload machine account and returns plaintext secret once. |
| `/api/v1/identity/service-clients/{id}/rotate` | `POST` | Privileged Admin Authorization | Administrative Route Policy | Rotates workload secret while maintaining controlled overlap. |
| `/api/v1/identity/service-clients/credentials/{id}/revoke`| `POST`| Privileged Admin Authorization | Administrative Route Policy | Revokes specific machine credentials immediately. |
| `/api/v1/identity/admin/users/status` | `POST` | Privileged Admin Authorization | Administrative Route Policy | Updates user account lock/suspension state; requires reason. |

---

## 2. API Gateway Routing & Header Propagation
- **Gateway Catch-All Forwarding Rules**:
  - External routes matching `/api/v1/auth/{**catch-all}` and `/api/v1/identity/{**catch-all}` are reliably forwarded to the internal upstream address `http://127.0.0.1:5101/`.
- **Header Propagation**:
  - Confirmed forwarding of Request ID (`X-Request-Id`) and Correlation ID (`X-Correlation-Id`) headers across gateway boundaries to ensure continuous tracing across distributed OpenTelemetry logs and problem detail reports.
  - Authorization headers (`Bearer {token}`) pass seamlessly to internal minimal API endpoints for local middleware evaluation.

---

## 3. RFC 7807 Problem Details Compliance
- **Exception Handling Middleware**: Registered globally at the top of the HTTP pipeline (`UseMiddleware<ExceptionHandlingMiddleware>()` in `Program.cs:L23`).
- **Standardized Format**:
  - Content-Type: Always set to `application/problem+json` upon validation failures, domain errors, authentication rejection, or unexpected server crashes.
  - Returned attributes include: `type`, `title`, `status`, `code`, `detail`, and distributed tracing tags (`traceId`, `requestId`, `correlationId`).
- **Information Leakage Protection**: Direct inspection of test execution logs verified that internal server stack traces, database schema structures, SQL connection details, and filesystem directory paths are completely sanitized and omitted from all client-facing problem details responses.
