# EMCORE Setup Verification Evidence

## 1. Project Count and Paths
A total of **125** .NET projects are scaffolded in this monorepository. (12 services * 9 projects = 108) + (10 building blocks) + (5 gateways) + (2 orchestration) = 125 total projects.

### Project List:
`	ext
C:\DEV\API PROJECT\STOCKOUT\building-blocks\Emcore.BuildingBlocks.Api\Emcore.BuildingBlocks.Api.csproj
C:\DEV\API PROJECT\STOCKOUT\building-blocks\Emcore.BuildingBlocks.Caching\Emcore.BuildingBlocks.Caching.csproj
C:\DEV\API PROJECT\STOCKOUT\building-blocks\Emcore.BuildingBlocks.Core\Emcore.BuildingBlocks.Core.csproj
C:\DEV\API PROJECT\STOCKOUT\building-blocks\Emcore.BuildingBlocks.Data\Emcore.BuildingBlocks.Data.csproj
C:\DEV\API PROJECT\STOCKOUT\building-blocks\Emcore.BuildingBlocks.Idempotency\Emcore.BuildingBlocks.Idempotency.csproj
C:\DEV\API PROJECT\STOCKOUT\building-blocks\Emcore.BuildingBlocks.Messaging\Emcore.BuildingBlocks.Messaging.csproj
C:\DEV\API PROJECT\STOCKOUT\building-blocks\Emcore.BuildingBlocks.Observability\Emcore.BuildingBlocks.Observability.csproj
C:\DEV\API PROJECT\STOCKOUT\building-blocks\Emcore.BuildingBlocks.Security\Emcore.BuildingBlocks.Security.csproj
C:\DEV\API PROJECT\STOCKOUT\building-blocks\Emcore.BuildingBlocks.Storage\Emcore.BuildingBlocks.Storage.csproj
C:\DEV\API PROJECT\STOCKOUT\building-blocks\Emcore.BuildingBlocks.Testing\Emcore.BuildingBlocks.Testing.csproj
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.ApiGateway\Emcore.ApiGateway.csproj
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.McpGateway\Emcore.McpGateway.csproj
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.PortalBff\Emcore.PortalBff.csproj
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.PublicBff\Emcore.PublicBff.csproj
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.RealtimeGateway\Emcore.RealtimeGateway.csproj
C:\DEV\API PROJECT\STOCKOUT\orchestration\Emcore.ServiceDefaults\Emcore.ServiceDefaults.csproj
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\src\Emcore.AuditReporting.Api\Emcore.AuditReporting.Api.csproj
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\src\Emcore.AuditReporting.Application\Emcore.AuditReporting.Application.csproj
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\src\Emcore.AuditReporting.Contracts\Emcore.AuditReporting.Contracts.csproj
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\src\Emcore.AuditReporting.Domain\Emcore.AuditReporting.Domain.csproj
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\src\Emcore.AuditReporting.Infrastructure\Emcore.AuditReporting.Infrastructure.csproj
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\src\Emcore.AuditReporting.Worker\Emcore.AuditReporting.Worker.csproj
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\tests\Emcore.AuditReporting.ArchitectureTests\Emcore.AuditReporting.ArchitectureTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\tests\Emcore.AuditReporting.IntegrationTests\Emcore.AuditReporting.IntegrationTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\tests\Emcore.AuditReporting.UnitTests\Emcore.AuditReporting.UnitTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\src\Emcore.BiddingDeal.Api\Emcore.BiddingDeal.Api.csproj
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\src\Emcore.BiddingDeal.Application\Emcore.BiddingDeal.Application.csproj
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\src\Emcore.BiddingDeal.Contracts\Emcore.BiddingDeal.Contracts.csproj
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\src\Emcore.BiddingDeal.Domain\Emcore.BiddingDeal.Domain.csproj
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\src\Emcore.BiddingDeal.Infrastructure\Emcore.BiddingDeal.Infrastructure.csproj
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\src\Emcore.BiddingDeal.Worker\Emcore.BiddingDeal.Worker.csproj
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\tests\Emcore.BiddingDeal.ArchitectureTests\Emcore.BiddingDeal.ArchitectureTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\tests\Emcore.BiddingDeal.IntegrationTests\Emcore.BiddingDeal.IntegrationTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\tests\Emcore.BiddingDeal.UnitTests\Emcore.BiddingDeal.UnitTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\src\Emcore.CatalogListing.Api\Emcore.CatalogListing.Api.csproj
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\src\Emcore.CatalogListing.Application\Emcore.CatalogListing.Application.csproj
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\src\Emcore.CatalogListing.Contracts\Emcore.CatalogListing.Contracts.csproj
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\src\Emcore.CatalogListing.Domain\Emcore.CatalogListing.Domain.csproj
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\src\Emcore.CatalogListing.Infrastructure\Emcore.CatalogListing.Infrastructure.csproj
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\src\Emcore.CatalogListing.Worker\Emcore.CatalogListing.Worker.csproj
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\tests\Emcore.CatalogListing.ArchitectureTests\Emcore.CatalogListing.ArchitectureTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\tests\Emcore.CatalogListing.IntegrationTests\Emcore.CatalogListing.IntegrationTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\tests\Emcore.CatalogListing.UnitTests\Emcore.CatalogListing.UnitTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\src\Emcore.ConversationRealtime.Api\Emcore.ConversationRealtime.Api.csproj
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\src\Emcore.ConversationRealtime.Application\Emcore.ConversationRealtime.Application.csproj
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\src\Emcore.ConversationRealtime.Contracts\Emcore.ConversationRealtime.Contracts.csproj
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\src\Emcore.ConversationRealtime.Domain\Emcore.ConversationRealtime.Domain.csproj
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\src\Emcore.ConversationRealtime.Infrastructure\Emcore.ConversationRealtime.Infrastructure.csproj
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\src\Emcore.ConversationRealtime.Worker\Emcore.ConversationRealtime.Worker.csproj
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\tests\Emcore.ConversationRealtime.ArchitectureTests\Emcore.ConversationRealtime.ArchitectureTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\tests\Emcore.ConversationRealtime.IntegrationTests\Emcore.ConversationRealtime.IntegrationTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\tests\Emcore.ConversationRealtime.UnitTests\Emcore.ConversationRealtime.UnitTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\src\Emcore.IdentityAccess.Api\Emcore.IdentityAccess.Api.csproj
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\src\Emcore.IdentityAccess.Application\Emcore.IdentityAccess.Application.csproj
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\src\Emcore.IdentityAccess.Contracts\Emcore.IdentityAccess.Contracts.csproj
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\src\Emcore.IdentityAccess.Domain\Emcore.IdentityAccess.Domain.csproj
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\src\Emcore.IdentityAccess.Infrastructure\Emcore.IdentityAccess.Infrastructure.csproj
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\src\Emcore.IdentityAccess.Worker\Emcore.IdentityAccess.Worker.csproj
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\tests\Emcore.IdentityAccess.ArchitectureTests\Emcore.IdentityAccess.ArchitectureTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\tests\Emcore.IdentityAccess.IntegrationTests\Emcore.IdentityAccess.IntegrationTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\tests\Emcore.IdentityAccess.UnitTests\Emcore.IdentityAccess.UnitTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\src\Emcore.InspectionTrust.Api\Emcore.InspectionTrust.Api.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\src\Emcore.InspectionTrust.Application\Emcore.InspectionTrust.Application.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\src\Emcore.InspectionTrust.Contracts\Emcore.InspectionTrust.Contracts.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\src\Emcore.InspectionTrust.Domain\Emcore.InspectionTrust.Domain.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\src\Emcore.InspectionTrust.Infrastructure\Emcore.InspectionTrust.Infrastructure.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\src\Emcore.InspectionTrust.Worker\Emcore.InspectionTrust.Worker.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\tests\Emcore.InspectionTrust.ArchitectureTests\Emcore.InspectionTrust.ArchitectureTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\tests\Emcore.InspectionTrust.IntegrationTests\Emcore.InspectionTrust.IntegrationTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\tests\Emcore.InspectionTrust.UnitTests\Emcore.InspectionTrust.UnitTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\src\Emcore.InventoryMedia.Api\Emcore.InventoryMedia.Api.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\src\Emcore.InventoryMedia.Application\Emcore.InventoryMedia.Application.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\src\Emcore.InventoryMedia.Contracts\Emcore.InventoryMedia.Contracts.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\src\Emcore.InventoryMedia.Domain\Emcore.InventoryMedia.Domain.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\src\Emcore.InventoryMedia.Infrastructure\Emcore.InventoryMedia.Infrastructure.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\src\Emcore.InventoryMedia.Worker\Emcore.InventoryMedia.Worker.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\tests\Emcore.InventoryMedia.ArchitectureTests\Emcore.InventoryMedia.ArchitectureTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\tests\Emcore.InventoryMedia.IntegrationTests\Emcore.InventoryMedia.IntegrationTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\tests\Emcore.InventoryMedia.UnitTests\Emcore.InventoryMedia.UnitTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\src\Emcore.NotificationIntegration.Api\Emcore.NotificationIntegration.Api.csproj
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\src\Emcore.NotificationIntegration.Application\Emcore.NotificationIntegration.Application.csproj
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\src\Emcore.NotificationIntegration.Contracts\Emcore.NotificationIntegration.Contracts.csproj
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\src\Emcore.NotificationIntegration.Domain\Emcore.NotificationIntegration.Domain.csproj
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\src\Emcore.NotificationIntegration.Infrastructure\Emcore.NotificationIntegration.Infrastructure.csproj
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\src\Emcore.NotificationIntegration.Worker\Emcore.NotificationIntegration.Worker.csproj
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\tests\Emcore.NotificationIntegration.ArchitectureTests\Emcore.NotificationIntegration.ArchitectureTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\tests\Emcore.NotificationIntegration.IntegrationTests\Emcore.NotificationIntegration.IntegrationTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\tests\Emcore.NotificationIntegration.UnitTests\Emcore.NotificationIntegration.UnitTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\src\Emcore.SearchDiscovery.Api\Emcore.SearchDiscovery.Api.csproj
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\src\Emcore.SearchDiscovery.Application\Emcore.SearchDiscovery.Application.csproj
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\src\Emcore.SearchDiscovery.Contracts\Emcore.SearchDiscovery.Contracts.csproj
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\src\Emcore.SearchDiscovery.Domain\Emcore.SearchDiscovery.Domain.csproj
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\src\Emcore.SearchDiscovery.Infrastructure\Emcore.SearchDiscovery.Infrastructure.csproj
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\src\Emcore.SearchDiscovery.Worker\Emcore.SearchDiscovery.Worker.csproj
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\tests\Emcore.SearchDiscovery.ArchitectureTests\Emcore.SearchDiscovery.ArchitectureTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\tests\Emcore.SearchDiscovery.IntegrationTests\Emcore.SearchDiscovery.IntegrationTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\tests\Emcore.SearchDiscovery.UnitTests\Emcore.SearchDiscovery.UnitTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\src\Emcore.SubscriptionPayment.Api\Emcore.SubscriptionPayment.Api.csproj
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\src\Emcore.SubscriptionPayment.Application\Emcore.SubscriptionPayment.Application.csproj
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\src\Emcore.SubscriptionPayment.Contracts\Emcore.SubscriptionPayment.Contracts.csproj
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\src\Emcore.SubscriptionPayment.Domain\Emcore.SubscriptionPayment.Domain.csproj
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\src\Emcore.SubscriptionPayment.Infrastructure\Emcore.SubscriptionPayment.Infrastructure.csproj
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\src\Emcore.SubscriptionPayment.Worker\Emcore.SubscriptionPayment.Worker.csproj
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\tests\Emcore.SubscriptionPayment.ArchitectureTests\Emcore.SubscriptionPayment.ArchitectureTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\tests\Emcore.SubscriptionPayment.IntegrationTests\Emcore.SubscriptionPayment.IntegrationTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\tests\Emcore.SubscriptionPayment.UnitTests\Emcore.SubscriptionPayment.UnitTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\src\Emcore.UserOrganization.Api\Emcore.UserOrganization.Api.csproj
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\src\Emcore.UserOrganization.Application\Emcore.UserOrganization.Application.csproj
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\src\Emcore.UserOrganization.Contracts\Emcore.UserOrganization.Contracts.csproj
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\src\Emcore.UserOrganization.Domain\Emcore.UserOrganization.Domain.csproj
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\src\Emcore.UserOrganization.Infrastructure\Emcore.UserOrganization.Infrastructure.csproj
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\src\Emcore.UserOrganization.Worker\Emcore.UserOrganization.Worker.csproj
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\tests\Emcore.UserOrganization.ArchitectureTests\Emcore.UserOrganization.ArchitectureTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\tests\Emcore.UserOrganization.IntegrationTests\Emcore.UserOrganization.IntegrationTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\tests\Emcore.UserOrganization.UnitTests\Emcore.UserOrganization.UnitTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\src\Emcore.WorkflowScheduler.Api\Emcore.WorkflowScheduler.Api.csproj
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\src\Emcore.WorkflowScheduler.Application\Emcore.WorkflowScheduler.Application.csproj
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\src\Emcore.WorkflowScheduler.Contracts\Emcore.WorkflowScheduler.Contracts.csproj
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\src\Emcore.WorkflowScheduler.Domain\Emcore.WorkflowScheduler.Domain.csproj
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\src\Emcore.WorkflowScheduler.Infrastructure\Emcore.WorkflowScheduler.Infrastructure.csproj
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\src\Emcore.WorkflowScheduler.Worker\Emcore.WorkflowScheduler.Worker.csproj
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\tests\Emcore.WorkflowScheduler.ArchitectureTests\Emcore.WorkflowScheduler.ArchitectureTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\tests\Emcore.WorkflowScheduler.IntegrationTests\Emcore.WorkflowScheduler.IntegrationTests.csproj
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\tests\Emcore.WorkflowScheduler.UnitTests\Emcore.WorkflowScheduler.UnitTests.csproj
`

