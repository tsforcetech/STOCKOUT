# EMCORE Platform — Swagger & OpenAPI Configuration Reference

**Purpose:** Comprehensive technical guide to ASP.NET Core service registration, YARP reverse-proxy JSON settings, path conditional routing, and environmental feature toggles governing OpenAPI specification generation across EMCORE hosts.

---

## 1. Building Block Service Registration (`Emcore.BuildingBlocks.Api`)

All microservices implement uniform Swagger specification architectures by invoking extension methods located in `Emcore.BuildingBlocks.Api.Extensions.OpenApiExtensions.cs`:

```csharp
// In Microservice Program.cs / Service Registration:
builder.Services.AddEmcoreOpenApi(options =>
{
    options.Title = "EMCORE Identity & Access API";
    options.Version = "v1.0.0";
    options.Description = "Authoritative service contract for authentication and RBAC management.";
    options.EnableJwtBearerSecurity = true;
    options.EnableIdempotencyHeaders = true;
});

// In Middleware Pipeline Configuration:
app.UseEmcoreOpenApi();
```

### 1.1 Registered Swashbuckle Filters & Transformers
When `AddEmcoreOpenApi` is invoked, the container registers `IHttpContextAccessor` and instantiates specialized OpenAPI structural filters:
* **`ServerUrlDocumentFilter`**: Evaluates incoming HTTP requests; dynamically replaces OpenAPI root server definitions with Gateway proxy paths when executed behind YARP (`X-Forwarded-Host`).
* **`IdempotencyHeaderOperationFilter`**: Selectively binds `X-Idempotency-Key` contract parameter definitions strictly to state-modifying mutation endpoints.
* **`ProblemDetailsOperationFilter`**: Binds standard RFC 7807 problem detail response schema structures to mapped operational failure outcomes (`400`, `401`, `403`, `404`, `409`, `422`, `429`, `500`).

---

## 2. Gateway YARP Configuration (`Emcore.ApiGateway`)

The Central API Gateway relies on configuration-driven YARP reverse-proxy rules in `appsettings.json` and `appsettings.Development.json` to proxy specification requests without altering domain service port settings.

### 2.1 Reverse Proxy Route Mapping (`appsettings.json`)
Every downstream microservice maintains a dedicated OpenAPI proxy route matching the canonical Central Try-It-Out path:

```json
{
  "ReverseProxy": {
    "Routes": {
      "swagger-identity-access": {
        "ClusterId": "identity-access-cluster",
        "Match": {
          "Path": "/swagger/services/identity-access/v1/openapi.json"
        },
        "Transforms": [
          { "PathSet": "/openapi/v1.json" },
          { "RequestHeaderOriginalHost": "true" }
        ]
      }
    }
  }
}
```

### 2.2 Cluster Destination Definition (`appsettings.Development.json`)
For local debug environments under **Option B (Static Development URLs)**, clusters specify exact confirmed IDE launch profile HTTP addresses:

```json
{
  "ReverseProxy": {
    "Clusters": {
      "identity-access-cluster": {
        "Destinations": {
          "identity-access-api": {
            "Address": "http://localhost:5194"
          }
        }
      }
    }
  }
}
```

---

## 3. Path-Insulated Middleware Architecture (`UseWhen`)

A critical challenge in unified API Gateways occurs when visual Swagger UI middleware (`UseSwaggerUI`) attempts to serve embedded frontend HTML/JS web assets from `/swagger/*`. Without insulation, requests to `/swagger/services/<service>/v1/openapi.json` are prematurely captured by Swagger UI asset providers, resulting in 404 file errors instead of proxying through YARP.

To overcome this, `Emcore.ApiGateway/Program.cs` incorporates path-conditional branching via `UseWhen`:

```csharp
// Insulate YARP proxy contract paths from being hijacked by static Swagger UI middleware
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/swagger/services"), swaggerApp =>
{
    swaggerApp.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "EMCORE Central API Gateway — Unified Developer Portal";
        options.ConfigObject.Urls = [ /* Dynamic registry endpoints */ ];
    });
});
```

---

## 4. Environmental Toggles & Security Exposure Controls

To prevent exposure of backend architecture details in untrusted network topologies, middleware execution is gated by environment assessment:
* **Development & Local Debug:** When `builder.Environment.IsDevelopment()` evaluates true, `/openapi/v1.json` specifications, `/swagger` UI portals, and `/api/v1/swagger/registry` metadata endpoints initialize by default.
* **Production & High-Security Staging:** When operating in Staging or Production modes, OpenAPI endpoint mapping is fully bypassed by default. Administrators wishing to expose Swagger UI in secure enterprise intranets must explicitly define `EMCORE_SECURITY_ENABLE_OPENAPI_PRODUCTION=true` within runtime deployment configurations.
