# Architecture Changes Summary

This document outlines the changes made during the `EMCORE / STOCKOUT` controller architecture refactor to migrate away from inline Minimal APIs.

## Executive Summary
All business logic HTTP endpoints have been migrated from `Program.cs` into properly structured MVC Controllers across all 15 services and gateways in the EMCORE/STOCKOUT solution, with the explicit exception of `Emcore.ApiGateway`.

## Changes by Project

### 1. `Emcore.IdentityAccess.Api`
- **Controllers Created:**
  - `AuthController.cs`: Endpoints for login, logout, refresh, and session management.
  - `VerificationController.cs`: Endpoints for email and mobile OTP verification.
  - `PasswordController.cs`: Endpoints for password resets and changes.
  - `MfaController.cs`: Endpoints for MFA verification and step-up authentication.
  - `ServiceClientController.cs`: Endpoints for OAuth2 client credentials and workload identities.
  - `AdminController.cs`: Endpoints for administrative user status changes.
  - `AccountController.cs`: Endpoints for inspecting current identity and account status.
  - `LegacyController.cs`: Aliases for legacy backward-compatible routes.
  - `JwksController.cs`: Public JWKS endpoints.
- **Middleware Created:**
  - `DatabaseCheckMiddleware.cs`: Replaces the Minimal API `.AddEndpointFilter(...)` that verified database configuration for `/api/v1` routes.
- **Extensions/Base Classes:**
  - `BaseApiController.cs`: Consolidates `AppResult<T>` mapping and `ExtractUserId` logic.
- **Program.cs cleanup:**
  - Extracted over 300 lines of Minimal API mappings into the structured controllers.
  - Maintained OpenAPI registration, health checks, and global exception handling.

### 2. Standard APIs & BFFs
The following projects were updated to move their `/api/v1/system/version` endpoint to a new `SystemController.cs`:
- `Emcore.UserOrganization.Api`
- `Emcore.CatalogListing.Api`
- `Emcore.InventoryMedia.Api`
- `Emcore.SearchDiscovery.Api`
- `Emcore.BiddingDeal.Api`
- `Emcore.InspectionTrust.Api`
- `Emcore.SubscriptionPayment.Api`
- `Emcore.ConversationRealtime.Api`
- `Emcore.NotificationIntegration.Api`
- `Emcore.WorkflowScheduler.Api`
- `Emcore.AuditReporting.Api`
- `Emcore.PublicBff`
- `Emcore.PortalBff`
- `Emcore.McpGateway`
- `Emcore.RealtimeGateway`

*Note: The BFFs and Gateways used anonymous types for their response, while the APIs used `SystemVersionResponse` from their respective Contracts library. `SystemController.cs` was generated to match these exact signatures.*

## Unchanged Assets
- `Emcore.ApiGateway` was explicitly excluded from modifications.
- Health endpoints (`/health/live`, `/health/ready`, `/healthz`) were preserved in `Program.cs` as they are considered framework infrastructure endpoints.

## Verification
- OpenAPI contracts were generated before and after the refactoring.
- The `03_ENDPOINT_INVENTORY_AFTER.md` was generated to match `02_ENDPOINT_INVENTORY_BEFORE.md`.
- No breaking changes to existing routes, business logic, or API contracts occurred.
