# EMCORE Antigravity Project Setup — Combined Master Document
**Version:** 1.0  
**Scope:** Repository and project foundation only; database and stored procedures deferred.

---

<!-- SOURCE: 00_README_FIRST.md -->

# EMCORE Antigravity Project Setup Pack

**Version:** 1.0  
**Purpose:** Instruct Antigravity to create the EMCORE backend repository and compile-ready skeleton for all 12 services.  
**Scope boundary:** Project setup only. Database creation, tables, migrations, stored procedures, domain API implementation, cloud provisioning and production deployment are deferred.

## Recommended usage

1. Give Antigravity `01_ANTIGRAVITY_MASTER_EXECUTION_PROMPT.md` first.
2. Attach or provide the remaining files as supporting specifications.
3. Tell the agent to execute the work in the target GitHub private monorepository.
4. Require the agent to complete `07_AGENT_RETURN_REPORT_TEMPLATE.md` before stopping.
5. Verify the result against `06_SETUP_ACCEPTANCE_CHECKLIST.md`.

## Files in this pack

| File | Purpose |
|---|---|
| `01_ANTIGRAVITY_MASTER_EXECUTION_PROMPT.md` | Primary implementation prompt and stopping rule |
| `02_PROJECT_SETUP_SCOPE_AND_GUARDRAILS.md` | Included work, exclusions and technical decisions |
| `03_REPOSITORY_AND_PROJECT_STRUCTURE.md` | Exact monorepo, project and dependency structure |
| `04_SHARED_SUPPORT_CLASSES_AND_DEPENDENCIES.md` | Technical building blocks and package requirements |
| `05_LOCAL_CONFIGURATION_ORCHESTRATION_AND_CI.md` | Local services, configuration, Docker/Aspire and GitHub Actions |
| `06_SETUP_ACCEPTANCE_CHECKLIST.md` | Objective completion criteria |
| `07_AGENT_RETURN_REPORT_TEMPLATE.md` | Mandatory information Antigravity must return |
| `08_NEXT_STAGE_HANDOFF.md` | Inputs required before database and API development |
| `EMCORE_ANTIGRAVITY_SETUP_MASTER.md` | Combined copy of the full pack |

## Project decisions already supplied

- Repository: GitHub private monorepository.
- Backend: .NET 10 / ASP.NET Core 10.
- Data access: Dapper with SQL Server stored procedures in later stages.
- Cloud: AWS, UAE region, ECS Fargate for Phase 1.
- CI/CD: GitHub Actions.
- Current environments: Local, Development and Integration.
- SQL Server: existing/managed development SQL Server; developers must not run SQL Server in Docker locally.
- Messaging: self-hosted RabbitMQ.
- Domains: production API `api.stockout.com`; development API `stockout.flowb.io`.
- Architecture: 12 deployable business services plus gateways and workers.

## Important stopping point

Antigravity must stop after the solution builds, tests pass, local infrastructure configuration is prepared and the setup report is written. It must not create business tables, stored procedures or domain APIs in this task.

---

<!-- SOURCE: 01_ANTIGRAVITY_MASTER_EXECUTION_PROMPT.md -->

# Antigravity Master Execution Prompt — EMCORE Backend Project Setup

You are responsible for implementing the **project foundation only** for the EMCORE multi-level material marketplace backend.

Work directly in the assigned GitHub private monorepository. Do not merely produce a plan. Create the files, projects, references, tests, configuration and automation required by this specification.

## 1. Mission

Create a compile-ready .NET 10 monorepository containing:

- All 12 deployable business-service skeletons.
- API and Worker projects for each business service.
- Domain, Application, Infrastructure and Contracts projects for each service.
- API Gateway, Public BFF, Portal BFF, MCP Gateway and Realtime Gateway skeletons.
- Shared technical building blocks.
- Central package and build configuration.
- Local orchestration/configuration without a local SQL Server container.
- GitHub Actions validation workflows.
- Architecture and smoke tests.
- Documentation and a mandatory setup completion report.

## 2. Hard scope boundary

This task is **project setup only**.

Do not create or implement:

- SQL Server databases.
- Business tables, views, functions, user-defined table types or indexes.
- Database migration scripts that create business objects.
- Stored procedures.
- Domain/business APIs such as registration, listing, bidding, delivery, Green Points or payments.
- Business entities or complete business rules.
- AWS infrastructure resources.
- ECS services, production load balancers, Route 53 records or certificates.
- Production secrets.
- Search mappings, RabbitMQ business queues or production workflow definitions.

You may create empty folders, interfaces, options classes, dependency-registration classes, health endpoints, sample/no-op implementations and README placeholders required to prove the architecture compiles.

## 3. Mandatory technical baseline

- Target framework: `net10.0`.
- Runtime: ASP.NET Core 10.
- Language: C# with nullable reference types enabled.
- Repository model: one GitHub private monorepository.
- Architecture: Domain, Application, Infrastructure, Contracts, API and Worker separation per service.
- Data access direction: Dapper + `Microsoft.Data.SqlClient` + stored procedures later.
- Messaging direction: RabbitMQ through MassTransit later.
- Cache/realtime direction: Redis later; SignalR gateway scaffold now.
- Search direction: OpenSearch later.
- Media direction: S3-compatible object storage later.
- Observability: OpenTelemetry-compatible service defaults.
- Deployment target: AWS ECS Fargate in Phase 1.
- CI/CD: GitHub Actions.
- Environments created now: Local, Development and Integration.
- Local SQL rule: do not add SQL Server to Docker Compose or Aspire.

## 4. Execution order

Perform the work in this order:

