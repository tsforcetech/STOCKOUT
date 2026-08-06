using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Emcore.IdentityAccess.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred during request processing for {Path}", context.Request.Path);

            context.Response.ContentType = "application/problem+json";
            var statusCode = StatusCodes.Status500InternalServerError;
            var title = "An unexpected server error occurred.";
            var detail = "An internal error occurred while processing your request. Please contact support if the issue persists.";

            if (ex is InvalidOperationException || ex is ArgumentException || ex is FormatException)
            {
                statusCode = StatusCodes.Status400BadRequest;
                title = "Invalid Request";
                detail = ex.Message;
            }
            else if (ex is UnauthorizedAccessException)
            {
                statusCode = StatusCodes.Status401Unauthorized;
                title = "Unauthorized";
                detail = "Authentication failed or credentials are invalid.";
            }

            context.Response.StatusCode = statusCode;

            var problemDetails = new
            {
                type = $"https://emcore.platform/errors/{statusCode}",
                title = title,
                status = statusCode,
                detail = detail,
                instance = context.Request.Path.ToString(),
                traceId = context.TraceIdentifier
            };

            var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await context.Response.WriteAsync(json);
        }
    }
}
