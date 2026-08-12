# EMCORE Application Project Architecture Standard

## Core Guidelines

1. **Controller-Based Routing**: All HTTP endpoints must be placed in structurally isolated and logically grouped MVC Controllers. Minimal APIs in `Program.cs` are strictly forbidden for business logic, mapping, and core application routing.
2. **Program.cs Responsibilities**:
   - `Program.cs` should be restricted to application startup, middleware pipeline composition, dependency injection initialization, OpenAPI generation, global routing configuration (`app.MapControllers()`), and basic health checks.
   - Use extension methods in `Extensions/ServiceCollectionExtensions.cs` to group massive Service Collection configurations into semantic blocks if they become unwieldy (e.g., `AddIdentityServices()`, `AddPersistenceLayer()`).
3. **Excluded Systems**: Third-party integrations or specifically engineered reverse-proxies/gateways (like `Emcore.ApiGateway`) are excluded from this structural standard, provided they do not host domain-specific application mapping logic.
4. **Health Checks**: Standard `.NET` health check endpoints (e.g. `/health/live`, `/health/ready`, `/healthz`) can remain in `Program.cs` as they are considered host/framework-level functionality.
5. **System Endpoints**: Endpoints like `/api/v1/system/version` should belong in an isolated `SystemController`.
6. **Backward Compatibility**: Refactoring to MVC controllers must not modify OpenAPI schemas, HTTP routing structures, payload contracts, or the persistence strategies (like Dapper/Stored Procedures).

## Verification Checks

- Run `./scripts/Generate-OpenApi.ps1` locally to ensure your changes to application boundaries do not produce destructive breaking alterations to the OpenAPI specifications.
- Use explicit `[Route("api/v1/...")]` attributes on controllers and specific `[HttpGet]`, `[HttpPost]` attributes on actions.
- Ensure all endpoints return strong, documented types using `[ProducesResponseType]`.
