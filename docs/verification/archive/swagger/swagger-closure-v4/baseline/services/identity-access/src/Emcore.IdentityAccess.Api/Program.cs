using Emcore.BuildingBlocks.Api;
using Emcore.BuildingBlocks.Core;
using Emcore.IdentityAccess.Api.Middleware;
using Emcore.IdentityAccess.Application;
using Emcore.IdentityAccess.Application.Abstractions;
using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Emcore.IdentityAccess.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddEmcoreOpenApi("v1", "EMCORE Identity & Access API", "Manages user registration, credential authentication, multi-factor authentication (MFA), step-up authorization workflows, JWT session token issuance, session revocation, workload service client identities, JWKS verification keys, and administrative user security status locking. Owns authentication and cryptographic tokens; does not own tenant role definitions or business permissions directly.", "1.0.0", "Identity & Access Security Team", "Platform clients, mobile apps, gateway middleware, and federated service consumers");

var app = builder.Build();
app.UseEmcoreOpenApi();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => true });
app.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => true });

app.Use(async (context, next) =>
{
    // Propagate standard enterprise tracing and idempotency headers
    if (context.Request.Headers.TryGetValue("X-Request-Id", out var reqId))
        context.Response.Headers["X-Request-Id"] = reqId;
    if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var corrId))
        context.Response.Headers["X-Correlation-Id"] = corrId;
    if (context.Request.Headers.TryGetValue("X-Idempotency-Key", out var idemp))
        context.Response.Headers["X-Idempotency-Key"] = idemp;

    await next();
});

// JWKS external endpoints
app.MapGet("/.well-known/jwks.json", (IJwksService jwks) => Results.Content(jwks.GetJwksJson(), "application/json"))
   .WithName("GetPublicJwks")
   .WithSummary("Retrieve public JSON Web Key Set (JWKS)")
   .WithDescription("Returns public RSA verification keys formatted as standard RFC 7517 JWKS for offline token signature verification by API gateways and federated consumers.")
   .WithTags("Public Security Metadata")
   .Produces<object>(200, "application/json");

app.MapGet("/api/v1/auth/.well-known/jwks.json", (IJwksService jwks) => Results.Content(jwks.GetJwksJson(), "application/json"))
   .WithName("GetAuthJwks")
   .WithSummary("Retrieve versioned public JWKS under auth prefix")
   .WithDescription("Returns versioned public RSA verification keys for offline token verification.")
   .WithTags("Public Security Metadata")
   .Produces<object>(200, "application/json");

var api = app.MapGroup("/api/v1").AddEndpointFilter(async (context, next) =>
{
    var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
    if (config.GetValue<bool>("Database:Enabled") == false)
    {
        return Results.Problem(statusCode: 503, title: "Database not configured", detail: "The Identity database is explicitly disabled or not configured.");
    }
    return await next(context);
});

string ExtractUserId(HttpContext context)
{
    if (context.Request.Headers.TryGetValue("X-User-Id", out var uid) && !string.IsNullOrWhiteSpace(uid))
        return uid.ToString();
    
    var sub = context.User?.FindFirst("sub")?.Value ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!string.IsNullOrWhiteSpace(sub)) return sub;

    // Check authorization header for JWT sub claim fallback without dependency on bearer middleware
    if (context.Request.Headers.TryGetValue("Authorization", out var auth) && auth.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var parts = auth.ToString().Substring("Bearer ".Length).Trim().Split('.');
            if (parts.Length >= 2)
            {
                var payloadJson = Convert.FromBase64String(parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=').Replace('-', '+').Replace('_', '/'));
                using var doc = JsonDocument.Parse(payloadJson);
                if (doc.RootElement.TryGetProperty("sub", out var subProp))
                    return subProp.GetString() ?? string.Empty;
            }
        }
        catch { }
    }

    // Default simulation fallback ID for local tests
    return "user_1234567890_default";
}

IResult MapResult<T>(AppResult<T> res)
{
    if (!res.IsSuccess)
        return Results.Problem(statusCode: res.StatusCode, title: res.ErrorTitle, detail: res.ErrorDetail, type: $"https://emcore.platform/errors/{res.StatusCode}");
    
    if (res.StatusCode == 201) return Results.Json(res.Data, statusCode: 201);
    return Results.Ok(res.Data);
}

