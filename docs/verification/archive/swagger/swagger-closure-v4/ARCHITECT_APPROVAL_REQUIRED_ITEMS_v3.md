# ARCHITECT APPROVAL REQUIRED ITEMS (v3)
**Date**: 2026-08-07
**Related Phase**: Swagger Closure v4 Security Audit

The following critical security items have been identified and formally documented in `CRITICAL_RUNTIME_SECURITY_GAPS_v1.md`. Because the current remediation track strictly prohibits altering business logic, these architectural changes require formal architect approval before being executed in a dedicated Security Remediation phase.

## 1. Minimal API Security Pipeline Integration
**Status**: MISSING
**Proposal**:
- Inject `builder.Services.AddAuthentication().AddJwtBearer()` in `Emcore.IdentityAccess.Api/Program.cs`.
- Add `app.UseAuthentication()` and `app.UseAuthorization()` to the main pipeline.

## 2. Endpoint Protection Enforcement
**Status**: UNPROTECTED (Gateway Passthrough)
**Proposal**:
- Modify the gateway YARP routing for `/api/v1/auth/{**catch-all}` to use a more restrictive policy if applicable, or ensure the Identity API explicitly guards its own endpoints.
- Decorate administrative endpoints (e.g., `/api/v1/identity/admin/users/status`) with `.RequireAuthorization("ElevatedAdminPolicy")`.
- Decorate standard authenticated endpoints (e.g., `/api/v1/auth/password/change`, `/api/v1/auth/sessions`) with `.RequireAuthorization()`.

## 3. Secure Identity Extraction
**Status**: CRITICAL VULNERABILITY (Spoofing & Signature Bypass)
**Proposal**:
- Rewrite `ExtractUserId(HttpContext context)` to exclusively rely on `context.User.FindFirst(ClaimTypes.NameIdentifier)` established by the verified JWT Bearer middleware.
- Completely remove the manual `Base64` payload decoding of the `Authorization: Bearer` header.
- Completely remove the `X-User-Id` header ingestion.
- Completely remove the hardcoded `"user_1234567890_default"` fallback in non-development environments.

## Architect Sign-Off
**Reviewer:** ___________________________
**Date:** _______________________________
**Decision:** [ ] APPROVED   [ ] REJECTED   [ ] REVISION REQUIRED
