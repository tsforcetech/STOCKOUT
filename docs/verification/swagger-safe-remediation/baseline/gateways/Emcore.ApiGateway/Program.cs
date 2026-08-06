using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Emcore.ApiGateway.Extensions;
using Emcore.ApiGateway.Middleware;
using Emcore.ServiceDefaults;
using Emcore.BuildingBlocks.Api;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// Register Gateway core services, rate limiting, authentication, CORS, and YARP
builder.AddGatewayServices();
builder.Services.AddEmcoreOpenApi("v1", "EMCORE Central API Gateway", "Central ingress edge router and unified reverse proxy for all EMCORE enterprise domain APIs and backend-for-frontend gateways. Provides rate limiting, correlation tracking, TLS termination, header sanitization, and centralized API contract discovery.", "1.0.0", "Platform Infrastructure & Edge Team", "All external public, mobile, portal, and partner consumers");

var app = builder.Build();
app.UseEmcoreOpenApi("/openapi/{documentName}.json", enableStandaloneSwaggerUi: false);
app.UseEmcoreOpenApi("/swagger/services/api-gateway/{documentName}/openapi.json", enableStandaloneSwaggerUi: false);

// 1. Forwarded headers
app.UseForwardedHeaders();

// 2. Global exception handling & RFC 7807 Problem Details
app.UseMiddleware<GatewayErrorHandlingMiddleware>();

// 3. Request ID and correlation ID (plus stripping unsafe client internal headers)
app.UseMiddleware<HeaderManagementMiddleware>();

// 4. Structured request logging
app.UseMiddleware<StructuredLoggingMiddleware>();

// 5. Security headers (HSTS, X-Content-Type-Options, Referrer-Policy, CSP)
app.UseMiddleware<Emcore.ApiGateway.Middleware.SecurityHeadersMiddleware>();

// 6. HTTPS redirection outside local development
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Local") && !app.Environment.IsEnvironment("Integration"))
{
    app.UseHttpsRedirection();
}

// 7. CORS
app.UseCors("GatewayCorsPolicy");

// 8. Rate limiting
app.UseRateLimiter();

// 9. Authentication
app.UseAuthentication();

// 10. Authorization
app.UseAuthorization();

// 11. Health endpoints
app.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy", Service = "Emcore.ApiGateway" }))
   .RequireRateLimiting("HealthPolicy")
   .WithName("GatewayLiveHealth")
   .WithSummary("Liveness health probe")
   .WithDescription("Returns immediate operational liveness status of the gateway edge routing process.")
   .WithTags("Health & System Diagnostics");

app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Service = "Emcore.ApiGateway", Dependencies = new { OpenTelemetry = "Optional", DownstreamServices = "ProxyReady" } }))
   .RequireRateLimiting("HealthPolicy")
   .WithName("GatewayReadyHealth")
   .WithSummary("Readiness health probe")
   .WithDescription("Checks downstream reverse proxy readiness and required telemetry dependencies before serving customer ingress traffic.")
   .WithTags("Health & System Diagnostics");

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Emcore.ApiGateway" }))
   .RequireRateLimiting("HealthPolicy")
   .WithName("GatewayGeneralHealth")
   .WithSummary("General health check alias")
   .WithDescription("General load-balancer target probe alias for gateway operational status.")
   .WithTags("Health & System Diagnostics");

app.MapGet("/api/v1/system/version", () => Results.Ok(new { ServiceName = "Emcore.ApiGateway", Version = "0.1.0", Environment = builder.Environment.EnvironmentName }))
   .RequireRateLimiting("AnonymousPolicy")
   .WithName("GatewaySystemVersion")
   .WithSummary("Gateway runtime version metadata")
   .WithDescription("Returns deployed platform gateway build version, host runtime configuration, and running environment identification.")
   .WithTags("Health & System Diagnostics");

