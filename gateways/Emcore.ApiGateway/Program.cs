using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Emcore.ApiGateway.Extensions;
using Emcore.ApiGateway.Middleware;
using Emcore.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Register Gateway core services, rate limiting, authentication, CORS, and YARP
builder.AddGatewayServices();

var app = builder.Build();

// 1. Forwarded headers
app.UseForwardedHeaders();

// 2. Global exception handling & RFC 7807 Problem Details
app.UseMiddleware<GatewayErrorHandlingMiddleware>();

// 3. Request ID and correlation ID (plus stripping unsafe client internal headers)
app.UseMiddleware<HeaderManagementMiddleware>();

// 4. Structured request logging
app.UseMiddleware<StructuredLoggingMiddleware>();

// 5. Security headers (HSTS, X-Content-Type-Options, Referrer-Policy, CSP)
app.UseMiddleware<SecurityHeadersMiddleware>();

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
   .RequireRateLimiting("HealthPolicy");

app.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Service = "Emcore.ApiGateway", Dependencies = new { OpenTelemetry = "Optional", DownstreamServices = "ProxyReady" } }))
   .RequireRateLimiting("HealthPolicy");

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Emcore.ApiGateway" }))
   .RequireRateLimiting("HealthPolicy");

app.MapGet("/api/v1/system/version", () => Results.Ok(new { ServiceName = "Emcore.ApiGateway", Version = "0.1.0", Environment = builder.Environment.EnvironmentName }))
   .RequireRateLimiting("AnonymousPolicy");

// 12. YARP reverse proxy
app.MapReverseProxy();

app.Run();

// Make Program public for WebApplicationFactory automated integration testing
public partial class Program { }