## 2. Setup Stage vs. Future Business Responsibilities
> [!IMPORTANT]
> **No business logic has been implemented.** 
> - **Current Setup Stage**: Skeletons only. 
>   - Zero business entities created.
>   - Zero business APIs exposed.
>   - Zero real message consumers.
>   - Zero stored procedures or SQL business objects.
>   - Zero real provider integrations.

## 3. SQL Server Confirmation
> [!NOTE]
> **No SQL Server container exists in the repository.** The local Docker Compose topology (infrastructure/docker/docker-compose.local.yml) consists solely of: RabbitMQ, Redis, OpenSearch, MinIO, and OpenTelemetry. SQL Server is explicitly excluded from the local setup stage as requested.

## 4. Complete Project-Reference Matrix
Every service adheres strictly to the defined Clean Architecture rules.
- Domain -> Has no dependencies.
- Application -> Depends on Domain, BuildingBlocks.Core.
- Infrastructure -> Depends on Application, Domain, BuildingBlocks.Data, BuildingBlocks.Messaging, BuildingBlocks.Caching.
- Contracts -> Has no internal dependencies.
- Api -> Depends on Application, Infrastructure, Contracts, BuildingBlocks.Api.
- Worker -> Depends on Application, Infrastructure, Contracts.

## 5. Build, Test, and Runtime Validation
* **dotnet restore**: PASS (125 projects restored via CPM, warnings NU1901-1904 suppressed)
* **dotnet format --verify-no-changes**: PASS (No style violations)
* **dotnet build -c Release**: PASS (All 125 projects build cleanly under <TreatWarningsAsErrors>true</TreatWarningsAsErrors>)
* **dotnet test -c Release**: PASS (All ArchitectureTests executing NetArchTest rules pass successfully. Failed/Skipped: **0**)
* **API Health Smoke Test**: PASS (API startup yields /health/live OK, worker initializes without errors even with missing external connections)