1. Create root solution/build files.
2. Create shared technical building-block projects.
3. Create the standard service template.
4. Scaffold all 12 business services from the same template.
5. Create gateways and orchestration projects.
6. Add references and enforce dependency rules.
7. Add configuration and local infrastructure definitions.
8. Add health, version and diagnostics endpoints only.
9. Add unit, architecture and smoke-test foundations.
10. Add GitHub Actions workflows.
11. Run restore, format verification, build and tests.
12. Write the mandatory completion report.
13. Stop. Do not continue into database or domain API development.

## 5. Services to create

Create these exact service folders and project namespaces:

1. `identity-access` — `Emcore.IdentityAccess`
2. `user-organization` — `Emcore.UserOrganization`
3. `catalog-listing` — `Emcore.CatalogListing`
4. `inventory-media` — `Emcore.InventoryMedia`
5. `search-discovery` — `Emcore.SearchDiscovery`
6. `bidding-deal` — `Emcore.BiddingDeal`
7. `inspection-trust` — `Emcore.InspectionTrust`
8. `subscription-payment` — `Emcore.SubscriptionPayment`
9. `conversation-realtime` — `Emcore.ConversationRealtime`
10. `notification-integration` — `Emcore.NotificationIntegration`
11. `workflow-scheduler` — `Emcore.WorkflowScheduler`
12. `audit-reporting` — `Emcore.AuditReporting`

Each service must include:

- `<Namespace>.Domain`
- `<Namespace>.Application`
- `<Namespace>.Infrastructure`
- `<Namespace>.Contracts`
- `<Namespace>.Api`
- `<Namespace>.Worker`
- `<Namespace>.UnitTests`
- `<Namespace>.ArchitectureTests`

Create integration-test project folders and placeholders, but do not require live SQL or RabbitMQ for the default test run.

## 6. Gateways and platform projects

Create:

- `Emcore.ApiGateway`
- `Emcore.PublicBff`
- `Emcore.PortalBff`
- `Emcore.McpGateway`
- `Emcore.RealtimeGateway`
- `Emcore.AppHost`
- `Emcore.ServiceDefaults`

The API Gateway may use YARP, but it must contain only placeholder route configuration and no production route assumptions.

## 7. Shared building blocks

Create small focused packages, not one large Common library:

- `Emcore.BuildingBlocks.Core`
- `Emcore.BuildingBlocks.Api`
- `Emcore.BuildingBlocks.Data`
- `Emcore.BuildingBlocks.Messaging`
- `Emcore.BuildingBlocks.Security`
- `Emcore.BuildingBlocks.Observability`
- `Emcore.BuildingBlocks.Caching`
- `Emcore.BuildingBlocks.Storage`
- `Emcore.BuildingBlocks.Idempotency`
- `Emcore.BuildingBlocks.Testing`

Use the support-class requirements in `04_SHARED_SUPPORT_CLASSES_AND_DEPENDENCIES.md`.

## 8. Required service behavior at setup stage

Every API project must expose only:

- `GET /health/live`
- `GET /health/ready`
- `GET /api/v1/system/version`

Every Worker project must:

- Start successfully without a database connection when external dependencies are disabled in Local configuration.
- Register OpenTelemetry/service defaults.
- Contain a no-op hosted service or worker heartbeat implementation.
- Avoid publishing or consuming business events.

Every service must have:

- `appsettings.json`
- `appsettings.Local.json`
- `appsettings.Development.json`
- `appsettings.Integration.json`
- Options validation at startup for required values in non-Local environments.
- A service manifest describing service name, namespace, local port, database logical name and deferred integrations.

## 9. Dapper readiness without database implementation

Add Dapper and SQL client packages only to the Data building block and relevant Infrastructure projects.

Create compile-ready abstractions and support classes:

- `ISqlConnectionFactory`
- `SqlConnectionFactory`
- `SqlDatabaseOptions`
- `IStoredProcedureExecutor`
- `StoredProcedureExecutor`
- `StoredProcedureCommand`
- `DatabaseDependencyState`
- `DatabaseNotConfiguredException`

Rules:

- Do not open a connection at startup.
- Do not execute a query in health checks in Local mode.
- Do not create SQL scripts.
- Do not hardcode connection strings.
- Do not put stored-procedure names in shared building blocks.
- Service-specific repositories will be added later.

## 10. Dependency rules

Enforce:

- Domain has no project reference to Application, Infrastructure, API, Worker or another service.
- Application references Domain and approved building-block abstractions only.
- Infrastructure references Application and Domain.
- Contracts contains transport contracts only and no Infrastructure dependency.
- API references Application, Contracts and Infrastructure only for composition/registration.
- Worker references Application, Contracts and Infrastructure.
- No service references another service's Domain, Application or Infrastructure project.
- Cross-service communication will occur later through versioned Contracts/APIs/events.

Add architecture tests that fail when these rules are violated.

## 11. Local setup

Create local orchestration for:

- RabbitMQ
- Redis
- OpenSearch
- MinIO or another S3-compatible local object store
- OpenTelemetry Collector and a lightweight local telemetry viewer where practical

Do not create a SQL Server container.

External dependencies must be individually switchable using configuration flags so developers can start a single API without running the entire platform.

Create scripts:

- `scripts/bootstrap-local.ps1`
- `scripts/bootstrap-local.sh`
- `scripts/build.ps1`
- `scripts/build.sh`
- `scripts/test.ps1`
- `scripts/test.sh`

The scripts must check prerequisites, print clear errors and avoid installing software silently.

## 12. CI requirements

Create GitHub Actions workflows for:

