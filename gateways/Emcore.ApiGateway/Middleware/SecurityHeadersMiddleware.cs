using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace Emcore.ApiGateway.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add X-Content-Type-Options
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Add Referrer-Policy
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Enable HSTS in production
        if (_environment.IsProduction())
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        // Apply CSP only where relevant without breaking JSON APIs or dev Swagger
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; upgrade-insecure-requests;";

        await _next(context);
    }
}
