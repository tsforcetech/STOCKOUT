# EMCORE Platform — Development Service URL Registry

**Authoritative System Reference**
**Environment:** Development & Local Debug (`ASPNETCORE_ENVIRONMENT = Development`)
**Architecture Strategy:** Option B — Configuration-Driven Static Development URLs
**Validation Status:** Fully Verified against `launchSettings.json` & `appsettings.Development.json`

---

## 1. Architectural Routing Strategy (Option B)

In local development and Visual Studio / IDE debug execution, the EMCORE Platform operates under **Option B: Configuration-Driven Static Development URLs**. To guarantee deterministic routing and eliminates local service discovery overhead, every microservice host and Backend-For-Frontend (BFF) gateway is statically assigned a permanent, reserved local HTTP debug port in its respective `launchSettings.json`.

The Central API Gateway (`Emcore.ApiGateway`), listening on **port 5000**, mirrors these exact port mappings in `appsettings.Development.json` under YARP (`ReverseProxy:Clusters:<ClusterName>:Destinations:<DestinationName>:Address`). This synchronization ensures that when developers invoke APIs directly or utilize the Central Swagger Try-It-Out UI, traffic routes predictably without port drift or route un-matching errors.

---

## 2. Comprehensive Microservice & BFF Port Registry

The table below catalogs the verified debug HTTP URLs and OpenAPI specification endpoints for all 17 EMCORE platform hosts:

| Service Classification | Service Identifier | Project Root Path | Reserved HTTP Port | Direct Base URL | Direct OpenAPI Contract URL | Central Gateway Proxy Try-It-Out Spec URL |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| **Central Gateway** | `emcore-api-gateway` | `gateways/Emcore.ApiGateway` | **5000** | `http://localhost:5000` | `http://localhost:5000/openapi/v1.json` | `http://localhost:5000/swagger` (Portal) |
| **BFF Gateway** | `public-bff` | `gateways/Emcore.PublicBff` | **5005** | `http://localhost:5005` | `http://localhost:5005/openapi/v1.json` | `http://localhost:5000/swagger/services/public-bff/v1/openapi.json` |
| **BFF Gateway** | `portal-bff` | `gateways/Emcore.PortalBff` | **5127** | `http://localhost:5127` | `http://localhost:5127/openapi/v1.json` | `http://localhost:5000/swagger/services/portal-bff/v1/openapi.json` |
| **Specialized Gateway**| `mcp-gateway` | `gateways/Emcore.McpGateway` | **5055** | `http://localhost:5055` | `http://localhost:5055/openapi/v1.json` | `http://localhost:5000/swagger/services/mcp-gateway/v1/openapi.json` |
| **Specialized Gateway**| `realtime-gateway` | `gateways/Emcore.RealtimeGateway` | **5225** | `http://localhost:5225` | `http://localhost:5225/openapi/v1.json` | `http://localhost:5000/swagger/services/realtime-gateway/v1/openapi.json` |
| **Core Business API** | `identity-access` | `services/identity-access/src/Emcore.IdentityAccess.Api` | **5194** | `http://localhost:5194` | `http://localhost:5194/openapi/v1.json` | `http://localhost:5000/swagger/services/identity-access/v1/openapi.json` |
| **Core Business API** | `user-organization` | `services/user-organization/src/Emcore.UserOrganization.Api` | **5291** | `http://localhost:5291` | `http://localhost:5291/openapi/v1.json` | `http://localhost:5000/swagger/services/user-organization/v1/openapi.json` |
| **Core Business API** | `catalog-listing` | `services/catalog-listing/src/Emcore.CatalogListing.Api` | **5072** | `http://localhost:5072` | `http://localhost:5072/openapi/v1.json` | `http://localhost:5000/swagger/services/catalog-listing/v1/openapi.json` |
| **Core Business API** | `inventory-media` | `services/inventory-media/src/Emcore.InventoryMedia.Api` | **5079** | `http://localhost:5079` | `http://localhost:5079/openapi/v1.json` | `http://localhost:5000/swagger/services/inventory-media/v1/openapi.json` |
| **Core Business API** | `search-discovery` | `services/search-discovery/src/Emcore.SearchDiscovery.Api` | **5255** | `http://localhost:5255` | `http://localhost:5255/openapi/v1.json` | `http://localhost:5000/swagger/services/search-discovery/v1/openapi.json` |
| **Core Business API** | `bidding-deal` | `services/bidding-deal/src/Emcore.BiddingDeal.Api` | **5186** | `http://localhost:5186` | `http://localhost:5186/openapi/v1.json` | `http://localhost:5000/swagger/services/bidding-deal/v1/openapi.json` |
| **Core Business API** | `inspection-trust` | `services/inspection-trust/src/Emcore.InspectionTrust.Api` | **5283** | `http://localhost:5283` | `http://localhost:5283/openapi/v1.json` | `http://localhost:5000/swagger/services/inspection-trust/v1/openapi.json` |
| **Core Business API** | `subscription-payment`| `services/subscription-payment/src/Emcore.SubscriptionPayment.Api` | **5091** | `http://localhost:5091` | `http://localhost:5091/openapi/v1.json` | `http://localhost:5000/swagger/services/subscription-payment/v1/openapi.json` |
| **Core Business API** | `conversation-realtime`| `services/conversation-realtime/src/Emcore.ConversationRealtime.Api`| **5208** | `http://localhost:5208` | `http://localhost:5208/openapi/v1.json` | `http://localhost:5000/swagger/services/conversation-realtime/v1/openapi.json` |
| **Core Business API** | `notification-integration`| `services/notification-integration/src/Emcore.NotificationIntegration.Api`| **5201** | `http://localhost:5201` | `http://localhost:5201/openapi/v1.json` | `http://localhost:5000/swagger/services/notification-integration/v1/openapi.json` |
| **Core Business API** | `workflow-scheduler` | `services/workflow-scheduler/src/Emcore.WorkflowScheduler.Api` | **5266** | `http://localhost:5266` | `http://localhost:5266/openapi/v1.json` | `http://localhost:5000/swagger/services/workflow-scheduler/v1/openapi.json` |
| **Core Business API** | `audit-reporting` | `services/audit-reporting/src/Emcore.AuditReporting.Api` | **5003** | `http://localhost:5003` | `http://localhost:5003/openapi/v1.json` | `http://localhost:5000/swagger/services/audit-reporting/v1/openapi.json` |

---

## 3. Automated Configuration Lock & Drift Prevention

To prevent accidental deviation between individual developer IDE debug port configs and the central gateway routing registry, the CI/CD architectural test suite executes `GatewayUrlVerificationTests`. 

1. **Static Spec Assertion:** Verifies that every microservice `launchSettings.json` HTTP URL perfectly matches the target destination address declared in `Emcore.ApiGateway`'s `appsettings.Development.json`.
2. **Reverse Proxy Verification:** Verifies that YARP route identifiers (`swagger-<service-key>`) correspond strictly to valid target clusters (`<service-key>-cluster`).

Any pull request that attempts to alter a service port or gateway routing target without updating both matching configurations is rejected by automated CI validation.
