using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Emcore.BuildingBlocks.Api;
using Xunit;
using Xunit.Abstractions;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Emcore.OpenApi.Tests;

public class ServiceOpenApiIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public ServiceOpenApiIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("emcore-api-gateway", "Emcore.ApiGateway")]
    [InlineData("emcore-public-bff", "Emcore.PublicBff")]
    [InlineData("emcore-portal-bff", "Emcore.PortalBff")]
    [InlineData("emcore-mcp-gateway", "Emcore.McpGateway")]
    [InlineData("emcore-realtime-gateway", "Emcore.RealtimeGateway")]
    [InlineData("emcore-identity-access-api", "Emcore.IdentityAccess.Api")]
    [InlineData("emcore-user-organization-api", "Emcore.UserOrganization.Api")]
    [InlineData("emcore-catalog-listing-api", "Emcore.CatalogListing.Api")]
    [InlineData("emcore-inventory-media-api", "Emcore.InventoryMedia.Api")]
    [InlineData("emcore-search-discovery-api", "Emcore.SearchDiscovery.Api")]
    [InlineData("emcore-bidding-deal-api", "Emcore.BiddingDeal.Api")]
    [InlineData("emcore-inspection-trust-api", "Emcore.InspectionTrust.Api")]
    [InlineData("emcore-subscription-payment-api", "Emcore.SubscriptionPayment.Api")]
    [InlineData("emcore-conversation-realtime-api", "Emcore.ConversationRealtime.Api")]
    [InlineData("emcore-notification-integration-api", "Emcore.NotificationIntegration.Api")]
    [InlineData("emcore-workflow-scheduler-api", "Emcore.WorkflowScheduler.Api")]
    [InlineData("emcore-audit-reporting-api", "Emcore.AuditReporting.Api")]
    public async Task GenerateAndValidateOpenApiContract(string serviceName, string assemblyName)
    {
        _output.WriteLine($"Testing and generating contract for {serviceName} ({assemblyName})...");
        
        // 1. Locate entry point type via Assembly loading
        var assembly = Assembly.Load(assemblyName);
        var programType = assembly.GetTypes().FirstOrDefault(t => t.Name == "Program") 
                          ?? assembly.EntryPoint?.DeclaringType 
                          ?? throw new System.InvalidOperationException($"Could not find Program entry point in assembly {assemblyName}");

        // 2. Boot up service in memory using WebApplicationFactory
        var factoryType = typeof(WebApplicationFactory<>).MakeGenericType(programType);
        using var factory = (System.IDisposable)System.Activator.CreateInstance(factoryType)!;
        var createClientMethod = factoryType.GetMethod("CreateClient", System.Array.Empty<System.Type>());
        using var client = (System.Net.Http.HttpClient)createClientMethod!.Invoke(factory, null)!;

        // 3. Fetch /openapi/v1.json
        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        var rawJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        // 4. Structural assertions & secret scanning
        root.GetProperty("openapi").GetString().Should().StartWith("3.0");
        var info = root.GetProperty("info");
        info.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
        info.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();

        rawJson.Should().NotContain("BEGIN RSA PRIVATE KEY", "OpenAPI specifications must never expose private keys or certificates.");
        rawJson.Should().NotContain("BEGIN PRIVATE KEY");

        var operationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("paths", out var paths))
        {
            foreach (var pathProperty in paths.EnumerateObject())
            {
                foreach (var verbProperty in pathProperty.Value.EnumerateObject())
                {
                    var op = verbProperty.Value;
                    // Ensure operation has responses defined
                    op.TryGetProperty("responses", out var responses).Should().BeTrue($"Operation {verbProperty.Name.ToUpper()} {pathProperty.Name} must define responses.");
                    
                    // Verify OperationId exists and is unique across the document
                    if (op.TryGetProperty("operationId", out var opIdProp))
                    {
                        var opId = opIdProp.GetString()!;
                        operationIds.Add(opId).Should().BeTrue($"OperationId '{opId}' on {verbProperty.Name.ToUpper()} {pathProperty.Name} must be unique across the contract.");
                    }

                    // Verify error code 500 is documented via our EmcoreErrorResponseTransformer for non-health endpoints
                    if (!pathProperty.Name.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
                    {
                        responses.TryGetProperty("500", out _).Should().BeTrue($"Operation {verbProperty.Name.ToUpper()} {pathProperty.Name} must document 500 Internal Server Error.");
                    }
                }
            }
        }

        // 5. Export deterministic JSON file to contracts/openapi/{serviceName}/v1/openapi.json
        var outputRoot = Environment.GetEnvironmentVariable("EMCORE_OPENAPI_EXPORT_PATH");
        if (string.IsNullOrWhiteSpace(outputRoot))
        {
            // Default to repository root -> contracts/openapi
            var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "Emcore.Platform.slnx")))
            {
                currentDir = currentDir.Parent;
            }
            if (currentDir != null)
            {
                outputRoot = Path.Combine(currentDir.FullName, "contracts", "openapi");
            }
            else
            {
                outputRoot = Path.Combine(Directory.GetCurrentDirectory(), "contracts", "openapi");
            }
        }

        var targetDir = Path.Combine(outputRoot, serviceName, "v1");
        Directory.CreateDirectory(targetDir);

        var targetFilePath = Path.Combine(targetDir, "openapi.json");
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var formattedJson = JsonSerializer.Serialize(root, jsonOptions);
        
        await File.WriteAllTextAsync(targetFilePath, formattedJson);
        _output.WriteLine($"Saved contract to: {targetFilePath}");
        File.Exists(targetFilePath).Should().BeTrue();
    }

    [Fact]
    public void Production_Swagger_Is_Disabled_By_Default()
    {
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(new[] { "--environment", "Production" });
        builder.Services.AddEmcoreOpenApi("v1", "Test API", "Test Desc", "1.0.0");
        var app = builder.Build();
        app.UseEmcoreOpenApi("/openapi/v1.json");

        var endpoints = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>(app.Services).Endpoints;
        endpoints.Any(e => (e as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern.RawText?.Contains("openapi") == true).Should().BeFalse("OpenAPI JSON endpoint must not be mapped in Production by default.");
    }

    [Fact]
    public void Production_SwaggerEnabled_GenericFlag_Does_Not_Bypass_Guard()
    {
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(new[] { "--environment", "Production" });
        Microsoft.Extensions.Configuration.MemoryConfigurationBuilderExtensions.AddInMemoryCollection(builder.Configuration, new Dictionary<string, string?>
        {
            ["Swagger:Enabled"] = "true",
            ["Swagger:EnableInProduction"] = "false"
        });
        builder.Services.AddEmcoreOpenApi("v1", "Test API", "Test Desc", "1.0.0");
        var app = builder.Build();
        app.UseEmcoreOpenApi("/openapi/v1.json");

        var endpoints = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>(app.Services).Endpoints;
        endpoints.Any(e => (e as Microsoft.AspNetCore.Routing.RouteEndpoint)?.RoutePattern.RawText?.Contains("openapi") == true).Should().BeFalse("Generic Swagger:Enabled=true must not override Production restrictions.");
    }

    [Fact]
    public async Task OpenApi_DoesNotClaimEnforcedIdempotency_When_NoOpStoreRegistered()
    {
        var assembly = Assembly.Load("Emcore.IdentityAccess.Api");
        var programType = assembly.GetTypes().FirstOrDefault(t => t.Name == "Program") ?? assembly.EntryPoint?.DeclaringType!;
        var factoryType = typeof(WebApplicationFactory<>).MakeGenericType(programType);
        using var factory = (System.IDisposable)System.Activator.CreateInstance(factoryType)!;
        var createClientMethod = factoryType.GetMethod("CreateClient", System.Array.Empty<System.Type>());
        using var client = (System.Net.Http.HttpClient)createClientMethod!.Invoke(factory, null)!;

        var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        if (root.TryGetProperty("paths", out var paths))
        {
            foreach (var p in paths.EnumerateObject())
            {
                foreach (var m in p.Value.EnumerateObject())
                {
                    if (m.Value.TryGetProperty("parameters", out var @params))
                    {
                        foreach (var param in @params.EnumerateArray())
                        {
                            if (param.GetProperty("name").GetString() == "X-Idempotency-Key")
                            {
                                var desc = param.GetProperty("description").GetString();
                                desc.Should().Contain("does not enforce duplicate-request protection", "When NoOp store is registered, X-Idempotency-Key description must state it is not enforced.");
                            }
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public async Task OpenApi_ResponseCodes_Match_Runtime_Metadata()
    {
        var assembly = Assembly.Load("Emcore.IdentityAccess.Api");
        var programType = assembly.GetTypes().FirstOrDefault(t => t.Name == "Program") ?? assembly.EntryPoint?.DeclaringType!;
        var factoryType = typeof(WebApplicationFactory<>).MakeGenericType(programType);
        using var factory = (System.IDisposable)System.Activator.CreateInstance(factoryType)!;
        var createClientMethod = factoryType.GetMethod("CreateClient", System.Array.Empty<System.Type>());
        using var client = (System.Net.Http.HttpClient)createClientMethod!.Invoke(factory, null)!;

        var response = await client.GetAsync("/openapi/v1.json");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("paths", out var paths))
        {
            foreach (var p in paths.EnumerateObject())
            {
                foreach (var m in p.Value.EnumerateObject())
                {
                    if (m.Value.TryGetProperty("responses", out var responses))
                    {
                        if (p.Name.StartsWith("/health", StringComparison.OrdinalIgnoreCase) || m.Name.ToLowerInvariant() == "get")
                        {
                            responses.TryGetProperty("422", out _).Should().BeFalse($"GET/Health endpoint {m.Name.ToUpper()} {p.Name} must not inject 422.");
                            responses.TryGetProperty("409", out _).Should().BeFalse($"GET/Health endpoint {m.Name.ToUpper()} {p.Name} must not inject 409.");
                        }
                    }
                }
            }
        }
    }
}
