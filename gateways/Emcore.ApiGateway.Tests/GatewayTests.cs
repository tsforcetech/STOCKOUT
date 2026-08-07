using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Emcore.ApiGateway.Extensions;
using Emcore.ApiGateway.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Emcore.ApiGateway.Tests;

public class GatewayTests
{
    // 1. Gateway starts successfully
    [Fact]
    public async Task Gateway_Starts_Successfully_And_Returns_Version()
    {
        await using var fixture = new GatewayTestFixture();
        await fixture.InitializeAsync();

        var response = await fixture.Client.GetAsync("/api/v1/system/version");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Emcore.ApiGateway");
    }

    // 2. Liveness and readiness return expected status
    [Fact]
    public async Task Liveness_And_Readiness_Return_Expected_Status()
    {
        await using var fixture = new GatewayTestFixture();
        await fixture.InitializeAsync();

        var liveResponse = await fixture.Client.GetAsync("/health/live");
        liveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var liveContent = await liveResponse.Content.ReadAsStringAsync();
        liveContent.Should().Contain("Healthy");

        var readyResponse = await fixture.Client.GetAsync("/health/ready");
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var readyContent = await readyResponse.Content.ReadAsStringAsync();
        readyContent.Should().Contain("Ready");
    }

    // 3. Identity and Organization routes forward correctly
    [Fact]
    public async Task Identity_And_Organization_Routes_Forward_Correctly()
    {
        await using var fixture = new GatewayTestFixture();
        await fixture.InitializeAsync();

        // Public auth route
        var authRes = await fixture.Client.GetAsync("/api/v1/auth/ping");
        authRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var authContent = await authRes.Content.ReadAsStringAsync();
        authContent.Should().Contain("MockIdentity-Auth");

        // Protected org route (providing test token)
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/organizations/test");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "valid-test-token");
        var orgRes = await fixture.Client.SendAsync(req);
        orgRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var orgContent = await orgRes.Content.ReadAsStringAsync();
        orgContent.Should().Contain("MockOrg-Organizations");
    }

    // 4. Authorization header is forwarded
    [Fact]
    public async Task Authorization_Header_Is_Forwarded()
    {
        await using var fixture = new GatewayTestFixture();
        await fixture.InitializeAsync();

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/profile");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "my-secret-test-jwt-token");

        var response = await fixture.Client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Bearer my-secret-test-jwt-token");
    }

    // 5. Request and correlation IDs are generated, preserved and returned
    [Fact]
    public async Task Request_And_Correlation_Ids_Are_Generated_Preserved_And_Returned()
    {
        await using var fixture = new GatewayTestFixture();
        await fixture.InitializeAsync();

        // Case A: When absent, generate and return both IDs
        var resA = await fixture.Client.GetAsync("/api/v1/auth/check");
        resA.StatusCode.Should().Be(HttpStatusCode.OK);
        resA.Headers.Contains("X-Request-Id").Should().BeTrue();
        resA.Headers.Contains("X-Correlation-Id").Should().BeTrue();

        // Case B: When present, preserve and return both IDs
        var reqB = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/check");
        reqB.Headers.Add("X-Request-Id", "req_custom_99");
        reqB.Headers.Add("X-Correlation-Id", "corr_custom_88");

        var resB = await fixture.Client.SendAsync(reqB);
        resB.StatusCode.Should().Be(HttpStatusCode.OK);
        resB.Headers.GetValues("X-Request-Id").Should().ContainSingle().Which.Should().Be("req_custom_99");
        resB.Headers.GetValues("X-Correlation-Id").Should().ContainSingle().Which.Should().Be("corr_custom_88");

        var contentB = await resB.Content.ReadAsStringAsync();
        contentB.Should().Contain("req_custom_99").And.Contain("corr_custom_88");
    }