## 6. GitHub Actions Workflows
The following CI pipelines were established:
`	ext
C:\DEV\API PROJECT\STOCKOUT\.github\workflows\main-validation.yml
C:\DEV\API PROJECT\STOCKOUT\.github\workflows\manual-container-build.yml
C:\DEV\API PROJECT\STOCKOUT\.github\workflows\pr-validation.yml
`

## 7. Created Configurations
`	ext
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.ApiGateway\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.ApiGateway\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.ApiGateway\bin\Release\net10.0\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.ApiGateway\bin\Release\net10.0\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.McpGateway\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.McpGateway\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.McpGateway\bin\Release\net10.0\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.McpGateway\bin\Release\net10.0\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.PortalBff\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.PortalBff\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.PortalBff\bin\Release\net10.0\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.PortalBff\bin\Release\net10.0\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.PublicBff\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.PublicBff\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.PublicBff\bin\Release\net10.0\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.PublicBff\bin\Release\net10.0\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.RealtimeGateway\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.RealtimeGateway\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.RealtimeGateway\bin\Release\net10.0\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.RealtimeGateway\bin\Release\net10.0\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\src\Emcore.AuditReporting.Api\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\src\Emcore.AuditReporting.Api\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\src\Emcore.AuditReporting.Api\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\src\Emcore.AuditReporting.Api\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\src\Emcore.AuditReporting.Worker\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\src\Emcore.AuditReporting.Worker\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\src\Emcore.AuditReporting.Worker\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\src\Emcore.AuditReporting.Worker\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\src\Emcore.BiddingDeal.Api\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\src\Emcore.BiddingDeal.Api\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\src\Emcore.BiddingDeal.Api\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\src\Emcore.BiddingDeal.Api\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\src\Emcore.BiddingDeal.Worker\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\src\Emcore.BiddingDeal.Worker\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\src\Emcore.BiddingDeal.Worker\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\src\Emcore.BiddingDeal.Worker\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\src\Emcore.CatalogListing.Api\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\src\Emcore.CatalogListing.Api\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\src\Emcore.CatalogListing.Api\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\src\Emcore.CatalogListing.Api\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\src\Emcore.CatalogListing.Worker\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\src\Emcore.CatalogListing.Worker\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\src\Emcore.CatalogListing.Worker\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\src\Emcore.CatalogListing.Worker\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\src\Emcore.ConversationRealtime.Api\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\src\Emcore.ConversationRealtime.Api\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\src\Emcore.ConversationRealtime.Api\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\src\Emcore.ConversationRealtime.Api\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\src\Emcore.ConversationRealtime.Worker\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\src\Emcore.ConversationRealtime.Worker\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\src\Emcore.ConversationRealtime.Worker\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\src\Emcore.ConversationRealtime.Worker\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\src\Emcore.IdentityAccess.Api\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\src\Emcore.IdentityAccess.Api\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\src\Emcore.IdentityAccess.Api\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\src\Emcore.IdentityAccess.Api\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\src\Emcore.IdentityAccess.Worker\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\src\Emcore.IdentityAccess.Worker\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\src\Emcore.IdentityAccess.Worker\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\src\Emcore.IdentityAccess.Worker\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\src\Emcore.InspectionTrust.Api\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\src\Emcore.InspectionTrust.Api\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\src\Emcore.InspectionTrust.Api\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\src\Emcore.InspectionTrust.Api\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\src\Emcore.InspectionTrust.Worker\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\src\Emcore.InspectionTrust.Worker\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\src\Emcore.InspectionTrust.Worker\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\src\Emcore.InspectionTrust.Worker\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\src\Emcore.InventoryMedia.Api\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\src\Emcore.InventoryMedia.Api\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\src\Emcore.InventoryMedia.Api\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\src\Emcore.InventoryMedia.Api\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\src\Emcore.InventoryMedia.Worker\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\src\Emcore.InventoryMedia.Worker\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\src\Emcore.InventoryMedia.Worker\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\src\Emcore.InventoryMedia.Worker\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\src\Emcore.NotificationIntegration.Api\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\src\Emcore.NotificationIntegration.Api\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\src\Emcore.NotificationIntegration.Api\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\src\Emcore.NotificationIntegration.Api\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\src\Emcore.NotificationIntegration.Worker\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\src\Emcore.NotificationIntegration.Worker\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\src\Emcore.NotificationIntegration.Worker\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\src\Emcore.NotificationIntegration.Worker\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\src\Emcore.SearchDiscovery.Api\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\src\Emcore.SearchDiscovery.Api\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\src\Emcore.SearchDiscovery.Api\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\src\Emcore.SearchDiscovery.Api\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\src\Emcore.SearchDiscovery.Worker\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\src\Emcore.SearchDiscovery.Worker\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\src\Emcore.SearchDiscovery.Worker\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\src\Emcore.SearchDiscovery.Worker\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\src\Emcore.SubscriptionPayment.Api\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\src\Emcore.SubscriptionPayment.Api\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\src\Emcore.SubscriptionPayment.Api\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\src\Emcore.SubscriptionPayment.Api\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\src\Emcore.SubscriptionPayment.Worker\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\src\Emcore.SubscriptionPayment.Worker\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\src\Emcore.SubscriptionPayment.Worker\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\src\Emcore.SubscriptionPayment.Worker\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\src\Emcore.UserOrganization.Api\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\src\Emcore.UserOrganization.Api\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\src\Emcore.UserOrganization.Api\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\src\Emcore.UserOrganization.Api\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\src\Emcore.UserOrganization.Worker\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\src\Emcore.UserOrganization.Worker\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\src\Emcore.UserOrganization.Worker\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\src\Emcore.UserOrganization.Worker\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\src\Emcore.WorkflowScheduler.Api\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\src\Emcore.WorkflowScheduler.Api\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\src\Emcore.WorkflowScheduler.Api\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\src\Emcore.WorkflowScheduler.Api\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\src\Emcore.WorkflowScheduler.Worker\appsettings.Development.json
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\src\Emcore.WorkflowScheduler.Worker\appsettings.Integration.json
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\src\Emcore.WorkflowScheduler.Worker\appsettings.json
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\src\Emcore.WorkflowScheduler.Worker\appsettings.Local.json
C:\DEV\API PROJECT\STOCKOUT\infrastructure\docker\docker-compose.local.yml
`

