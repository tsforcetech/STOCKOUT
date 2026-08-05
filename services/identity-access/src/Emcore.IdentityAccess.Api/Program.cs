using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Emcore.IdentityAccess.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

app.MapGet("/health/live", IResult () => Results.Ok(new { Status = "Healthy" }));
app.MapGet("/health/ready", IResult () => Results.Ok(new { Status = "Ready", Dependencies = new { } }));
app.MapGet("/api/v1/system/version", IResult () => Results.Ok(new { Service = "emcore-identity-access-api", Version = "0.1.0", Environment = builder.Environment.EnvironmentName }));

var api = app.MapGroup("/api/v1").AddEndpointFilter(async (context, next) => {
    var config = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
    if (config.GetValue<bool>("Database:Enabled") == false)
    {
        return Results.Problem(statusCode: 503, title: "Database not configured");
    }
    return await next(context);
});

api.MapPost("/auth/register", IResult ([FromBody] RegisterRequest request, [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey) => Results.StatusCode(201));
api.MapPost("/auth/verify", IResult ([FromBody] VerifyRequest request, [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey) => Results.Ok());
api.MapPost("/auth/resend-verification", IResult ([FromBody] ResendVerificationRequest request, [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey) => Results.Ok());
api.MapPost("/auth/login", IResult ([FromBody] LoginRequest request) => Results.Ok(new LoginResponse("access_token", "refresh_token", 900)));
api.MapPost("/auth/token/refresh", IResult ([FromBody] RefreshRequest request, [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey) => Results.Ok(new RefreshResponse("access_token", "refresh_token", 900)));
api.MapPost("/auth/logout", IResult ([FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey) => Results.NoContent());
api.MapGet("/security/sessions", IResult () => Results.Ok(new[] { new SessionDto("sess_1", "Active", System.DateTime.UtcNow, null) }));
api.MapDelete("/security/sessions/{sessionId}", IResult (string sessionId, [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey) => Results.NoContent());
api.MapPost("/security/sessions/revoke-all", IResult ([FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey) => Results.NoContent());
api.MapPost("/auth/password/forgot", IResult ([FromBody] ForgotPasswordRequest request) => Results.Accepted());
api.MapPost("/auth/password/reset", IResult ([FromBody] ResetPasswordRequest request, [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey) => Results.Ok());

app.Run();
