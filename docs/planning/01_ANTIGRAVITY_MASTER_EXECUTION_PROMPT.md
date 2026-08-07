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
