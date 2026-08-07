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