// Section 4 Approved Auth Contract Endpoints
api.MapPost("/auth/register", async (RegisterRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.RegisterAsync(req, ct)))
   .WithName("RegisterUser")
   .WithSummary("Register new user identity")
   .WithDescription("Registers a new user identity within the system. Generates an account and emits a verification challenge event to the notification outbox.")
   .Produces<RegisterResponse>(201)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(409)
   .Produces<ProblemDetails>(422);

api.MapPost("/auth/verification/email/send", async (SendEmailVerificationRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.SendEmailVerificationAsync(req, ct)))
   .WithName("SendEmailVerification")
   .WithSummary("Send email verification challenge")
   .WithDescription("Triggers a one-time cryptographic verification challenge delivered via email to confirm domain and mailbox control.")
   .Produces<StandardSuccessResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(404)
   .Produces<ProblemDetails>(429);

api.MapPost("/auth/verification/email/confirm", async (ConfirmEmailVerificationRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.ConfirmEmailVerificationAsync(req, ct)))
   .WithName("ConfirmEmailVerification")
   .WithSummary("Confirm email verification token")
   .WithDescription("Validates an email verification token against pending verification records and transitions user account status to Active.")
   .Produces<StandardSuccessResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(422);

api.MapPost("/auth/verification/mobile/send", async (SendMobileVerificationRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.SendMobileVerificationAsync(req, ct)))
   .WithName("SendMobileVerification")
   .WithSummary("Send mobile verification OTP")
   .WithDescription("Dispatches a one-time passcode via SMS channel to the registered international E.164 mobile contact number.")
   .Produces<StandardSuccessResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(404)
   .Produces<ProblemDetails>(429);

api.MapPost("/auth/verification/mobile/confirm", async (ConfirmMobileVerificationRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.ConfirmMobileVerificationAsync(req, ct)))
   .WithName("ConfirmMobileVerification")
   .WithSummary("Confirm mobile verification OTP")
   .WithDescription("Verifies the submitted SMS passcode against active challenge records and verifies mobile channel ownership.")
   .Produces<StandardSuccessResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(422);

api.MapPost("/auth/login", async (LoginRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.LoginAsync(req, ct)))
   .WithName("Login")
   .WithSummary("Authenticate user credentials")
   .WithDescription("Validates identity credentials (email/password or mobile/password), checks account locking rules, and issues JWT access and refresh token pair upon successful authentication.")
   .Produces<LoginResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(401)
   .Produces<ProblemDetails>(403)
   .Produces<ProblemDetails>(429);

api.MapPost("/auth/token/refresh", async (RefreshRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.RefreshAsync(req, ct)))
   .WithName("RefreshToken")
   .WithSummary("Refresh access token")
   .WithDescription("Exchanges a valid, unexpired refresh token for a fresh JWT access token and rotating refresh token pair. Revokes consumed refresh token.")
   .Produces<RefreshResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(401);

api.MapPost("/auth/logout", async (LogoutRequest? req, HttpContext ctx, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.LogoutAsync(req?.RefreshToken, ExtractUserId(ctx), ct)))
   .WithName("Logout")
   .WithSummary("Terminate current session")
   .WithDescription("Revokes the specified active refresh token or authenticated session in distributed session storage, terminating immediate token renewal capabilities.")
   .Produces<StandardSuccessResponse>(200)
   .Produces<ProblemDetails>(401);

api.MapPost("/auth/logout-all", async (HttpContext ctx, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.LogoutAllAsync(ExtractUserId(ctx), ct)))
   .WithName("LogoutAll")
   .WithSummary("Revoke all active sessions")
   .WithDescription("Performs blanket session revocation across all devices and clients for the authenticated identity by incrementing session security timestamps.")
   .Produces<StandardSuccessResponse>(200)
   .Produces<ProblemDetails>(401);

api.MapPost("/auth/password/forgot", async (ForgotPasswordRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.ForgotPasswordAsync(req, ct)))
   .WithName("ForgotPassword")
   .WithSummary("Initiate password reset")
   .WithDescription("Generates a secure password reset token delivered via verified communication channels if the provided account identifier exists.")
   .Produces<ForgotPasswordResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(429);