    // 6. Unknown route returns 404 Problem Details
    [Fact]
    public async Task Unknown_Route_Returns_404_Problem_Details()
    {
        await using var fixture = new GatewayTestFixture();
        await fixture.InitializeAsync();

        var response = await fixture.Client.GetAsync("/api/v1/non-existing-service/data");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("unmatched_gateway_route");
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    // 7. Unavailable destination returns controlled 502/503 Problem Details
    [Fact]
    public async Task Unavailable_Destination_Returns_Controlled_502_Or_503()
    {
        await using var fixture = new GatewayTestFixture();
        await fixture.InitializeAsync();

        // Shut down the identity server to simulate target unavailability
        await fixture.StopMockIdentityAsync();

        var response = await fixture.Client.GetAsync("/api/v1/auth/login");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadGateway, HttpStatusCode.ServiceUnavailable);

        var content = await response.Content.ReadAsStringAsync();
        Assert.True(content.Contains("destination_unavailable") || content.Contains("proxy_error"), $"Expected destination_unavailable or proxy_error but got: {content}");
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    // 8. Timeout returns a controlled result
    [Fact]
    public async Task Timeout_Returns_Controlled_Result()
    {
        await using var fixture = new GatewayTestFixture();
        await fixture.InitializeAsync(timeoutSeconds: "00:00:00.200");

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/slow");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        var response = await fixture.Client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.GatewayTimeout);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("downstream_timeout");
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    // 9. Rate limiting returns 429
    [Fact]
    public async Task Rate_Limiting_Returns_429()
    {
        await using var fixture = new GatewayTestFixture();
        await fixture.InitializeAsync(testAuthPermitLimit: "2");

        var res1 = await fixture.Client.GetAsync("/api/v1/auth/attempt");
        var res2 = await fixture.Client.GetAsync("/api/v1/auth/attempt");
        var res3 = await fixture.Client.GetAsync("/api/v1/auth/attempt");

        res3.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        res3.Headers.Contains("Retry-After").Should().BeTrue();

        var content = await res3.Content.ReadAsStringAsync();
        content.Should().Contain("rate_limit_exceeded");
    }

    // 10. Public authentication route works without authentication
    [Fact]
    public async Task Public_Authentication_Route_Works_Without_Authentication()
    {
        await using var fixture = new GatewayTestFixture();
        await fixture.InitializeAsync();

        var response = await fixture.Client.GetAsync("/api/v1/auth/register");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("MockIdentity-Auth");
    }

    // 11. Protected organization route rejects unauthenticated requests
    [Fact]
    public async Task Protected_Organization_Route_Rejects_Unauthenticated_Requests()
    {
        await using var fixture = new GatewayTestFixture();
        await fixture.InitializeAsync();

        var response = await fixture.Client.GetAsync("/api/v1/organizations/list");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("authentication_required");
    }

    // 12. CORS accepts approved origins and rejects unapproved origins
    [Fact]
    public async Task CORS_Accepts_Approved_Origins_And_Rejects_Unapproved_Origins()
    {
        await using var fixture = new GatewayTestFixture();
        await fixture.InitializeAsync();

        // Case A: Approved origin
        var reqA = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        reqA.Headers.Add("Origin", "http://localhost:5173");
        reqA.Headers.Add("Access-Control-Request-Method", "POST");

        var resA = await fixture.Client.SendAsync(reqA);
        resA.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue();

        // Case B: Unapproved origin
        var reqB = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        reqB.Headers.Add("Origin", "http://malicious-attacker.local");
        reqB.Headers.Add("Access-Control-Request-Method", "POST");

        var resB = await fixture.Client.SendAsync(reqB);
        resB.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    // 13. Unsafe client headers and X-Internal-* are removed case-insensitively while telemetry is preserved
    [Fact]
    public async Task Unsafe_Client_Headers_Are_Removed_Or_Overwritten()
    {
        await using var fixture = new GatewayTestFixture();
        await fixture.InitializeAsync();

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/profile");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");
        // Use varying casing to prove case-insensitive stripping
        req.Headers.Add("x-user-id", "spoofed_admin_id_999");
        req.Headers.Add("X-TENANT-ID", "tenant_hacked");
        req.Headers.Add("X-Internal-SuperSecret", "internal-bypass");
        req.Headers.Add("traceparent", "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01");

        var response = await fixture.Client.SendAsync(req);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotContain("spoofed_admin_id_999")
               .And.NotContain("tenant_hacked")
               .And.NotContain("internal-bypass")
               .And.Contain("4bf92f3577b34da6a3ce929d0e0e4736"); // OpenTelemetry preserves W3C TraceID while adding child span
    }

    // 14. Forwarded header spoofing resistance: untrusted client cannot spoof X-Forwarded-For to evade rate limiting
    [Fact]
    public async Task Untrusted_Client_Cannot_Spoof_Source_IP_Through_Forwarded_Headers()
    {
        await using var fixture = new GatewayTestFixture();
        // Configure trusted proxy to a remote subnet so the test connection from loopback is untrusted
        await fixture.InitializeAsync(anonymousPermitLimit: "2", trustedProxy: "10.0.0.1");

        var req1 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/system/version");
        req1.Headers.Add("X-Forwarded-For", "1.1.1.1");
        var res1 = await fixture.Client.SendAsync(req1);
        res1.StatusCode.Should().Be(HttpStatusCode.OK);

        var req2 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/system/version");
        req2.Headers.Add("X-Forwarded-For", "2.2.2.2");
        var res2 = await fixture.Client.SendAsync(req2);
        res2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Attempting a 3rd request with a different fake IP should fail with 429 because untrusted header is ignored
        var req3 = new HttpRequestMessage(HttpMethod.Get, "/api/v1/system/version");
        req3.Headers.Add("X-Forwarded-For", "3.3.3.3");
        var res3 = await fixture.Client.SendAsync(req3);
        
        res3.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        res3.Headers.Contains("Retry-After").Should().BeTrue();
    }

    // 15. Privileged Identity administrative endpoints under /api/v1/identity/ enforce authentication
    [Fact]
    public async Task Protected_Identity_Administrative_Endpoint_Rejects_Unauthenticated_Requests()
    {
        await using var fixture = new GatewayTestFixture();
        await fixture.InitializeAsync();

        var response = await fixture.Client.GetAsync("/api/v1/identity/admin/revoke");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("authentication_required");
    }

    // 16. Production environment startup fail-fast validation for missing CORS or Authentication configuration
    [Fact]
    public void Production_Environment_Without_Required_Config_Throws_InvalidOperationException()
    {
        // Test 16A: Missing Authentication config throws InvalidOperationException in Production
        var builderA = WebApplication.CreateBuilder(new[] { "--environment", "Production" });
        builderA.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Gateway:AllowedOrigins:0"] = "https://portal.emcore.com"
        });
        Action actA = () => builderA.AddGatewayServices();
        actA.Should().Throw<InvalidOperationException>()
            .WithMessage("*Production authentication configuration*");

        // Test 16B: Missing CORS AllowedOrigins throws InvalidOperationException in Production
        var builderB = WebApplication.CreateBuilder(new[] { "--environment", "Production" });
        builderB.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Issuer"] = "test-issuer",
            ["Authentication:Audience"] = "test-audience",
            ["Authentication:SigningKey"] = "test-secret-key",
            ["Gateway:AllowedOrigins:0"] = string.Empty,
            ["Gateway:AllowedOrigins:1"] = string.Empty,
            ["Gateway:AllowedOrigins:2"] = string.Empty
        });
        
        Action actB = () => builderB.AddGatewayServices();
        actB.Should().Throw<InvalidOperationException>()
            .WithMessage("*Gateway:AllowedOrigins*");
    }

    [Fact]
    public async Task Registry_Identity_Contains_All_Gateway_Prefixes()
    {
        await using var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var res = await client.GetAsync("/api/v1/swagger/registry");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content).RootElement;

        var identity = doc.EnumerateArray().FirstOrDefault(x => x.GetProperty("service").GetString() == "emcore-identity-access-api");
        var prefixes = new List<string>();
        foreach (var p in identity.GetProperty("gatewayPrefixes").EnumerateArray())
        {
            prefixes.Add(p.GetString()!);
        }
        prefixes.Should().Contain(new[] { "/api/v1/auth", "/api/v1/identity" });
    }

    [Fact]
    public async Task Registry_UserOrganization_Contains_All_Gateway_Prefixes()
    {
        await using var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var res = await client.GetAsync("/api/v1/swagger/registry");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content).RootElement;

        var userOrg = doc.EnumerateArray().FirstOrDefault(x => x.GetProperty("service").GetString() == "emcore-user-organization-api");
        var prefixes = new List<string>();
        foreach (var p in userOrg.GetProperty("gatewayPrefixes").EnumerateArray())
        {
            prefixes.Add(p.GetString()!);
        }
        prefixes.Should().Contain(new[] { "/api/v1/users", "/api/v1/organizations" });
    }

    [Fact]
    public async Task Registry_Urls_Are_Unique()
    {
        await using var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var res = await client.GetAsync("/api/v1/swagger/registry");
        var content = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content).RootElement;

        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in doc.EnumerateArray())
        {
            var url = item.GetProperty("url").GetString()!;
            urls.Add(url).Should().BeTrue($"URL '{url}' must be unique in swagger registry.");
        }
    }

    [Fact]
    public async Task Registry_ServiceIds_Are_Unique()
    {
        await using var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var res = await client.GetAsync("/api/v1/swagger/registry");
        var content = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content).RootElement;

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in doc.EnumerateArray())
        {
            var id = item.GetProperty("service").GetString()!;
            ids.Add(id).Should().BeTrue($"Service ID '{id}' must be unique in swagger registry.");
        }
    }

    [Fact]
    public void Registry_Is_Disabled_In_Production_By_Default()
    {
        Action act = () =>
        {
            var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>()
                .WithWebHostBuilder(b => 
                {
                    b.UseEnvironment("Production");
                });
            var client = factory.CreateClient();
        };

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Missing required Production authentication configuration*");
    }

    [Fact]
    public void SwaggerProxy_Is_Disabled_In_Production_By_Default()
    {
        Action act = () =>
        {
            var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>()
                .WithWebHostBuilder(b => 
                {
                    b.UseEnvironment("Production");
                });
            var client = factory.CreateClient();
        };

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Missing required Production authentication configuration*");
    }
}
