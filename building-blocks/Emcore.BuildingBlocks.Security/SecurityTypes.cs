using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Emcore.BuildingBlocks.Security;

public interface ICurrentUser
{
    string? UserId { get; }
    string? SessionId { get; }
}

public class CurrentUserContext : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public string? SessionId => _httpContextAccessor.HttpContext?.User?.FindFirst("sid")?.Value;
}

public interface IOrganizationContext { }
public class OrganizationContext : IOrganizationContext { }
public interface IPermissionChecker { }
public class PermissionDecision { }
public interface IServiceIdentity { }
public class SensitiveValueMasker { }
public class AuthenticationOptions { }

public class GatewayHeaderAuthenticationOptions : AuthenticationSchemeOptions { }

public class GatewayHeaderAuthenticationHandler : AuthenticationHandler<GatewayHeaderAuthenticationOptions>
{
    public const string SchemeName = "GatewayHeader";

    public GatewayHeaderAuthenticationHandler(
        IOptionsMonitor<GatewayHeaderAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var userId) && !string.IsNullOrWhiteSpace(userId))
        {
            var claims = new System.Collections.Generic.List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            if (Request.Headers.TryGetValue("X-Session-Id", out var sessionId) && !string.IsNullOrWhiteSpace(sessionId))
            {
                claims.Add(new Claim("sid", sessionId.ToString()));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}

public static class AuthorizationRegistrationExtensions
{
    public static IServiceCollection AddEmcoreSecurity(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserContext>();

        services.AddAuthentication(GatewayHeaderAuthenticationHandler.SchemeName)
                .AddScheme<GatewayHeaderAuthenticationOptions, GatewayHeaderAuthenticationHandler>(
                    GatewayHeaderAuthenticationHandler.SchemeName, options => { });

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(GatewayHeaderAuthenticationHandler.SchemeName)
                .Build();
        });

        return services;
    }
}