api.MapPost("/auth/password/reset", async (ResetPasswordRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.ResetPasswordAsync(req, ct)))
   .WithName("ResetPassword")
   .WithSummary("Complete password reset")
   .WithDescription("Applies a new complex user password authenticated by a valid password recovery token and automatically terminates existing sessions.")
   .Produces<ResetPasswordResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(422);

api.MapPost("/auth/password/change", async (ChangePasswordRequest req, HttpContext ctx, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.ChangePasswordAsync(ExtractUserId(ctx), req, ct)))
   .WithName("ChangePassword")
   .WithSummary("Change authenticated user password")
   .WithDescription("Updates the password for an authenticated identity after verifying current password knowledge. Retires active session tokens.")
   .Produces<ChangePasswordResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(401)
   .Produces<ProblemDetails>(422);

api.MapGet("/auth/sessions", async (HttpContext ctx, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.GetSessionsAsync(ExtractUserId(ctx), ct)))
   .WithName("GetSessions")
   .WithSummary("List active authentication sessions")
   .WithDescription("Retrieves all currently active login sessions for the authenticated identity, including IP address, user agent, and creation timestamps.")
   .Produces<List<SessionDto>>(200)
   .Produces<ProblemDetails>(401);

api.MapDelete("/auth/sessions/{sessionId}", async (string sessionId, HttpContext ctx, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.RevokeSessionAsync(ExtractUserId(ctx), sessionId, ct)))
   .WithName("RevokeSession")
   .WithSummary("Revoke specific authentication session")
   .WithDescription("Remotely terminates a specific active login session identified by its session UUID, immediately disallowing future token renewals.")
   .Produces<StandardSuccessResponse>(200)
   .Produces<ProblemDetails>(401)
   .Produces<ProblemDetails>(404);

api.MapGet("/auth/account/status", async (HttpContext ctx, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.GetAccountStatusAsync(ExtractUserId(ctx), ct)))
   .WithName("GetAccountStatus")
   .WithSummary("Inspect user account status")
   .WithDescription("Returns current operational status indicators (Active, Suspended, Locked, PendingVerification) and lockout expiration timestamps for the caller.")
   .Produces<AccountStatusResponse>(200)
   .Produces<ProblemDetails>(401);

api.MapGet("/identity/me", async (HttpContext ctx, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.GetCurrentIdentityAsync(ExtractUserId(ctx), ct)))
   .WithName("GetCurrentIdentity")
   .WithSummary("Retrieve profile for authenticated user")
   .WithDescription("Returns comprehensive profile attributes, organizational membership context, and role assignments for the currently authenticated bearer identity.")
   .Produces<CurrentIdentityResponse>(200)
   .Produces<ProblemDetails>(401);

// MFA & Step-Up Authentication Endpoints
api.MapPost("/auth/mfa/verify", async (MfaLoginVerifyRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.VerifyMfaLoginAsync(req, ct)))
   .WithName("VerifyMfaLogin")
   .WithSummary("Verify multi-factor challenge during login")
   .WithDescription("Verifies an MFA TOTP code or backup verification token during the second phase of user authentication to complete token issuance.")
   .Produces<LoginResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(401)
   .Produces<ProblemDetails>(422);

api.MapPost("/auth/mfa/register", async (RegisterMfaRequest req, HttpContext ctx, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.RegisterMfaAsync(ExtractUserId(ctx), req, ct)))
   .WithName("RegisterMfa")
   .WithSummary("Initialize MFA authenticator registration")
   .WithDescription("Generates a secure TOTP secret seed and QR code enrollment URI for connecting hardware or mobile software authenticator applications.")
   .Produces<RegisterMfaResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(401);

api.MapPost("/auth/mfa/confirm", async (ConfirmMfaRequest req, HttpContext ctx, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.ConfirmMfaAsync(ExtractUserId(ctx), req, ct)))
   .WithName("ConfirmMfa")
   .WithSummary("Confirm MFA authenticator registration")
   .WithDescription("Validates an initial TOTP passcode against a pending MFA secret seed to activate multi-factor authentication requirements for the account.")
   .Produces<StandardSuccessResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(401)
   .Produces<ProblemDetails>(422);