- Pull-request validation: restore, format check, build, unit tests, architecture tests and dependency vulnerability audit.
- Main-branch validation: repeat quality gates and build Docker images for changed deployable projects.
- Manual container-build workflow: build one selected service/gateway image.

Do not deploy to AWS in this task. Add documented placeholders for later GitHub OIDC role, ECR repositories, ECS cluster and environment secrets.

## 13. Mandatory validation commands

Run and record results for:

```bash
dotnet --info
dotnet restore
dotnet format --verify-no-changes
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
```

Also validate the Docker build for at least:

- Identity Access API
- Identity Access Worker
- API Gateway

If Docker is unavailable, report that fact and provide the exact unexecuted commands.

## 14. Required documentation output

Create/update:

- Root `README.md`
- `docs/architecture/repository-structure.md`
- `docs/architecture/dependency-rules.md`
- `docs/development/local-setup.md`
- `docs/development/configuration.md`
- `docs/development/adding-a-service.md`
- `docs/deployment/ecs-fargate-readiness.md`
- `docs/database/deferred-database-work.md`
- `docs/setup/SETUP_COMPLETION_REPORT.md`

Use `07_AGENT_RETURN_REPORT_TEMPLATE.md` as the report structure.

## 15. Definition of done

The task is complete only when:

- The full solution restores and builds in Release mode.
- Default tests pass without requiring SQL Server.
- Architecture tests enforce project boundaries.
- Each API starts and exposes health/version endpoints.
- Each Worker starts in Local mode without external dependencies.
- Local orchestration excludes SQL Server.
- GitHub Actions YAML files are valid and documented.
- No business database objects or business APIs were created.
- The setup completion report is complete.

After meeting the definition of done, stop and return the completion report plus a concise list of unresolved prerequisites for Stage 2.

---

<!-- SOURCE: 02_PROJECT_SETUP_SCOPE_AND_GUARDRAILS.md -->

# EMCORE Project Setup Scope and Guardrails

## 1. Objective

Establish an implementation-ready backend monorepository for the 12-service EMCORE architecture. The output must make the next development stage predictable while avoiding premature database and business implementation.

## 2. Included in this setup stage

### Repository and solution

- GitHub private monorepository structure.
- Root .NET solution.
- Central package management.
- Shared analyzers, compiler settings and formatting rules.
- Source, tests, contracts, infrastructure and documentation folders.

### Deployable skeletons

- 12 API deployments.
- 12 Worker deployments.
- API Gateway.
- Public BFF.
- Portal BFF.
- MCP Gateway.
- Realtime Gateway.
- Aspire AppHost/service defaults or an equivalent local orchestration structure.

### Technical foundations

- Health and version endpoints.
- Global exception-handler foundation.
- RFC Problem Details foundation.
- Correlation/request/trace ID middleware.
- Configuration/options validation.
- OpenTelemetry instrumentation foundation.
- Dapper and SQL Server support abstractions.
- MassTransit/RabbitMQ support abstractions.
- Redis, object storage and OpenSearch option models.
- Authentication and authorization abstractions without business policy implementation.
- Idempotency and Outbox/Inbox interfaces without database persistence.

### Engineering quality

- Unit-test template.
- Architecture-test template and enforced boundaries.
- Integration-test placeholder that is excluded from dependency-required default runs.
- Dockerfiles for APIs, Workers and gateways.
- GitHub Actions build/test/container templates.
- Local bootstrap scripts.
- Documentation.

## 3. Explicitly excluded

The agent must not create:

- SQL databases or SQL logins.
- Schemas, tables, views, functions, user-defined types or indexes.
- Stored procedures.
- Seed/master data.
- Entity repositories tied to real database objects.
- Domain API endpoints beyond health and version endpoints.
- RabbitMQ business exchanges or queue topology.
- OpenSearch indexes or mappings.
- Redis business keys.
- AWS resources or production deployment.
- Real provider integrations.
- Real JWT signing keys or secrets.
- Product, delivery, Green Points or price-range business logic.

## 4. Architecture decisions

| Area | Decision |
|---|---|
| Backend | .NET 10 and ASP.NET Core 10 |
| Repository | GitHub private monorepository |
| Initial deployment | 12 business services plus gateways and Workers |
| Data access | Dapper with stored procedures in later stage |
| Transactional database | SQL Server; separate logical database per service later |
| Local SQL | No SQL Server Docker container |
| Messaging | Self-hosted RabbitMQ; MassTransit integration foundation |
| Cache/realtime | Redis-compatible cache and SignalR gateway |
| Search | Managed OpenSearch later; local OpenSearch optional for setup |
| Media | S3-compatible storage; MinIO allowed locally |
| Cloud | AWS, UAE region |
| Container platform | ECS Fargate Phase 1 |
| CI/CD | GitHub Actions |
| Environments now | Local, Development, Integration |
| Production API domain | `api.stockout.com` |
| Development API domain | `stockout.flowb.io` |

## 5. Coding standards

- Nullable reference types enabled.
- Warnings treated as errors for project code, with justified exceptions documented.
- Implicit usings enabled unless a project requires otherwise.
- Async methods accept `CancellationToken` where relevant.
- No static service locator.
- No direct use of configuration strings outside typed options.
- No secrets committed to source control.
- No shared marketplace business entities in building-block projects.
- Public transport models must not be reused as database models.
- No direct service-to-service project references.
- No SQL connection opened during application startup.

## 6. Naming rules

### Assembly names

`Emcore.<Capability>.<Layer>`

Examples:

- `Emcore.CatalogListing.Domain`
- `Emcore.CatalogListing.Application`
- `Emcore.CatalogListing.Infrastructure`
- `Emcore.CatalogListing.Contracts`
- `Emcore.CatalogListing.Api`
- `Emcore.CatalogListing.Worker`

