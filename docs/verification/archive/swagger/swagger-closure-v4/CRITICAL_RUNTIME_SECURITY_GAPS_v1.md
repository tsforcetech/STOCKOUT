# CRITICAL RUNTIME SECURITY GAPS (v1)
**Date**: 2026-08-07
**Component**: `Emcore.IdentityAccess.Api`

## Executive Summary
During the Swagger OpenAPI v4 remediation phase, a critical runtime security gap was formally verified in the core Identity & Access API (`Emcore.IdentityAccess.Api/Program.cs`). The service's endpoints are completely exposed without any active cryptographic authentication, allowing anonymous access to all identity operations including administrative user management, profile retrieval, and session revocation.

## Vulnerability Details
1. **Missing Authentication Middleware**: 
   - `AddAuthentication()`, `UseAuthentication()`, and `UseAuthorization()` are completely missing from the Minimal API configuration.
2. **Missing Endpoint Authorization Constraints**: 
   - No endpoints are decorated with `.RequireAuthorization()` or `.AllowAnonymous()`. They are completely unprotected by default in Minimal APIs.
3. **Gateway Passthrough**: 
   - The API Gateway routes all traffic to `/api/v1/auth/{**catch-all}` using the `PublicPolicy`, meaning the gateway performs no authentication enforcement before proxying.
4. **Vulnerable User ID Extraction** (`ExtractUserId`):
   - **Spoofing**: The implementation accepts the `X-User-Id` header blindly without any validation or trust boundary checks.
   - **Cryptographic Failure**: If the `Authorization: Bearer` header is provided, the code performs a raw Base64 decode of the JWT payload to extract the `sub` claim **without verifying the cryptographic signature**.
   - **Hardcoded Fallback**: The method ultimately falls back to returning the hardcoded string `"user_1234567890_default"` if no headers are present.

## Affected Endpoints
Because there is no global authorization filter or endpoint-specific requirement, the following operations (among others) are vulnerable to anonymous execution or ID spoofing:
- `POST /api/v1/identity/admin/users/status`
- `PUT /api/v1/identity/admin/users/{id}/status`
- `POST /api/v1/auth/password/change`
- `POST /api/v1/auth/logout-all`
- `GET /api/v1/auth/sessions`
- `DELETE /api/v1/auth/sessions/{sessionId}`
- `GET /api/v1/identity/me`

## Recommendation
This behavior must be urgently addressed in a separate Security Remediation task (Track C) as mandated by the "Do not change business logic during Swagger remediation" directive. The fix must include:
- Registering JWT Bearer Authentication (`AddAuthentication().AddJwtBearer()`) configured with the correct Authority/Issuer and Audience.
- Adding `UseAuthentication()` and `UseAuthorization()` to the pipeline.
- Decorating sensitive endpoints with `.RequireAuthorization()`.
- Registering specific Role/Policy checks for the Administrative endpoints.
- Securing `ExtractUserId()` to only use securely validated claims from `HttpContext.User`.