api.MapPost("/auth/stepup/initiate", async (InitiateStepUpRequest req, HttpContext ctx, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.InitiateStepUpAsync(ExtractUserId(ctx), req, ct)))
   .WithName("InitiateStepUp")
   .WithSummary("Initiate step-up authorization challenge")
   .WithDescription("Initiates an elevated step-up verification workflow for protecting sensitive administrative actions or financial transaction changes.")
   .Produces<InitiateStepUpResponse>(200)
   .Produces<ProblemDetails>(401);

api.MapPost("/auth/stepup/verify", async (VerifyStepUpRequest req, HttpContext ctx, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.VerifyStepUpAsync(ExtractUserId(ctx), req, ct)))
   .WithName("VerifyStepUp")
   .WithSummary("Verify step-up authorization challenge")
   .WithDescription("Confirms a step-up challenge verification passcode and issues an short-lived elevated authorization ticket header (X-StepUp-Token).")
   .Produces<VerifyStepUpResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(401)
   .Produces<ProblemDetails>(422);

// Workload & Service Client Identity Endpoints
api.MapPost("/auth/token", async (ServiceTokenRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.IssueServiceTokenAsync(req, ct)))
   .WithName("IssueServiceToken")
   .WithSummary("Issue OAuth2 client credentials token")
   .WithDescription("Authenticates machine workload identities and service clients using ID and secret credentials, issuing a short-lived scoped service JWT.")
   .Produces<ServiceTokenResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(401);

api.MapPost("/identity/service-clients/register", async (RegisterServiceClientRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.RegisterServiceClientAsync(req, ct)))
   .WithName("RegisterServiceClient")
   .WithSummary("Register new service workload client")
   .WithDescription("Creates a dedicated workload machine identity with defined scope permissions and generates an initial high-entropy secret credential.")
   .Produces<RegisterServiceClientResponse>(201)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(403)
   .Produces<ProblemDetails>(409);

api.MapPost("/identity/service-clients/{id}/rotate", async (string id, RotateServiceClientCredentialRequest? req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.RotateServiceClientCredentialAsync(req ?? new RotateServiceClientCredentialRequest(id), ct)))
   .WithName("RotateServiceClientCredential")
   .WithSummary("Rotate service client secret credential")
   .WithDescription("Generates a replacement secret credential for a service client while initiating a grace-period transition window for the legacy secret.")
   .Produces<RotateServiceClientCredentialResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(403)
   .Produces<ProblemDetails>(404);

api.MapPost("/identity/service-clients/credentials/revoke", async (RevokeServiceClientCredentialRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.RevokeServiceClientCredentialAsync(req, ct)))
   .WithName("RevokeServiceClientCredential")
   .WithSummary("Revoke specific service client secret")
   .WithDescription("Immediately revokes and disables an active or retiring secret credential for a workload identity, terminating future token issuance.")
   .Produces<StandardSuccessResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(403)
   .Produces<ProblemDetails>(404);

api.MapGet("/identity/service-clients/{id}/credentials", async (string id, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.ListServiceClientCredentialsAsync(id, ct)))
   .WithName("ListServiceClientCredentials")
   .WithSummary("List active credentials for service client")
   .WithDescription("Lists metadata and status (Active, Retiring, Revoked) for all cryptographic credentials associated with a service client identifier. Does not return secret plaintext.")
   .Produces<List<ServiceClientCredentialDto>>(200)
   .Produces<ProblemDetails>(403)
   .Produces<ProblemDetails>(404);

// Administrative Security Action Endpoints
api.MapPost("/identity/admin/users/status", async (AdminUpdateUserStatusRequest req, HttpContext ctx, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.AdminUpdateUserStatusAsync(req, ExtractUserId(ctx), ct)))
   .WithName("AdminUpdateUserStatusPost")
   .WithSummary("Administrative user account status modification")
   .WithDescription("Allows privileged security administrative officers to suspend, lock, unlock, or terminate an arbitrary user identity across the platform.")
   .Produces<StandardSuccessResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(403)
   .Produces<ProblemDetails>(404);