### Configuration sections

- `Service`
- `Database`
- `Messaging`
- `Redis`
- `OpenSearch`
- `ObjectStorage`
- `Authentication`
- `Telemetry`
- `FeatureManagement`

### Environment names

Use exactly:

- `Local`
- `Development`
- `Integration`

## 7. Source-control rules

- Trunk-based development.
- `main` must remain releasable.
- Short-lived branches: `feature/*`, `bugfix/*`, `hotfix/*`.
- Pull request required before merge.
- Generated secrets, local user-secrets and build output excluded by `.gitignore`.
- Add `CODEOWNERS` placeholders by service ownership area.
- Add a pull-request template with build/test/security checklist.

## 8. Setup-stage security rules

- No real credentials in repository files.
- Use environment variables, user-secrets and AWS Secrets Manager placeholders.
- Restrict diagnostic details outside Local.
- Create strict default CORS configuration with empty allowed-origin list outside Local until supplied.
- Add rate-limit configuration structure but do not invent production limits.
- Add request-size configuration structure.
- Include security headers middleware foundation where appropriate.
- Health details must not expose secrets or connection strings.

## 9. Change-control rule

When the implementation agent encounters a choice not explicitly defined here, it must:

1. Choose the least-coupled compile-ready option.
2. Record the decision in an ADR or the completion report.
3. Avoid introducing business behavior.
4. Avoid blocking the setup unless a value is genuinely required.
5. List the missing value in the Stage 2 prerequisites section.

---

<!-- SOURCE: 03_REPOSITORY_AND_PROJECT_STRUCTURE.md -->

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

---

<!-- SOURCE: 04_SHARED_SUPPORT_CLASSES_AND_DEPENDENCIES.md -->

# EMCORE Shared Support Classes and Dependencies

## 1. Principle

Shared libraries contain technical cross-cutting capabilities only. They must not contain EMCORE marketplace entities, rules or stored-procedure names.

## 2. `Emcore.BuildingBlocks.Core`

Create compile-ready versions of:

- `Result`
- `Result<T>`
- `Error`
- `ErrorType`
- `DomainException`
- `NotFoundException`
- `ConflictException`
- `ForbiddenException`
- `ValidationException`
- `IClock`
- `SystemClock`
- `IIdGenerator`
- `UlidGenerator` or UUID implementation
- `Guard`

Do not add business error codes.

## 3. `Emcore.BuildingBlocks.Api`

Create:

- `ApiResponse<T>` or a documented decision to use plain responses plus Problem Details.
- `PagedResponse<T>`
- `CursorResponse<T>`
- `ProblemCode`
- `ValidationErrorItem`
- `GlobalExceptionHandler`
- `CorrelationIdMiddleware`
- `RequestIdMiddleware`
- `SecurityHeadersMiddleware`
- `EndpointConventionExtensions`
- `HealthEndpointExtensions`
- `VersionEndpointExtensions`
- `OpenApiExtensions`

Use ASP.NET Core Problem Details. Do not expose stack traces outside Local.

## 4. `Emcore.BuildingBlocks.Data`

Required packages:

- `Dapper`
- `Microsoft.Data.SqlClient`
- `Microsoft.Extensions.Options.ConfigurationExtensions`
- `Microsoft.Extensions.Diagnostics.HealthChecks`

Create:

```csharp
public sealed class SqlDatabaseOptions
{
    public const string SectionName = "Database";
    public string? ConnectionString { get; init; }
    public int CommandTimeoutSeconds { get; init; } = 30;
    public bool Enabled { get; init; }
}
```

Interfaces/classes:

- `ISqlConnectionFactory`
- `SqlConnectionFactory`
- `IStoredProcedureExecutor`
- `StoredProcedureExecutor`
- `StoredProcedureCommand`
- `DatabaseDependencyState`
- `DatabaseNotConfiguredException`
- `DatabaseRegistrationExtensions`

Executor capabilities should be compile-ready for later use:

- `ExecuteAsync`
- `QueryAsync<T>`
- `QuerySingleOrDefaultAsync<T>`
- `QueryMultipleAsync<TResult>`

Rules:

- Require `CommandType.StoredProcedure`.
- Accept cancellation tokens.
- Use explicit timeout.
- Open connections per operation and dispose correctly.
- Do not open a connection during DI registration or startup.
- When `Database:Enabled=false`, database readiness is reported as disabled, not failed, in Local only.
- No service-specific stored-procedure constants in this package.

## 5. `Emcore.BuildingBlocks.Messaging`

Required packages:

- `MassTransit`
- `MassTransit.RabbitMQ`

Create foundations:

- `IntegrationEvent`
- `EventEnvelope<T>`
- `MessageContext`
- `IEventPublisher`
- `IMessageConsumer<T>`
- `IOutboxWriter`
- `IInboxStore`
- `OutboxMessageState`
- `InboxMessageState`
- `MessagingOptions`
- `MessagingDependencyState`
- `NoOpEventPublisher`
- `MessagingRegistrationExtensions`

Do not declare business queues or business event types. Local mode must support `Messaging:Enabled=false`.

## 6. `Emcore.BuildingBlocks.Security`

Create:

- `ICurrentUser`
- `CurrentUserContext`
- `IOrganizationContext`
- `OrganizationContext`
- `IPermissionChecker`
- `PermissionDecision`
- `IServiceIdentity`
- `SensitiveValueMasker`
- `AuthenticationOptions`
- `AuthorizationRegistrationExtensions`