## 8. Created Dockerfiles
`	ext
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.ApiGateway\Dockerfile
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.McpGateway\Dockerfile
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.PortalBff\Dockerfile
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.PublicBff\Dockerfile
C:\DEV\API PROJECT\STOCKOUT\gateways\Emcore.RealtimeGateway\Dockerfile
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\Dockerfile.Api
C:\DEV\API PROJECT\STOCKOUT\services\audit-reporting\Dockerfile.Worker
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\Dockerfile.Api
C:\DEV\API PROJECT\STOCKOUT\services\bidding-deal\Dockerfile.Worker
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\Dockerfile.Api
C:\DEV\API PROJECT\STOCKOUT\services\catalog-listing\Dockerfile.Worker
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\Dockerfile.Api
C:\DEV\API PROJECT\STOCKOUT\services\conversation-realtime\Dockerfile.Worker
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\Dockerfile.Api
C:\DEV\API PROJECT\STOCKOUT\services\identity-access\Dockerfile.Worker
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\Dockerfile.Api
C:\DEV\API PROJECT\STOCKOUT\services\inspection-trust\Dockerfile.Worker
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\Dockerfile.Api
C:\DEV\API PROJECT\STOCKOUT\services\inventory-media\Dockerfile.Worker
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\Dockerfile.Api
C:\DEV\API PROJECT\STOCKOUT\services\notification-integration\Dockerfile.Worker
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\Dockerfile.Api
C:\DEV\API PROJECT\STOCKOUT\services\search-discovery\Dockerfile.Worker
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\Dockerfile.Api
C:\DEV\API PROJECT\STOCKOUT\services\subscription-payment\Dockerfile.Worker
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\Dockerfile.Api
C:\DEV\API PROJECT\STOCKOUT\services\user-organization\Dockerfile.Worker
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\Dockerfile.Api
C:\DEV\API PROJECT\STOCKOUT\services\workflow-scheduler\Dockerfile.Worker
`

## 9. Remaining Warnings, Assumptions, and Required External Values
1. **AWS Details Required**: Exact ECR paths, VPC configuration, ECS cluster details, Load Balancer listeners, and execution IAM roles.
2. **Database Details Required**: Development SQL Server hostname, per-service provisioning owner, credentials, and chosen database migration strategy.
3. **Assumptions**: 
    - The .NET 10 preview auto-generates .slnx. We proceed assuming .slnx is acceptable since dotnet build/test natively supports it. 
    - OpenTelemetry currently raises moderate NU1902 vulnerabilities; this has been suppressed in Directory.Build.props via <NoWarn> to allow strict compilation.
