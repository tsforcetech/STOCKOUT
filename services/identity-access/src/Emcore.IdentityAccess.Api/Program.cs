using Emcore.BuildingBlocks.Api;
using Emcore.BuildingBlocks.Core;
using Emcore.BuildingBlocks.Security;
using Emcore.IdentityAccess.Api.Middleware;
using Emcore.IdentityAccess.Application;
using Emcore.IdentityAccess.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register application and infrastructure services
builder.Services.AddSingleton(builder.Configuration.GetSection(Emcore.IdentityAccess.Application.Configuration.IdentityOptions.SectionName).Get<Emcore.IdentityAccess.Application.Configuration.IdentityOptions>() ?? new Emcore.IdentityAccess.Application.Configuration.IdentityOptions());
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Register security
builder.Services.AddEmcoreSecurity();

// Register controllers and health checks
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// Register OpenAPI documentation
builder.Services.AddEmcoreOpenApi("v1", "EMCORE Identity & Access API", "Manages user registration, credential authentication, multi-factor authentication (MFA), step-up authorization workflows, JWT session token issuance, session revocation, workload service client identities, JWKS verification keys, and administrative user security status locking. Owns authentication and cryptographic tokens; does not own tenant role definitions or business permissions directly.", "1.0.0", "Identity & Access Security Team", "Platform clients, mobile apps, gateway middleware, and federated service consumers");

var app = builder.Build();

app.UseEmcoreOpenApi();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Health checks
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => true });
app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => true });

// Propagate enterprise tracing headers
app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("X-Request-Id", out var reqId))
        context.Response.Headers["X-Request-Id"] = reqId;
    if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var corrId))
        context.Response.Headers["X-Correlation-Id"] = corrId;
    if (context.Request.Headers.TryGetValue("X-Idempotency-Key", out var idemp))
        context.Response.Headers["X-Idempotency-Key"] = idemp;

    await next();
});

// Database check for /api/v1 endpoints
app.UseMiddleware<DatabaseCheckMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Map all standard MVC controllers
app.MapControllers();

app.Run();

public partial class Program { }
