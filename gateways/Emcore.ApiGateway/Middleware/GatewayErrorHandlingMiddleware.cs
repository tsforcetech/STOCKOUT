using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace Emcore.ApiGateway.Middleware;

public class GatewayErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayErrorHandlingMiddleware> _logger;

    public GatewayErrorHandlingMiddleware(RequestDelegate next, ILogger<GatewayErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // Intercept errors set by YARP or unmapped routes that haven't written a body yet
            if (context.Response.StatusCode >= 400 && !context.Response.HasStarted)
            {
                var forwarderErrorFeature = context.Features.Get<IForwarderErrorFeature>();
                if (forwarderErrorFeature != null || context.Response.StatusCode is 404 or 502 or 503 or 504 or 429 or 401 or 403)
                {
                    await HandleStatusCodeAsync(context, forwarderErrorFeature);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred in API Gateway pipeline");
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await WriteProblemDetailsAsync(context, StatusCodes.Status500InternalServerError, "Gateway Internal Error", "gateway_internal_error", "An unexpected error occurred processing the request.");
            }
        }
    }

    private async Task HandleStatusCodeAsync(HttpContext context, IForwarderErrorFeature? forwarderError)
    {
        int statusCode = context.Response.StatusCode;
        string title = "An error occurred while processing the request.";
        string machineCode = "gateway_error";
        string detail = "Please check the request parameters or try again later.";

        if (forwarderError != null)
        {
            switch (forwarderError.Error)
            {
                case ForwarderError.RequestTimedOut:
                case ForwarderError.RequestCanceled:
                    statusCode = StatusCodes.Status504GatewayTimeout;
                    title = "Downstream timeout";
                    machineCode = "downstream_timeout";
                    detail = "The downstream destination did not respond in time.";
                    break;
                case ForwarderError.NoAvailableDestinations:
                    statusCode = StatusCodes.Status503ServiceUnavailable;
                    title = "Destination unavailable";
                    machineCode = "destination_unavailable";
                    detail = "The targeted service destination is temporarily unavailable.";
                    break;
                case ForwarderError.Request:
                    statusCode = StatusCodes.Status502BadGateway;
                    title = "Proxy or invalid downstream response";
                    machineCode = "proxy_error";
                    detail = "The gateway received an invalid response from the downstream destination.";
                    break;
                default:
                    statusCode = StatusCodes.Status502BadGateway;
                    title = "Proxy error";
                    machineCode = "proxy_error";
                    detail = "An error occurred forwarding the request to the destination.";
                    break;
            }
            context.Response.StatusCode = statusCode;
        }
        else
        {
            switch (statusCode)
            {
                case StatusCodes.Status404NotFound:
                    title = "Unmatched gateway route";
                    machineCode = "unmatched_gateway_route";
                    detail = "No matching route configuration found in the API Gateway for the requested endpoint.";
                    break;
                case StatusCodes.Status429TooManyRequests:
                    title = "Too Many Requests";
                    machineCode = "rate_limit_exceeded";
                    detail = "You have exceeded your rate limit. Please wait before retrying.";
                    break;
                case StatusCodes.Status401Unauthorized:
                    title = "Unauthorized";
                    machineCode = "authentication_required";
                    detail = "Valid authentication credentials are required to access this resource.";
                    break;
                case StatusCodes.Status403Forbidden:
                    title = "Forbidden";
                    machineCode = "forbidden_access";
                    detail = "You do not have sufficient permissions to access this resource.";
                    break;
                case StatusCodes.Status502BadGateway:
                    title = "Proxy or invalid downstream response";
                    machineCode = "proxy_error";
                    detail = "The gateway encountered an invalid or unroutable downstream destination.";
                    break;
                case StatusCodes.Status503ServiceUnavailable:
                    title = "Destination unavailable";
                    machineCode = "destination_unavailable";
                    detail = "The service destination is currently unavailable.";
                    break;
                case StatusCodes.Status504GatewayTimeout:
                    title = "Downstream timeout";
                    machineCode = "downstream_timeout";
                    detail = "The downstream service timed out.";
                    break;
            }
        }

        await WriteProblemDetailsAsync(context, statusCode, title, machineCode, detail);
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, int statusCode, string title, string machineCode, string detail)
    {
        context.Response.ContentType = "application/problem+json";

        var requestId = context.Items["X-Request-Id"]?.ToString() ?? context.Request.Headers["X-Request-Id"].ToString() ?? context.TraceIdentifier;
        var correlationId = context.Items["X-Correlation-Id"]?.ToString() ?? context.Request.Headers["X-Correlation-Id"].ToString() ?? requestId;
        var traceId = context.Items["traceId"]?.ToString() ?? Activity.Current?.TraceId.ToString();

        var problemDetails = new
        {
            type = $"https://httpstatuses.io/{statusCode}",
            title = title,
            status = statusCode,
            detail = detail,
            code = machineCode,
            requestId = requestId,
            correlationId = correlationId,
            traceId = traceId,
            instance = context.Request.Path.ToString()
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
    }
}
