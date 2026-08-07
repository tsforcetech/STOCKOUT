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