At setup stage, use safe placeholder implementations. Do not invent actual roles, permissions or token issuers.

## 7. `Emcore.BuildingBlocks.Observability`

Required packages should cover:

- OpenTelemetry hosting.
- ASP.NET Core instrumentation.
- HTTP client instrumentation.
- Runtime/process instrumentation where supported.
- OTLP exporter.

Create:

- `TelemetryOptions`
- `TelemetryConstants`
- `ActivitySources`
- `MetricNames`
- `CorrelationContext`
- `ServiceTelemetryExtensions`
- Structured logging helpers.

Every service must emit service name, service version and environment resource attributes.

## 8. `Emcore.BuildingBlocks.Caching`

Required package:

- `StackExchange.Redis`

Create:

- `RedisOptions`
- `ICacheService`
- `NoOpCacheService`
- `RedisCacheService` skeleton
- `CacheKeyBuilder`
- `CacheRegistrationExtensions`

No permanent business state may be defined here.

## 9. `Emcore.BuildingBlocks.Storage`

Required package:

- AWS SDK for S3 or a compatible abstraction package.

Create:

- `ObjectStorageOptions`
- `IObjectStorage`
- `SignedUploadRequest`
- `SignedUploadResult`
- `SignedDownloadResult`
- `ObjectMetadata`
- `NoOpObjectStorage`
- `ObjectStorageRegistrationExtensions`

Do not implement production buckets or access policies.

## 10. `Emcore.BuildingBlocks.Idempotency`

Create:

- `IdempotencyOptions`
- `IIdempotencyStore`
- `IdempotencyRequest`
- `IdempotencyResult`
- `IdempotencyStatus`
- `IdempotencyKeyValidator`
- `NoOpIdempotencyStore`

Persistence will be added after database design.

## 11. `Emcore.BuildingBlocks.Testing`

Create helpers for:

- Test application factory.
- Fake clock.
- Deterministic ID generator.
- Configuration test builder.
- Health endpoint assertions.
- Architecture-test assembly scanning.

## 12. Service-specific support classes

Each service Infrastructure project must include empty/compile-ready service-owned types:

- `<Service>InfrastructureOptions`
- `DependencyInjection`
- `Persistence/README.md`
- `Messaging/README.md`
- `Integrations/README.md`

Each service Application project must include:

- `DependencyInjection`
- `AssemblyReference`
- `I<Service>ApplicationMarker` or equivalent marker.

Each service Contracts project must include:

- `SystemVersionResponse`
- Contract namespace marker.

## 13. Central package management

Use `Directory.Packages.props`. Resolve and pin mutually compatible stable package versions. At minimum include:

- Microsoft ASP.NET Core/OpenAPI packages required by the chosen template.
- Dapper.
- Microsoft.Data.SqlClient.
- MassTransit.
- MassTransit.RabbitMQ.
- StackExchange.Redis.
- OpenTelemetry packages.
- YARP for the API Gateway.
- AWS SDK S3.
- FluentValidation if used.
- Microsoft.Extensions.Http.Resilience.
- xUnit.
- FluentAssertions or equivalent.
- Architecture-test library such as NetArchTest or equivalent.
- Testcontainers packages only in optional integration-test projects.

The completion report must list every resolved version and explain any prerelease package. Prefer stable packages only.

## 14. Disallowed shared code

Do not put any of the following in building blocks:

- Listing, Product, Bid, Deal, Delivery, Inspection, Green Points, Subscription, Payment or Organization entities.
- Service-specific database models.
- Service-specific stored-procedure names.
- Service-specific error codes.
- Service-specific queue names.
- Service-specific permission constants.
- Direct references to another service.

---

<!-- SOURCE: 05_LOCAL_CONFIGURATION_ORCHESTRATION_AND_CI.md -->

# EMCORE Local Configuration, Orchestration and CI

## 1. Environment model

Create configuration support for:

- `Local`
- `Development`
- `Integration`

Do not create UAT, Staging or Production environment files in this task unless they contain documentation-only placeholders.

## 2. Configuration hierarchy

Use this order:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User secrets for Local
4. Environment variables
5. AWS Secrets Manager/parameter provider later

No secret values may be committed.

## 3. Standard service configuration

Each API and Worker should support:

```json
{
  "Service": {
    "Name": "emcore-identity-access-api",
    "Version": "0.1.0",
    "Environment": "Local"
  },
  "Database": {
    "Enabled": false,
    "ConnectionString": null,
    "CommandTimeoutSeconds": 30
  },
  "Messaging": {
    "Enabled": false,
    "Host": "localhost",
    "VirtualHost": "/",
    "Username": null,
    "Password": null
  },
  "Redis": {
    "Enabled": false,
    "ConnectionString": null
  },
  "OpenSearch": {
    "Enabled": false,
    "Endpoint": null
  },
  "ObjectStorage": {
    "Enabled": false,
    "ServiceUrl": null,
    "Bucket": null,
    "AccessKey": null,
    "SecretKey": null
  },
  "Telemetry": {
    "Enabled": true,
    "OtlpEndpoint": null
  }
}
```

Local defaults must allow an API/Worker to start with all external dependencies disabled.

Development and Integration must fail fast when a dependency is marked enabled but mandatory configuration is missing.

## 4. Service manifests

Each service must have `service-manifest.json`:

```json
{
  "serviceKey": "identity-access",
  "namespace": "Emcore.IdentityAccess",
  "apiProject": "Emcore.IdentityAccess.Api",
  "workerProject": "Emcore.IdentityAccess.Worker",
  "localApiPort": 7101,
  "databaseLogicalName": "EMCORE_IDENTITY_DB",
  "databaseProvisioning": "DEFERRED",
  "storedProcedures": "DEFERRED",
  "messagingTopology": "DEFERRED",
  "deploymentTarget": "AWS_ECS_FARGATE"
}
```

