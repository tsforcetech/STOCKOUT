using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Emcore.IdentityAccess.Api.Middleware;

public sealed class DatabaseCheckMiddleware
{
    private readonly RequestDelegate _next;

    public DatabaseCheckMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration config)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1"))
        {
            if (config.GetValue<bool>("Database:Enabled") == false)
            {
                context.Response.StatusCode = 503;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = 503,
                    Title = "Database not configured",
                    Detail = "The Identity database is explicitly disabled or not configured."
                });
                return;
            }
        }
        await _next(context);
    }
}
