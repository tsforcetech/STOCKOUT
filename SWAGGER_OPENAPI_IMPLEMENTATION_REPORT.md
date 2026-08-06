# EMCORE Platform — Complete Swagger/OpenAPI Implementation Report

**Status:** Implementation Complete & Live Verified
**Scope:** 17 Platform Hosts (1 Central Gateway, 4 Specialized/BFF Gateways, 12 Domain Backend APIs)
**Standard:** OpenAPI 3.0 / Swashbuckle ASP.NET Core with Customized EMCORE Building Block Transformers

---

## 1. Executive Implementation Overview

A uniform, high-fidelity Swagger/OpenAPI infrastructure has been engineered and implemented across the entire EMCORE Platform. Rather than simple static documentation, the implementation is an active runtime architecture integrated directly into the core building blocks (`Emcore.BuildingBlocks.Api`). It introspects actual controllers, endpoint route mappings, authentication policies, input/output data transfer contracts, and YARP routing rules to expose accurate, testable API specifications.

---

## 2. Shared Architecture & OpenAPI Transformers (`Emcore.BuildingBlocks.Api`)

All EMCORE web services consume a centralized configuration extension (`AddEmcoreOpenApi` / `UseEmcoreOpenApi` in `OpenApiExtensions.cs`), which instruments Swashbuckle with specialized domain transformers:

### 2.1 Server URL Resolution via `IHttpContextAccessor`
To ensure interoperability between direct IDE debug sessions (`http://localhost:<debug-port>/swagger`) and central gateway portal routing (`http://localhost:5000/swagger`), OpenAPI specification generation evaluates runtime execution context:
* **Direct Access:** Returns the base server host as accessed directly.
* **Reverse Proxy Access:** Inspects incoming YARP headers (`X-Forwarded-Host`, `X-Forwarded-Proto`, `X-Forwarded-Prefix`) to dynamically rewrite the OpenAPI document `servers` collection. When loaded via the Central API Gateway, Try-It-Out invocations target the Gateway (Port 5000) rather than attempting direct cross-origin calls to backend container ports.

### 2.2 Granular Idempotency Documentation
Universal, un-targeted header injection was removed in favor of semantic introspection:
* `X-Idempotency-Key` headers are strictly documented on core business mutation endpoints (`POST`, `PUT`, `PATCH` operations modifying critical financial, inventory, or transactional state).
* Authentication, token refresh, login, MFA challenge, step-up authentication, read-only queries (`GET`), and system diagnostic/health endpoints are explicitly exempted from idempotency key documentation.

### 2.3 Exact Operational Response Mappings
Rather than universally injecting arbitrary HTTP status code arrays across every endpoint, operation filters evaluate controller method returns and authorization attributes:
* **Authentication/Authorization:** `401 Unauthorized` and `403 Forbidden` responses are documented exclusively on routes adorned with `[Authorize]` or security policies.
* **Validation & Exceptions:** `400 Bad Request` ("Invalid Request") problem detail models are mapped to operations accepting rich command request payload structures.
* **Idempotency Conflict:** `409 Conflict` is documented on operations supporting idempotent retry execution.

### 2.4 Multi-Tenancy & Context Headers
Headers for tenant isolation (`X-Tenant-Id` and `X-Organization-Id`) are documented using standard ULID/UUID patterns, explicitly indicating that value extraction acts as validation input rather than trusted authorization (actual security context is derived from validated JWT bearer claims).

---

## 3. Centralized Swagger Portal (`Emcore.ApiGateway`)

The Central API Gateway serves as the single unified developer portal for interactive contract exploration across the organization:

1. **Registry Aggregation (`/api/v1/swagger/registry`):** Exposes an automated manifest enumerating all available microservices, version tags, service classifications, and target OpenAPI proxy paths.
2. **Path-Insulated UI Rendering (`UseWhen` Architecture):** In `Program.cs`, the Gateway wraps `UseSwaggerUI` inside conditional middleware (`UseWhen(context => !context.Request.Path.StartsWithSegments("/swagger/services"), ...)`). This ensures that while browser requests to `/swagger` display the consolidated portal interface, backend requests to `/swagger/services/<service>/v1/openapi.json` bypass UI middleware and route cleanly through YARP reverse-proxy clusters to retrieve downstream OpenAPI JSON contracts.

---

## 4. Strict Environment & Exposure Guardrails

To preserve enterprise posture and prevent sensitive interface disclosure in public cloud deployments, exposure logic adheres to strict environmental guardrails:
* **Development & Local Debug:** Both `/openapi/v1.json` specification endpoints and visual `/swagger` UI portals are enabled by default when `ASPNETCORE_ENVIRONMENT` is set to `Development`, `Debug`, or `Local`.
* **Production & Staging:** In Production environments, OpenAPI specifications and Swagger UI endpoints are disabled completely by default, requiring explicit diagnostic feature-flag opt-ins to expose interface contracts.

---

## 5. Automated CI Validation & Contract Exports

To guarantee that no undocumented endpoints or broken contract regressions can enter the production codebase, automated tooling executes continuous verification:
* **`Generate-OpenApi.ps1` Script:** Executes ASP.NET Core `WebApplicationFactory` test harnesses across all 17 service projects to compile and export versioned JSON specification files directly into the repository repository's `contracts/openapi/<service>/v1/openapi.json` structure.
* **Zero Undocumented Endpoints Lock:** CI tests enforce that all publicly routed API methods define descriptive summary documentation, structured error return types, and explicit request schemas.