Use the appropriate logical database name for every service.

## 5. Logical database names for manifests only

- `EMCORE_IDENTITY_DB`
- `EMCORE_ORGANIZATION_DB`
- `EMCORE_CATALOG_LISTING_DB`
- `EMCORE_INVENTORY_MEDIA_DB`
- `EMCORE_SEARCH_DB`
- `EMCORE_BIDDING_DEAL_DB`
- `EMCORE_INSPECTION_TRUST_DB`
- `EMCORE_SUBSCRIPTION_PAYMENT_DB`
- `EMCORE_CONVERSATION_DB`
- `EMCORE_NOTIFICATION_INTEGRATION_DB`
- `EMCORE_WORKFLOW_DB`
- `EMCORE_AUDIT_REPORTING_DB`

These are metadata only. Do not create them.

## 6. Local Docker Compose

Create `infrastructure/docker/docker-compose.local.yml` with optional profiles for:

- `rabbitmq`
- `redis`
- `opensearch`
- `minio`
- `otel`

Requirements:

- No SQL Server service.
- Named volumes for local dependency data.
- Health checks.
- Ports documented and configurable through `.env`.
- Development credentials only in `.env.example`, clearly marked non-production.
- Compose profiles so each dependency can be started separately.

Suggested ports:

| Dependency | Port |
|---|---:|
| RabbitMQ AMQP | 5672 |
| RabbitMQ management | 15672 |
| Redis | 6379 |
| OpenSearch | 9200 |
| MinIO API | 9000 |
| MinIO console | 9001 |
| OTLP gRPC | 4317 |
| OTLP HTTP | 4318 |

## 7. Aspire AppHost

Create selectable groups rather than one forced all-services launch:

- `foundation`: gateways + local infrastructure.
- `access`: Identity Access and User Organization.
- `marketplace-core`: Catalog Listing, Inventory Media and Inspection Trust.
- `search`: Search Discovery.
- `commercial`: Bidding Deal and Subscription Payment.
- `engagement`: Conversation Realtime and Notification Integration.
- `operations`: Workflow Scheduler and Audit Reporting.

Document how to start only one group.

## 8. Health behavior

### Liveness

`/health/live` checks only process health.

### Readiness

`/health/ready` reports:

- Service application status.
- Dependency enabled/disabled state.
- Dependency connectivity only when enabled.

In Local, disabled dependencies must not prevent readiness. In Development/Integration, enabled-but-unavailable dependencies must fail readiness.

## 9. Dockerfiles

Create multi-stage Dockerfiles for:

- Each API.
- Each Worker.
- Each Gateway/BFF.

Requirements:

- Non-root runtime user.
- Release publish.
- Read-only-friendly filesystem where practical.
- Health check or ECS-compatible health endpoint documented.
- OCI labels for service and version.
- No secrets baked into image.
- Build context optimized with `.dockerignore`.

## 10. GitHub Actions

### Pull request workflow

Trigger on pull requests to `main`.

Steps:

1. Checkout.
2. Set up approved .NET SDK from `global.json`.
3. Restore with lock-file enforcement where configured.
4. Run formatting verification.
5. Build Release.
6. Run unit and architecture tests.
7. Run vulnerable/deprecated package checks.
8. Upload test results.

### Main workflow

Trigger on merge/push to `main`.

- Repeat PR gates.
- Detect changed deployable projects.
- Build corresponding Docker images.
- Tag images with commit SHA and semantic build version.
- Do not push to AWS unless a later stage supplies ECR/OIDC configuration.

### Manual image workflow

Inputs:

- Deployable project/service.
- Image tag.
- Push flag defaulting to false.

## 11. Required repository files

- `.github/workflows/pr-validation.yml`
- `.github/workflows/main-validation.yml`
- `.github/workflows/manual-container-build.yml`
- `.github/pull_request_template.md`
- `.github/dependabot.yml`
- `CODEOWNERS`

## 12. AWS/ECS readiness placeholders

Document but do not provision:

- AWS region variable.
- GitHub OIDC role ARN.
- ECR repository names.
- ECS cluster name.
- ECS service names.
- Task execution role ARN.
- Task role ARN per service or role pattern.
- CloudWatch log group names.
- Load balancer/listener/target-group identifiers.
- Secrets Manager paths.
- Development domain and certificate details.

The completion report must list every missing value.

## 13. Domains

Document routing intent only:

- Production API: `api.stockout.com`
- Development API: `stockout.flowb.io`

Do not modify DNS or certificates.

## 14. Script behavior

Scripts must:

- Detect the repository root.
- Verify .NET SDK compatibility.
- Verify Docker only when a Docker command is requested.
- Print commands and clear next actions.
- Return non-zero exit codes on failure.
- Avoid modifying machine-wide settings.
- Avoid silently installing prerequisites.

---

<!-- SOURCE: 06_SETUP_ACCEPTANCE_CHECKLIST.md -->

# EMCORE Setup Acceptance Checklist

Antigravity must mark each item **PASS**, **FAIL**, **NOT RUN** or **NOT APPLICABLE**, with evidence.

## A. Repository baseline

- [ ] Root solution exists and opens successfully.
- [ ] `global.json` pins the approved .NET 10 SDK feature band/version.
- [ ] Central package management is enabled.
- [ ] `.editorconfig`, `.gitignore` and `.dockerignore` exist.
- [ ] Nullable reference types are enabled.
- [ ] Warnings/analyzers policy is documented.
- [ ] Repository README explains quick start.

