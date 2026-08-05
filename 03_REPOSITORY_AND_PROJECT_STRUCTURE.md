# EMCORE Repository and Project Structure

## 1. Root structure

```text
emcore-platform/
├── Emcore.Platform.sln
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── Directory.Build.targets
├── .editorconfig
├── .gitignore
├── .dockerignore
├── CODEOWNERS
├── README.md
├── apps/
├── gateways/
├── orchestration/
├── services/
├── building-blocks/
├── contracts/
├── infrastructure/
├── scripts/
├── tests/
└── docs/
```

## 2. Service folders

```text
services/
├── identity-access/
├── user-organization/
├── catalog-listing/
├── inventory-media/
├── search-discovery/
├── bidding-deal/
├── inspection-trust/
├── subscription-payment/
├── conversation-realtime/
├── notification-integration/
├── workflow-scheduler/
└── audit-reporting/
```

## 3. Standard structure for every service

Example: `services/catalog-listing/`

```text
catalog-listing/
├── src/
│   ├── Emcore.CatalogListing.Domain/
│   ├── Emcore.CatalogListing.Application/
│   ├── Emcore.CatalogListing.Infrastructure/
│   ├── Emcore.CatalogListing.Contracts/
│   ├── Emcore.CatalogListing.Api/
│   └── Emcore.CatalogListing.Worker/
├── tests/
│   ├── Emcore.CatalogListing.UnitTests/
│   ├── Emcore.CatalogListing.ArchitectureTests/
│   └── Emcore.CatalogListing.IntegrationTests/
├── database/
│   ├── migrations/.gitkeep
│   ├── schemas/.gitkeep
│   ├── tables/.gitkeep
│   ├── types/.gitkeep
│   ├── functions/.gitkeep
│   ├── procedures/.gitkeep
│   ├── indexes/.gitkeep
│   └── seed/.gitkeep
├── service-manifest.json
├── Dockerfile.Api
├── Dockerfile.Worker
└── README.md
```

The database folders are placeholders only. No SQL object scripts are allowed in this task.

## 4. Layer folder guidance

### Domain

```text
Domain/
├── Abstractions/
├── Entities/
├── ValueObjects/
├── Enums/
├── Events/
├── Errors/
└── AssemblyReference.cs
```

At setup stage, keep these folders mostly empty. Include only technical base abstractions when they are service-owned. Do not invent business entities.

### Application

```text
Application/
├── Abstractions/
├── Behaviors/
├── Commands/
├── Queries/
├── Validation/
├── DependencyInjection.cs
└── AssemblyReference.cs
```

Add interfaces and pipeline foundations only. No business commands or queries are required.

### Infrastructure

```text
Infrastructure/
├── Persistence/
│   ├── Connections/
│   ├── StoredProcedures/
│   └── Repositories/
├── Messaging/
├── Caching/
├── Search/
├── Storage/
├── Integrations/
├── DependencyInjection.cs
└── AssemblyReference.cs
```

Add dependency-registration and deferred adapter folders. Do not create service-specific repository implementations yet.

### Contracts

```text
Contracts/
├── Api/
├── Events/
├── Webhooks/
├── Realtime/
├── Errors/
└── AssemblyReference.cs
```

Only technical/system contracts such as version responses may be included now.

### API

```text
Api/
├── Endpoints/
│   └── System/
├── Middleware/
├── OpenApi/
├── Configuration/
├── Program.cs
├── appsettings.json
├── appsettings.Local.json
├── appsettings.Development.json
└── appsettings.Integration.json
```

### Worker

```text
Worker/
├── HostedServices/
├── Consumers/
├── Configuration/
├── Program.cs
├── appsettings.json
├── appsettings.Local.json
├── appsettings.Development.json
└── appsettings.Integration.json
```

## 5. Gateways

```text
gateways/
├── Emcore.ApiGateway/
├── Emcore.PublicBff/
├── Emcore.PortalBff/
├── Emcore.McpGateway/
└── Emcore.RealtimeGateway/
```

