using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Emcore.ApiGateway.Tests.Fixtures;

public class GatewayTestFixture : IAsyncDisposable
{
    private WebApplication? _mockIdentityServer;
    private WebApplication? _mockOrgServer;
    private WebApplication? _gatewayApp;

    public HttpClient Client { get; private set; } = null!;
    public string MockIdentityUrl { get; private set; } = string.Empty;
    public string MockOrgUrl { get; private set; } = string.Empty;
    public string GatewayUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync(string testAuthPermitLimit = "30", string timeoutSeconds = "00:00:00.200", string anonymousPermitLimit = "60", string trustedProxy = "127.0.0.1")
    {
        // 1. Start Mock Identity Service on loopback
        var idBuilder = WebApplication.CreateBuilder(new[] { "--urls", "http://127.0.0.1:0" });
        _mockIdentityServer = idBuilder.Build();
        _mockIdentityServer.MapAny("/api/v1/auth/{**path}", (HttpContext ctx) =>
        {
            var authHeader = ctx.Request.Headers["Authorization"].ToString();
            var reqId = ctx.Request.Headers["X-Request-Id"].ToString();
            var corrId = ctx.Request.Headers["X-Correlation-Id"].ToString();
            var unsafeHeader = ctx.Request.Headers["X-User-Id"].ToString();
            return Results.Ok(new { Service = "MockIdentity-Auth", Path = ctx.Request.Path.ToString(), AuthHeader = authHeader, ReqId = reqId, CorrId = corrId, UnsafeHeader = unsafeHeader });
        });
        _mockIdentityServer.MapAny("/api/v1/identity/{**path}", (HttpContext ctx) =>
        {
            var authHeader = ctx.Request.Headers["Authorization"].ToString();
            var reqId = ctx.Request.Headers["X-Request-Id"].ToString();
            var corrId = ctx.Request.Headers["X-Correlation-Id"].ToString();
            var unsafeHeader = ctx.Request.Headers["X-User-Id"].ToString();
            return Results.Ok(new { Service = "MockIdentity-Identity", Path = ctx.Request.Path.ToString(), AuthHeader = authHeader, ReqId = reqId, CorrId = corrId, UnsafeHeader = unsafeHeader });
        });
        await _mockIdentityServer.StartAsync();
        MockIdentityUrl = _mockIdentityServer.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First() + "/";

        // 2. Start Mock Organization Service on loopback
        var orgBuilder = WebApplication.CreateBuilder(new[] { "--urls", "http://127.0.0.1:0" });
        _mockOrgServer = orgBuilder.Build();
        _mockOrgServer.MapAny("/api/v1/users/{**path}", (HttpContext ctx) =>
        {
            var authHeader = ctx.Request.Headers["Authorization"].ToString();
            var reqId = ctx.Request.Headers["X-Request-Id"].ToString();
            var corrId = ctx.Request.Headers["X-Correlation-Id"].ToString();
            var unsafeHeader = ctx.Request.Headers["X-User-Id"].ToString();
            var tenantHeader = ctx.Request.Headers["X-Tenant-Id"].ToString();
            var internalSecret = ctx.Request.Headers["X-Internal-SuperSecret"].ToString();
            var traceParent = ctx.Request.Headers["traceparent"].ToString();
            return Results.Ok(new
            {
                Service = "MockOrg-Users",
                Path = ctx.Request.Path.ToString(),
                AuthHeader = authHeader,
                ReqId = reqId,
                CorrId = corrId,
                UnsafeHeader = unsafeHeader,
                TenantHeader = tenantHeader,
                InternalSecret = internalSecret,
                TraceParent = traceParent
            });
        });
        _mockOrgServer.MapAny("/api/v1/organizations/{**path}", (HttpContext ctx) =>
        {
            var authHeader = ctx.Request.Headers["Authorization"].ToString();
            var reqId = ctx.Request.Headers["X-Request-Id"].ToString();
            var corrId = ctx.Request.Headers["X-Correlation-Id"].ToString();
            var unsafeHeader = ctx.Request.Headers["X-User-Id"].ToString();
            return Results.Ok(new { Service = "MockOrg-Organizations", Path = ctx.Request.Path.ToString(), AuthHeader = authHeader, ReqId = reqId, CorrId = corrId, UnsafeHeader = unsafeHeader });
        });
        _mockOrgServer.MapGet("/api/v1/users/slow", async () =>
        {
            await Task.Delay(5000);
            return Results.Ok("Finished after delay");
        });
        await _mockOrgServer.StartAsync();
        MockOrgUrl = _mockOrgServer.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First() + "/";

        // 3. Start ApiGateway on loopback with overridden configuration
        var gatewayArgs = new[] { "--urls", "http://127.0.0.1:0", "--environment", "Integration" };
        var builder = WebApplication.CreateBuilder(gatewayArgs);

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReverseProxy:Clusters:identity-access-cluster:Destinations:identity-access-api:Address"] = MockIdentityUrl,
            ["ReverseProxy:Clusters:user-organization-cluster:Destinations:user-organization-api:Address"] = MockOrgUrl,
            ["ReverseProxy:Clusters:user-organization-cluster:HttpRequest:Timeout"] = timeoutSeconds,
            ["ReverseProxy:Clusters:user-organization-cluster:HttpRequest:ActivityTimeout"] = timeoutSeconds,
            ["ReverseProxy:Clusters:user-organization-cluster:HttpClient:Timeout"] = timeoutSeconds,
            ["Gateway:AllowedOrigins:0"] = "http://localhost:5173",
            ["RateLimiting:LoginOtp:PermitLimit"] = testAuthPermitLimit,
            ["RateLimiting:Anonymous:PermitLimit"] = anonymousPermitLimit,
            ["Gateway:TrustedProxies:0"] = trustedProxy
        });

        // Register gateway services
        Emcore.ApiGateway.Extensions.GatewayExtensions.AddGatewayServices(builder);

        _gatewayApp = builder.Build();

        _gatewayApp.UseForwardedHeaders();
        _gatewayApp.UseMiddleware<Emcore.ApiGateway.Middleware.GatewayErrorHandlingMiddleware>();
        _gatewayApp.UseMiddleware<Emcore.ApiGateway.Middleware.HeaderManagementMiddleware>();
        _gatewayApp.UseMiddleware<Emcore.ApiGateway.Middleware.StructuredLoggingMiddleware>();
        _gatewayApp.UseMiddleware<Emcore.ApiGateway.Middleware.SecurityHeadersMiddleware>();
        _gatewayApp.UseCors("GatewayCorsPolicy");
        _gatewayApp.UseRateLimiter();
        _gatewayApp.UseAuthentication();
        _gatewayApp.UseAuthorization();

        _gatewayApp.MapGet("/health/live", () => Results.Ok(new { Status = "Healthy", Service = "Emcore.ApiGateway" })).RequireRateLimiting("HealthPolicy");
        _gatewayApp.MapGet("/health/ready", () => Results.Ok(new { Status = "Ready", Service = "Emcore.ApiGateway" })).RequireRateLimiting("HealthPolicy");
        _gatewayApp.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Emcore.ApiGateway" })).RequireRateLimiting("HealthPolicy");
        _gatewayApp.MapGet("/api/v1/system/version", () => Results.Ok(new { ServiceName = "Emcore.ApiGateway", Version = "0.1.0" })).RequireRateLimiting("AnonymousPolicy");
        _gatewayApp.MapReverseProxy();

        await _gatewayApp.StartAsync();
        GatewayUrl = _gatewayApp.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First() + "/";
        Client = new HttpClient { BaseAddress = new Uri(GatewayUrl) };
    }

    public async Task StopMockIdentityAsync()
    {
        if (_mockIdentityServer != null)
        {
            await _mockIdentityServer.StopAsync();
            _mockIdentityServer = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        if (_gatewayApp != null) await _gatewayApp.StopAsync();
        if (_mockOrgServer != null) await _mockOrgServer.StopAsync();
        if (_mockIdentityServer != null) await _mockIdentityServer.StopAsync();
    }
}

public static class EndpointRouteBuilderExtensions
{
    public static void MapAny(this WebApplication app, string pattern, Delegate handler)
    {
        app.MapGet(pattern, handler);
        app.MapPost(pattern, handler);
        app.MapPut(pattern, handler);
        app.MapDelete(pattern, handler);
    }
}