## B. Twelve services

For every service:

- [ ] Domain project exists.
- [ ] Application project exists.
- [ ] Infrastructure project exists.
- [ ] Contracts project exists.
- [ ] API project exists.
- [ ] Worker project exists.
- [ ] Unit-test project exists.
- [ ] Architecture-test project exists.
- [ ] Integration-test placeholder exists.
- [ ] API and Worker Dockerfiles exist.
- [ ] Service manifest exists.
- [ ] Database placeholder folders exist with no SQL business objects.

## C. Gateways/orchestration

- [ ] API Gateway exists.
- [ ] Public BFF exists.
- [ ] Portal BFF exists.
- [ ] MCP Gateway exists.
- [ ] Realtime Gateway exists.
- [ ] AppHost exists.
- [ ] ServiceDefaults exists.

## D. Shared building blocks

- [ ] Core package exists.
- [ ] API package exists.
- [ ] Data package exists with Dapper/SP abstractions.
- [ ] Messaging package exists with no-op mode.
- [ ] Security package exists.
- [ ] Observability package exists.
- [ ] Caching package exists.
- [ ] Storage package exists.
- [ ] Idempotency package exists.
- [ ] Testing package exists.
- [ ] No marketplace business entity exists in a building block.

## E. Dependency enforcement

- [ ] Domain projects do not depend on Infrastructure/API/Worker.
- [ ] Application projects do not depend on API/Worker.
- [ ] Contracts do not depend on Infrastructure.
- [ ] No service references another service implementation project.
- [ ] Architecture tests verify the rules.
- [ ] Architecture tests pass.

## F. Runtime foundation

For every API:

- [ ] `/health/live` responds successfully.
- [ ] `/health/ready` responds successfully in Local with dependencies disabled.
- [ ] `/api/v1/system/version` responds successfully.
- [ ] No domain/business endpoint is implemented.

For every Worker:

- [ ] Starts in Local with dependencies disabled.
- [ ] Registers telemetry and health foundation.
- [ ] Does not consume/publish business messages.

## G. Configuration

- [ ] Local configuration exists.
- [ ] Development configuration exists.
- [ ] Integration configuration exists.
- [ ] Secrets are not committed.
- [ ] Options are validated.
- [ ] SQL connection is not opened at startup.
- [ ] No local SQL Server container exists.

## H. Local infrastructure

- [ ] RabbitMQ Compose profile exists.
- [ ] Redis Compose profile exists.
- [ ] OpenSearch Compose profile exists.
- [ ] MinIO/object-storage profile exists.
- [ ] OTEL profile exists.
- [ ] Compose file has health checks.
- [ ] Dependencies can be started separately.

## I. CI/CD

- [ ] PR validation workflow exists.
- [ ] Main validation workflow exists.
- [ ] Manual container workflow exists.
- [ ] Workflows do not deploy to AWS.
- [ ] AWS inputs are documented as placeholders.
- [ ] Pull-request template exists.
- [ ] Dependabot configuration exists.

## J. Build/test evidence

- [ ] `dotnet --info` recorded.
- [ ] `dotnet restore` passed.
- [ ] `dotnet format --verify-no-changes` passed.
- [ ] `dotnet build -c Release --no-restore` passed.
- [ ] `dotnet test -c Release --no-build` passed.
- [ ] Identity API Docker build passed or limitation documented.
- [ ] Identity Worker Docker build passed or limitation documented.
- [ ] API Gateway Docker build passed or limitation documented.

## K. Scope guardrail

- [ ] No database was created.
- [ ] No table/view/function/type/index was created.
- [ ] No stored procedure was created.
- [ ] No production RabbitMQ topology was created.
- [ ] No business API was implemented.
- [ ] No AWS resource was created.
- [ ] No production secret was stored.

## L. Documentation and handoff

- [ ] Repository structure document exists.
- [ ] Dependency-rules document exists.
- [ ] Local-setup document exists.
- [ ] Configuration document exists.
- [ ] Adding-a-service document exists.
- [ ] ECS readiness document exists.
- [ ] Deferred database work document exists.
- [ ] Setup completion report is fully populated.
- [ ] Missing inputs for Stage 2 are clearly listed.

---

<!-- SOURCE: 07_AGENT_RETURN_REPORT_TEMPLATE.md -->

# EMCORE Setup Completion Report

**Agent:**  
**Execution date/time:**  
**Repository:**  
**Branch:**  
**Commit SHA:**  
**Operating system:**  
**Final status:** PASS / PARTIAL / FAILED

## 1. Executive result

Summarize what was created, what was validated and whether the repository is ready for database/API development.

## 2. Scope confirmation

| Scope item | Result | Evidence/path |
|---|---|---|
| Project skeleton only |  |  |
| No database creation |  |  |
| No stored procedures |  |  |
| No domain APIs |  |  |
| No AWS provisioning |  |  |

## 3. Toolchain versions

Include complete output or concise verified values:

| Tool/package | Version |
|---|---|
| .NET SDK |  |
| .NET runtime |  |
| Docker |  |
| Docker Compose |  |
| Git |  |

## 4. Resolved NuGet versions

List every centrally managed package and exact version. Identify any prerelease dependency and why it was necessary.

## 5. Created projects

List every project path grouped by:

- Building blocks.
- Gateways/BFFs.
- Orchestration.
- Each of the 12 services.
- Tests.

Also provide totals:

- Total projects.
- Total deployable APIs.
- Total deployable Workers.
- Total gateways/BFFs.
- Total test projects.

## 6. Project-reference validation