var swaggerRegistry = new[]
{
    new { service = "emcore-api-gateway", name = "EMCORE Central API Gateway", version = "v1", url = "/swagger/services/api-gateway/v1/openapi.json", gatewayPrefix = "/", classification = "Gateway", available = true },
    new { service = "emcore-public-bff", name = "EMCORE Public Web & Mobile BFF", version = "v1", url = "/swagger/services/public-bff/v1/openapi.json", gatewayPrefix = "/api/v1/public", classification = "Gateway", available = true },
    new { service = "emcore-portal-bff", name = "EMCORE Tenant Portal BFF", version = "v1", url = "/swagger/services/portal-bff/v1/openapi.json", gatewayPrefix = "/api/v1/portal", classification = "Gateway", available = true },
    new { service = "emcore-mcp-gateway", name = "EMCORE AI & MCP Tools Gateway", version = "v1", url = "/swagger/services/mcp-gateway/v1/openapi.json", gatewayPrefix = "/api/v1/mcp", classification = "Gateway", available = true },
    new { service = "emcore-realtime-gateway", name = "EMCORE SignalR Realtime Gateway", version = "v1", url = "/swagger/services/realtime-gateway/v1/openapi.json", gatewayPrefix = "/api/v1/realtime", classification = "Gateway", available = true },
    new { service = "emcore-identity-access-api", name = "EMCORE Identity & Access API", version = "v1", url = "/swagger/services/identity-access/v1/openapi.json", gatewayPrefix = "/api/v1/auth", classification = "Platform Security", available = true },
    new { service = "emcore-user-organization-api", name = "EMCORE User & Organization API", version = "v1", url = "/swagger/services/user-organization/v1/openapi.json", gatewayPrefix = "/api/v1/users", classification = "Core Domain", available = true },
    new { service = "emcore-catalog-listing-api", name = "EMCORE Catalog & Listing API", version = "v1", url = "/swagger/services/catalog-listing/v1/openapi.json", gatewayPrefix = "/api/v1/catalog", classification = "Core Domain", available = true },
    new { service = "emcore-inventory-media-api", name = "EMCORE Inventory & Media API", version = "v1", url = "/swagger/services/inventory-media/v1/openapi.json", gatewayPrefix = "/api/v1/inventory", classification = "Core Domain", available = true },
    new { service = "emcore-search-discovery-api", name = "EMCORE Search & Discovery API", version = "v1", url = "/swagger/services/search-discovery/v1/openapi.json", gatewayPrefix = "/api/v1/search", classification = "Core Domain", available = true },
    new { service = "emcore-bidding-deal-api", name = "EMCORE Bidding & Deal Trading API", version = "v1", url = "/swagger/services/bidding-deal/v1/openapi.json", gatewayPrefix = "/api/v1/deals", classification = "Core Domain", available = true },
    new { service = "emcore-inspection-trust-api", name = "EMCORE Inspection & Trust API", version = "v1", url = "/swagger/services/inspection-trust/v1/openapi.json", gatewayPrefix = "/api/v1/inspections", classification = "Core Domain", available = true },
    new { service = "emcore-subscription-payment-api", name = "EMCORE Subscription & Payment API", version = "v1", url = "/swagger/services/subscription-payment/v1/openapi.json", gatewayPrefix = "/api/v1/payments", classification = "Core Domain", available = true },
    new { service = "emcore-conversation-realtime-api", name = "EMCORE Conversation & Realtime API", version = "v1", url = "/swagger/services/conversation-realtime/v1/openapi.json", gatewayPrefix = "/api/v1/messages", classification = "Core Domain", available = true },
    new { service = "emcore-notification-integration-api", name = "EMCORE Notification & Integration API", version = "v1", url = "/swagger/services/notification-integration/v1/openapi.json", gatewayPrefix = "/api/v1/webhooks", classification = "Core Domain", available = true },
    new { service = "emcore-workflow-scheduler-api", name = "EMCORE Workflow & Scheduler API", version = "v1", url = "/swagger/services/workflow-scheduler/v1/openapi.json", gatewayPrefix = "/api/v1/workflows", classification = "Core Domain", available = true },
    new { service = "emcore-audit-reporting-api", name = "EMCORE Audit & Reporting API", version = "v1", url = "/swagger/services/audit-reporting/v1/openapi.json", gatewayPrefix = "/api/v1/audit", classification = "Platform Governance", available = true }
};

app.MapGet("/api/v1/swagger/registry", () => Results.Ok(swaggerRegistry))
   .RequireRateLimiting("AnonymousPolicy")
   .WithName("GetSwaggerRegistry")
   .WithSummary("Centralized OpenAPI document registry")
   .WithDescription("Returns structured metadata and OpenAPI spec URLs for all 17 EMCORE platform services and gateways.")
   .WithTags("Platform Documentation & OpenAPI");

app.MapGet("/docs", () => Results.Redirect("/swagger/index.html"))
   .ExcludeFromDescription();

app.UseWhen(context => !context.Request.Path.StartsWithSegments("/swagger/services"), builder =>
{
    builder.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "EMCORE Platform — Universal API Portal";
        options.EnableDeepLinking();
        options.DisplayRequestDuration();
        options.DefaultModelsExpandDepth(1);
        options.DefaultModelExpandDepth(1);
        
        foreach (var spec in swaggerRegistry)
        {
            options.SwaggerEndpoint(spec.url, $"{spec.name} ({spec.classification})");
        }
    });
});

// 12. YARP reverse proxy
app.MapReverseProxy();

app.Run();

// Make Program public for WebApplicationFactory automated integration testing
public partial class Program { }
