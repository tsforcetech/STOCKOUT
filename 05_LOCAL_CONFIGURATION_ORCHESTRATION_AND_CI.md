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