Provide:

- Reference rules implemented.
- Architecture-test library used.
- Test names.
- Test result.
- Any approved exception.

## 7. Runtime endpoint validation

For each API/gateway, report:

| Deployable | Local URL | Liveness | Readiness | Version endpoint |
|---|---|---|---|---|

For Workers, report start/stop validation and whether dependencies were disabled.

## 8. Build and test results

| Command | Result | Duration | Relevant output/error |
|---|---|---:|---|
| `dotnet restore` |  |  |  |
| `dotnet format --verify-no-changes` |  |  |  |
| `dotnet build -c Release --no-restore` |  |  |  |
| `dotnet test -c Release --no-build` |  |  |  |

## 9. Docker validation

| Image/project | Result | Image/tag | Notes |
|---|---|---|---|
| Identity Access API |  |  |  |
| Identity Access Worker |  |  |  |
| API Gateway |  |  |  |

## 10. Local infrastructure

Report:

- Compose profiles created.
- Ports.
- Health-check behavior.
- Exact commands to start each dependency.
- Confirmation that SQL Server is absent.

## 11. Configuration inventory

List every required configuration key by environment. Do not include secret values.

Include:

- Environment-variable naming pattern.
- User-secrets instructions.
- Development/Integration fail-fast rules.
- Disabled dependency behavior.

## 12. GitHub Actions inventory

For each workflow:

- File path.
- Trigger.
- Jobs.
- Required future secrets/variables.
- Validation status.

## 13. AWS/ECS information still required

Return all unresolved values, including where applicable:

- Confirmed AWS region code.
- AWS account ID.
- GitHub OIDC role ARN.
- ECR repository naming convention.
- ECS cluster name.
- ECS service names.
- Task execution role ARN.
- Task role strategy.
- VPC/subnets/security groups.
- Load balancer and listener identifiers.
- CloudWatch log-group naming convention.
- Secrets Manager path convention.
- Development DNS/certificate identifiers.

## 14. Database-stage information still required

Even though database creation is deferred, list what must be supplied next:

- Development SQL Server host/instance.
- Authentication method.
- DBA/provisioning owner.
- Database naming approval.
- Per-service credential strategy.
- Network access from developer machines and ECS.
- Backup/retention expectation.
- Migration tool decision.
- Stored-procedure naming standard approval.
- Dapper transaction and result-contract conventions.

## 15. Decisions and deviations

For every implementation choice not explicitly fixed in the input documents, record:

| Decision | Choice | Reason | Impact | Needs approval? |
|---|---|---|---|---|

## 16. Problems and limitations

List every failed/unexecuted validation and the exact reason. Never hide or downgrade failures.

## 17. Stage 2 readiness verdict

Choose one:

- `READY` — database and first vertical slice can start.
- `READY WITH CONDITIONS` — list conditions.
- `NOT READY` — list blocking issues.

## 18. Recommended next command/task

Provide the precise next Antigravity task to execute, but do not execute it in the current setup task.

---

<!-- SOURCE: 08_NEXT_STAGE_HANDOFF.md -->

# EMCORE Next-Stage Handoff

This file defines the stopping point after project setup and the inputs required before database or business API work begins.

## 1. Setup-stage output

The completed repository should provide:

- Compile-ready skeleton for 12 services.
- Compile-ready gateways and Workers.
- Shared support packages.
- Dapper/stored-procedure execution foundation.
- Local dependency orchestration without SQL Server.
- CI validation.
- Architecture enforcement.
- Setup completion report.

## 2. Next stage: database foundation and first vertical slice

Do not begin until the following are confirmed.

### SQL Server access

- Development SQL Server host/instance.
- Authentication type.
- Network/firewall access.
- DBA owner.
- Database-creation permission/process.
- Connection-string delivery method.
- Local developer connection method without Docker.

### Database conventions

- Final 12 database names.
- Schema naming standard.
- Migration/versioning tool.
- Deployment-job mechanism.
- Stored-procedure naming convention.
- Standard procedure result/error contract.
- Pagination result convention.
- Rowversion/concurrency convention.
- Idempotency table standard.
- Outbox/Inbox table standard.
- Audit columns and retention standard.

### Platform dependencies

- RabbitMQ Development host and credentials.
- RabbitMQ vhost and queue naming convention.
- Redis Development endpoint.
- OpenSearch Development endpoint.
- Object-storage bucket/prefix strategy.
- OTEL collector/observability endpoint.

### Security

- Identity provider decision or self-hosted identity confirmation.
- Token issuer/audience.
- JWT signing-key management.
- Organization/tenant context rule.
- Initial role/permission matrix.
- Development CORS origins.
- Secret-management convention.

## 3. Recommended next implementation slice

The next Antigravity instruction should implement only:

1. Identity Access database foundation.
2. Identity technical tables: idempotency, Outbox and Inbox.
3. First stored procedures for registration/session foundation after field approval.
4. Identity API registration vertical slice.
5. Outbox Relay Worker.
6. User Organization consumer skeleton and Inbox deduplication.
7. Integration and concurrency tests.

Do not begin Catalog/Listing, bidding, delivery, Green Points or pricing-range APIs until the access/organization foundation is proven.

## 4. Required evidence before moving beyond the first slice

- Database migrations run through a controlled job.
- Stored procedures are versioned and tested.
- Dapper mapping is typed.
- No controller accesses Dapper directly.
- Idempotency returns the original result on replay.
- Business change and Outbox event commit atomically.
- Consumer Inbox prevents duplicate effects.
- Trace/correlation IDs flow through API, Outbox and consumer.
- Build, integration tests and deployment smoke checks pass.
