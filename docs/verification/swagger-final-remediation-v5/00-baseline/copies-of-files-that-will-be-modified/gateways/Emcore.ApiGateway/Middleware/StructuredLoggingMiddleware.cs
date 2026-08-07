using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Model;

namespace Emcore.ApiGateway.Middleware;

public class StructuredLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<StructuredLoggingMiddleware> _logger;

    public StructuredLoggingMiddleware(RequestDelegate next, ILogger<StructuredLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();

        var requestId = context.Items["X-Request-Id"]?.ToString() ?? context.TraceIdentifier;
        var correlationId = context.Items["X-Correlation-Id"]?.ToString() ?? requestId;
        var traceId = context.Items["traceId"]?.ToString() ?? Activity.Current?.TraceId.ToString() ?? "N/A";

        var method = context.Request.Method;
        var path = context.Request.Path;
        var responseStatus = context.Response.StatusCode;
        var durationMs = stopwatch.ElapsedMilliseconds;

        // Retrieve YARP routing feature if present
        var reverseProxyFeature = context.Features.Get<IReverseProxyFeature>();
        var routeId = reverseProxyFeature?.Route?.Config?.RouteId ?? "N/A";
        var clusterId = reverseProxyFeature?.Cluster?.Config?.ClusterId ?? "N/A";
        var destination = reverseProxyFeature?.ProxiedDestination?.Model?.Config?.Address ?? reverseProxyFeature?.ProxiedDestination?.DestinationId ?? "N/A";

        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User?.Identity?.Name ?? "anonymous";
        var clientId = context.User?.FindFirst("client_id")?.Value ?? "N/A";
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

        // Structured logging output without logging tokens, cookies, secrets, or full request bodies
        _logger.LogInformation(
            "Gateway Request Completed: {requestId}, Correlation: {correlationId}, Trace: {traceId}, Method: {method}, Path: {path}, Route: {routeId}, Cluster: {clusterId}, Destination: {destination}, Status: {responseStatus}, Duration: {durationMs}ms, User: {userId}, Client: {clientId}, RemoteIP: {remoteIp}",
            requestId, correlationId, traceId, method, path, routeId, clusterId, destination, responseStatus, durationMs, userId, clientId, remoteIp);
    }
}
