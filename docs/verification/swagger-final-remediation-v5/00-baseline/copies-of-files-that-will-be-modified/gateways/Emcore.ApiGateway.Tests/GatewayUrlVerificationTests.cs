using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Emcore.ApiGateway.Tests;

public class GatewayUrlVerificationTests
{
    private static string GetSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "Emcore.Platform.slnx")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }
        return dir ?? throw new InvalidOperationException("Could not locate Emcore.Platform.slnx solution root from " + AppContext.BaseDirectory);
    }

    private static JsonElement GetGatewayDevConfig()
    {
        var root = GetSolutionRoot();
        var path = Path.Combine(root, "gateways", "Emcore.ApiGateway", "appsettings.Development.json");
        var json = File.ReadAllText(path);
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement GetGatewayBaseConfig()
    {
        var root = GetSolutionRoot();
        var path = Path.Combine(root, "gateways", "Emcore.ApiGateway", "appsettings.json");
        var json = File.ReadAllText(path);
        return JsonDocument.Parse(json).RootElement;
    }

    private static string GetLaunchSettingsHttpUrl(string relativeProjectPath)
    {
        var root = GetSolutionRoot();
        var path = Path.Combine(root, relativeProjectPath, "Properties", "launchSettings.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find launchSettings.json at {path}");
        }

        var json = File.ReadAllText(path);
        var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("profiles", out var profiles) ||
            !profiles.TryGetProperty("http", out var http) ||
            !http.TryGetProperty("applicationUrl", out var urlElement))
        {
            throw new InvalidOperationException($"Could not extract profiles.http.applicationUrl from {path}");
        }

        var urls = urlElement.GetString()?.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var httpUrl = urls?.FirstOrDefault(u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(httpUrl))
        {
            throw new InvalidOperationException($"No HTTP URL found in applicationUrl for {path}");
        }

        return httpUrl.EndsWith("/") ? httpUrl : httpUrl + "/";
    }

    [Fact]
    public void Gateway_LaunchSettings_DefaultHttpPort_Is_5000()
    {
        var url = GetLaunchSettingsHttpUrl("gateways/Emcore.ApiGateway");
        url.Should().Be("http://localhost:5000/", "Gateway default HTTP development port must be 5000.");
    }

    [Fact]
    public void Gateway_IdentityCluster_Destination_Matches_IdentityLaunchSettings()
    {
        var devConfig = GetGatewayDevConfig();
        var address = devConfig.GetProperty("ReverseProxy").GetProperty("Clusters").GetProperty("identity-access-cluster")
            .GetProperty("Destinations").GetProperty("identity-access-api").GetProperty("Address").GetString();

        var expectedUrl = GetLaunchSettingsHttpUrl("services/identity-access/src/Emcore.IdentityAccess.Api");

        address.Should().NotBeNullOrEmpty();
        address.Should().Be(expectedUrl, "The Gateway YARP destination for identity-access-cluster must exactly match Emcore.IdentityAccess.Api launchSettings HTTP port 5194.");
    }

    [Theory]
    [InlineData("public-bff", "public-bff-cluster", "public-bff", "gateways/Emcore.PublicBff")]
    [InlineData("portal-bff", "portal-bff-cluster", "portal-bff", "gateways/Emcore.PortalBff")]
    [InlineData("mcp-gateway", "mcp-gateway-cluster", "mcp-gateway", "gateways/Emcore.McpGateway")]
    [InlineData("realtime-gateway", "realtime-gateway-cluster", "realtime-gateway", "gateways/Emcore.RealtimeGateway")]
    [InlineData("identity-access", "identity-access-cluster", "identity-access-api", "services/identity-access/src/Emcore.IdentityAccess.Api")]
    [InlineData("user-organization", "user-organization-cluster", "user-organization-api", "services/user-organization/src/Emcore.UserOrganization.Api")]
    [InlineData("catalog-listing", "catalog-listing-cluster", "catalog-listing-api", "services/catalog-listing/src/Emcore.CatalogListing.Api")]
    [InlineData("inventory-media", "inventory-media-cluster", "inventory-media-api", "services/inventory-media/src/Emcore.InventoryMedia.Api")]
    [InlineData("search-discovery", "search-discovery-cluster", "search-discovery-api", "services/search-discovery/src/Emcore.SearchDiscovery.Api")]
    [InlineData("bidding-deal", "bidding-deal-cluster", "bidding-deal-api", "services/bidding-deal/src/Emcore.BiddingDeal.Api")]
    [InlineData("inspection-trust", "inspection-trust-cluster", "inspection-trust-api", "services/inspection-trust/src/Emcore.InspectionTrust.Api")]
    [InlineData("subscription-payment", "subscription-payment-cluster", "subscription-payment-api", "services/subscription-payment/src/Emcore.SubscriptionPayment.Api")]
    [InlineData("conversation-realtime", "conversation-realtime-cluster", "conversation-realtime-api", "services/conversation-realtime/src/Emcore.ConversationRealtime.Api")]
    [InlineData("notification-integration", "notification-integration-cluster", "notification-integration-api", "services/notification-integration/src/Emcore.NotificationIntegration.Api")]
    [InlineData("workflow-scheduler", "workflow-scheduler-cluster", "workflow-scheduler-api", "services/workflow-scheduler/src/Emcore.WorkflowScheduler.Api")]
    [InlineData("audit-reporting", "audit-reporting-cluster", "audit-reporting-api", "services/audit-reporting/src/Emcore.AuditReporting.Api")]
    public void Gateway_Cluster_Destination_Matches_Service_LaunchSettings(string serviceKey, string clusterId, string destinationId, string relativeProjectPath)
    {
        clusterId.Should().Be($"{serviceKey}-cluster", "Cluster ID must conform to standard EMCORE reverse-proxy naming convention.");
        var devConfig = GetGatewayDevConfig();
        var clusters = devConfig.GetProperty("ReverseProxy").GetProperty("Clusters");

        clusters.TryGetProperty(clusterId, out var cluster).Should().BeTrue($"Cluster '{clusterId}' must be defined in appsettings.Development.json");
        cluster.GetProperty("Destinations").TryGetProperty(destinationId, out var destination).Should().BeTrue($"Destination '{destinationId}' must exist under '{clusterId}'");

        var address = destination.GetProperty("Address").GetString();
        var expectedUrl = GetLaunchSettingsHttpUrl(relativeProjectPath);

        address.Should().NotBeNullOrEmpty();
        address.Should().Be(expectedUrl, $"Gateway YARP destination address for '{clusterId}' must match '{relativeProjectPath}' launchSettings.");
        address!.Should().StartWith("http://", "All local development cluster forwarding must use consistent HTTP scheme to avoid Dev Certificate TLS negotiation mismatches.");
    }

    [Fact]
    public void All_Development_Service_Ports_Are_Unique()
    {
        var devConfig = GetGatewayDevConfig();
        var clusters = devConfig.GetProperty("ReverseProxy").GetProperty("Clusters");

        var addresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var cluster in clusters.EnumerateObject())
        {
            var dests = cluster.Value.GetProperty("Destinations");
            foreach (var dest in dests.EnumerateObject())
            {
                var addr = dest.Value.GetProperty("Address").GetString()!;
                addresses.Add(addr).Should().BeTrue($"Address '{addr}' in cluster '{cluster.Name}' is duplicated across services. Every service must have a unique Development port.");
            }
        }
    }

    [Fact]
    public void No_Local_Development_Addresses_Exposed_In_Base_AppSettings()
    {
        var baseConfig = GetGatewayBaseConfig();
        if (baseConfig.TryGetProperty("ReverseProxy", out var proxy) && proxy.TryGetProperty("Clusters", out var clusters))
        {
            var json = clusters.ToString();
            json.Should().NotContain("localhost").And.NotContain("127.0.0.1", "Base appsettings.json must never embed local loopback addresses.");
        }
    }

    [Fact]
    public void OpenApi_Proxy_Routes_Are_Configured_For_All_Services()
    {
        var baseConfig = GetGatewayBaseConfig();
        var routes = baseConfig.GetProperty("ReverseProxy").GetProperty("Routes");

        var requiredServices = new[]
        {
            "public-bff", "portal-bff", "mcp-gateway", "realtime-gateway",
            "identity-access", "user-organization", "catalog-listing", "inventory-media",
            "search-discovery", "bidding-deal", "inspection-trust", "subscription-payment",
            "conversation-realtime", "notification-integration", "workflow-scheduler", "audit-reporting"
        };

        foreach (var service in requiredServices)
        {
            var routeKey = $"swagger-{service}";
            routes.TryGetProperty(routeKey, out var route).Should().BeTrue($"Route '{routeKey}' must exist in appsettings.json to proxy OpenAPI documents through Gateway.");

            var matchPath = route.GetProperty("Match").GetProperty("Path").GetString();
            matchPath.Should().Be($"/swagger/services/{service}/v1/openapi.json");

            var transform = route.GetProperty("Transforms")[0].GetProperty("PathSet").GetString();
            transform.Should().Be("/openapi/v1.json");
        }
    }
}
