using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Emcore.ApiGateway.Middleware;

public class HeaderManagementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HeaderManagementMiddleware> _logger;

    private static readonly string[] UnsafeInternalHeaders = 
    [
        "X-User-Id",
        "X-Tenant-Id",
        "X-Organization-Id",
        "X-Internal-Identity",
        "X-Internal-Role",
        "X-Permissions",
        "X-Is-Admin",
        "X-Internal-Sub",
        "X-Internal-Caller"
    ];

    public HeaderManagementMiddleware(RequestDelegate next, ILogger<HeaderManagementMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Remove unsafe client-supplied internal headers case-insensitively and handle any X-Internal-* patterns
        var headersToRemove = new List<string>();
        foreach (var key in context.Request.Headers.Keys)
        {
            if (UnsafeInternalHeaders.Any(h => string.Equals(h, key, StringComparison.OrdinalIgnoreCase)) ||
                key.StartsWith("X-Internal-", StringComparison.OrdinalIgnoreCase))
            {
                headersToRemove.Add(key);
            }
        }

        foreach (var header in headersToRemove)
        {
            _logger.LogWarning("Removing unsafe client-supplied header: {HeaderName} from remote IP {RemoteIp}", header, context.Connection.RemoteIpAddress);
            context.Request.Headers.Remove(header);
        }

        // Generate X-Request-Id if absent
        string requestId = context.Request.Headers["X-Request-Id"].ToString();
        if (string.IsNullOrWhiteSpace(requestId))
        {
            requestId = Guid.NewGuid().ToString();
            context.Request.Headers["X-Request-Id"] = requestId;
        }

        // Generate X-Correlation-Id if absent
        string correlationId = context.Request.Headers["X-Correlation-Id"].ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers["X-Correlation-Id"] = correlationId;
        }

        // Store IDs in HttpContext.Items for problem details and structured logging
        context.Items["X-Request-Id"] = requestId;
        context.Items["X-Correlation-Id"] = correlationId;
        if (Activity.Current != null)
        {
            Activity.Current.SetTag("requestId", requestId);
            Activity.Current.SetTag("correlationId", correlationId);
            context.Items["traceId"] = Activity.Current.TraceId.ToString();
        }

        // Ensure both IDs are returned in client response headers
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("X-Request-Id"))
            {
                context.Response.Headers["X-Request-Id"] = requestId;
            }
            if (!context.Response.Headers.ContainsKey("X-Correlation-Id"))
            {
                context.Response.Headers["X-Correlation-Id"] = correlationId;
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