### API Gateway

- YARP-ready reverse proxy.
- Placeholder routes in Local only.
- Correlation ID forwarding.
- Health/version endpoints.
- No production route configuration.

### Public BFF

- Empty screen-aggregation skeleton.
- Health/version endpoints.
- No public marketplace API implementation.

### Portal BFF

- Empty buyer/seller/inspector/admin aggregation skeleton.
- Health/version endpoints.
- No actor-specific business API implementation.

### MCP Gateway

- Protocol host skeleton and configuration model.
- Empty tool registry.
- No business tools enabled.

### Realtime Gateway

- SignalR registration.
- One non-business diagnostic hub may be created only for local connectivity testing.
- No marketplace groups, authorization rules or business event fan-out.

## 6. Orchestration

```text
orchestration/
├── Emcore.AppHost/
└── Emcore.ServiceDefaults/
```

`Emcore.ServiceDefaults` must centralize:

- OpenTelemetry registration.
- Standard health checks.
- Service discovery defaults.
- Resilience defaults.
- Correlation and request context defaults.

`Emcore.AppHost` must support selective startup profiles so developers do not need to run all 12 APIs and 12 Workers simultaneously.

## 7. Building blocks

```text
building-blocks/
├── Emcore.BuildingBlocks.Core/
├── Emcore.BuildingBlocks.Api/
├── Emcore.BuildingBlocks.Data/
├── Emcore.BuildingBlocks.Messaging/
├── Emcore.BuildingBlocks.Security/
├── Emcore.BuildingBlocks.Observability/
├── Emcore.BuildingBlocks.Caching/
├── Emcore.BuildingBlocks.Storage/
├── Emcore.BuildingBlocks.Idempotency/
└── Emcore.BuildingBlocks.Testing/
```

## 8. Contracts area

```text
contracts/
├── openapi/
├── events/
├── webhooks/
├── realtime/
└── mcp/
```

Add README files explaining future contract versioning. Do not add the 543 domain endpoints during setup.

## 9. Infrastructure area

```text
infrastructure/
├── aws/
│   ├── ecs/
│   ├── ecr/
│   ├── iam/
│   ├── networking/
│   └── README.md
├── docker/
│   ├── docker-compose.local.yml
│   └── env.example
├── observability/
├── rabbitmq/
├── redis/
├── opensearch/
├── object-storage/
└── database/
    └── README.md
```

AWS folders contain documentation/placeholders only. Do not provision resources.

## 10. Dependency graph

```text
Api ────────────────> Application ───────────────> Domain
 │                         │
 ├──> Contracts            └──> approved building-block abstractions
 └──> Infrastructure (composition root only)

Infrastructure ─────> Application + Domain
Worker ─────────────> Application + Contracts + Infrastructure
Domain ─────────────> no infrastructure/framework dependencies
```

## 11. Architecture-test requirements

Tests must detect:

- Domain depending on Infrastructure/API/Worker.
- Application depending on API/Worker.
- Contracts depending on Infrastructure.
- One service referencing another service implementation assembly.
- Building blocks referencing a service project.
- API endpoint code directly referencing Dapper or `SqlConnection`.
- Business entities being added to a generic building-block project.

## 12. Suggested local ports

| Deployable | Port |
|---|---:|
| API Gateway | 7000 |
| Public BFF | 7010 |
| Portal BFF | 7020 |
| MCP Gateway | 7030 |
| Realtime Gateway | 7040 |
| Identity Access API | 7101 |
| User Organization API | 7102 |
| Catalog Listing API | 7103 |
| Inventory Media API | 7104 |
| Search Discovery API | 7105 |
| Bidding Deal API | 7106 |
| Inspection Trust API | 7107 |
| Subscription Payment API | 7108 |
| Conversation Realtime API | 7109 |
| Notification Integration API | 7110 |
| Workflow Scheduler API | 7111 |
| Audit Reporting API | 7112 |

Workers do not need host ports unless diagnostics are explicitly enabled.
