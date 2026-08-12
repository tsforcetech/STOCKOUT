using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Emcore.IdentityAccess.IntegrationTests;

public class ApiAuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiAuthenticationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:IdentityDatabase"] = "inmemory-auth-api-test-db",
                    ["Database:Enabled"] = "true"
                });
            });
        });
    }

    // Test 1: Public Login
    [Fact]
    public async Task Anonymous_Can_Reach_Login_Endpoint()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/auth/login", content);
        
        // Should not be 401. Since the payload is empty/invalid, it should be 400 Bad Request
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Test 2 & Test 4: Protected Current User Endpoint & Identity Me Route
    [Fact]
    public async Task Anonymous_Cannot_Reach_IdentityMe_Endpoint()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/identity/me");
        
        // 401 means the route exists and requires auth. If it was 404, the route would be missing or wrong.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test 3: Account Status Route
    [Fact]
    public async Task Anonymous_Cannot_Reach_AccountStatus_Endpoint()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/account/status");
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test 8: Admin Protection
    [Fact]
    public async Task Anonymous_Cannot_Reach_Admin_Mutation()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/identity/admin/users/status", content);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test 9: Service Client Management
    [Fact]
    public async Task Anonymous_Cannot_Reach_ServiceClient_Management()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/identity/service-clients/register", content);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Test 10: Service Client Token
    [Fact]
    public async Task Anonymous_Can_Reach_ServiceClient_Token_Endpoint()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/auth/token", content);
        
        // Should not be 401 Unauthorized for user bearer token absence
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests 5, 6, 7: Authenticated user context, Missing claim, and Spoofing
    [Fact]
    public async Task Authenticated_User_Resolves_Correctly_And_Spoofing_Is_Ignored()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("TestScheme")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });
                services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("TestScheme")
                        .RequireAuthenticatedUser()
                        .Build();
                });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Register a user so it exists in the DB
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<Emcore.IdentityAccess.Application.Commands.IdentityApplicationService>();
        var reg = await service.RegisterAsync(new Emcore.IdentityAccess.Application.DTOs.RegisterRequest("spoofing@emcore.com", "9998881234", "SecurePass!123"), CancellationToken.None);
        var realUserId = reg.Data!.UserId;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");
        client.DefaultRequestHeaders.Add("X-Test-Inject-UserId", realUserId);
        client.DefaultRequestHeaders.Add("X-User-Id", "attacker-user");

        var response = await client.GetAsync("/api/v1/identity/me");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseString = await response.Content.ReadAsStringAsync();
        
        Assert.Contains(realUserId, responseString);
        Assert.DoesNotContain("attacker-user", responseString);
    }

    [Fact]
    public async Task Missing_Required_User_Claim_Fails_Securely()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("TestSchemeEmpty")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandlerEmpty>("TestSchemeEmpty", options => { });
                services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("TestSchemeEmpty")
                        .RequireAuthenticatedUser()
                        .Build();
                });
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestSchemeEmpty");

        var response = await client.GetAsync("/api/v1/identity/me");
        
        // The endpoint should fail safely (e.g. 401 or 403 or 400 depending on app logic) if UserId cannot be resolved.
        // But since ICurrentUser requires NameIdentifier, and it's missing...
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var injected = Request.Headers["X-Test-Inject-UserId"].ToString();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, string.IsNullOrEmpty(injected) ? "real-user" : injected) };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class TestAuthHandlerEmpty : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandlerEmpty(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Empty claims, missing NameIdentifier
        var identity = new ClaimsIdentity(Array.Empty<Claim>(), "TestSchemeEmpty");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestSchemeEmpty");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