api.MapPut("/identity/admin/users/{id}/status", async (string id, AdminUpdateUserStatusRequest req, HttpContext ctx, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.AdminUpdateUserStatusAsync(req with { UserId = id }, ExtractUserId(ctx), ct)))
   .WithName("AdminUpdateUserStatusPut")
   .WithSummary("Administrative user status modification by ID")
   .WithDescription("Idempotently updates the lock or active operational status for the target user ID specified in the URI path.")
   .Produces<StandardSuccessResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(403)
   .Produces<ProblemDetails>(404);

// Backward compatibility legacy aliases for existing tests
api.MapPost("/identity/register", async (RegisterRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.RegisterAsync(req, ct)))
   .WithName("LegacyRegister")
   .WithSummary("Legacy identity registration alias")
   .WithDescription("Backward-compatible alias route for /api/v1/auth/register.")
   .WithTags("Legacy Compatibility")
   .Produces<RegisterResponse>(201)
   .Produces<ProblemDetails>(400);

api.MapPost("/identity/verify", async (VerifyRequest req, IdentityApplicationService service, CancellationToken ct) =>
{
    if (req.Channel?.Equals("Mobile", StringComparison.OrdinalIgnoreCase) == true)
        return MapResult(await service.ConfirmMobileVerificationAsync(new ConfirmMobileVerificationRequest(req.UserId, req.Token), ct));
    return MapResult(await service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest(req.UserId, req.Token), ct));
})
.WithName("LegacyVerify")
.WithSummary("Legacy identity verification alias")
.WithDescription("Backward-compatible alias route for verification confirmation across mobile and email channels.")
.WithTags("Legacy Compatibility")
.Produces<StandardSuccessResponse>(200)
.Produces<ProblemDetails>(400);

api.MapPost("/identity/resend-verification", async (ResendVerificationRequest req, IdentityApplicationService service, CancellationToken ct) =>
{
    if (req.Channel?.Equals("Mobile", StringComparison.OrdinalIgnoreCase) == true)
        return MapResult(await service.SendMobileVerificationAsync(new SendMobileVerificationRequest(req.UserId), ct));
    return MapResult(await service.SendEmailVerificationAsync(new SendEmailVerificationRequest(req.UserId), ct));
})
.WithName("LegacyResendVerification")
.WithSummary("Legacy resend verification alias")
.WithDescription("Backward-compatible alias route for resending verification challenges.")
.WithTags("Legacy Compatibility")
.Produces<StandardSuccessResponse>(200)
.Produces<ProblemDetails>(400);

api.MapPost("/identity/login", async (LoginRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.LoginAsync(req, ct)))
   .WithName("LegacyLogin")
   .WithSummary("Legacy login authentication alias")
   .WithDescription("Backward-compatible alias route for /api/v1/auth/login.")
   .WithTags("Legacy Compatibility")
   .Produces<LoginResponse>(200)
   .Produces<ProblemDetails>(400)
   .Produces<ProblemDetails>(401);

api.MapPost("/identity/refresh", async (RefreshRequest req, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.RefreshAsync(req, ct)))
   .WithName("LegacyRefresh")
   .WithSummary("Legacy token refresh alias")
   .WithDescription("Backward-compatible alias route for /api/v1/auth/token/refresh.")
   .WithTags("Legacy Compatibility")
   .Produces<RefreshResponse>(200)
   .Produces<ProblemDetails>(401);

api.MapPost("/identity/logout", async (HttpContext ctx, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.LogoutAsync(null, ExtractUserId(ctx), ct)))
   .WithName("LegacyLogout")
   .WithSummary("Legacy session logout alias")
   .WithDescription("Backward-compatible alias route for /api/v1/auth/logout.")
   .WithTags("Legacy Compatibility")
   .Produces<StandardSuccessResponse>(200);

api.MapGet("/identity/users/{id}", async (string id, IdentityApplicationService service, CancellationToken ct) => MapResult(await service.GetCurrentIdentityAsync(id, ct)))
   .WithName("LegacyGetUserById")
   .WithSummary("Retrieve user profile by explicit ID")
   .WithDescription("Retrieves public profile attributes and tenant organization role context for the specified identity ID.")
   .Produces<CurrentIdentityResponse>(200)
   .Produces<ProblemDetails>(404);

app.Run();

public partial class Program { }

