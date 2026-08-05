# EMCORE Platform Backend — Developer Onboarding & Architecture Guide

> [!NOTE]  
> **Document Version:** 1.0 (Production & Setup Reference)  
> **Target Audience:** New Software Engineers, DevOps Specialists, QA Specialists, and Architecture Reviewers joining the EMCORE / StockOut team.  
> **Core Framework:** .NET 10 / ASP.NET Core 10 (C# 13+)  
> **Architecture Pattern:** Clean Architecture, Domain-Driven Design (DDD), Microservices  

---

## 👋 Welcome to the EMCORE / StockOut Engineering Team!

This manual serves as your definitive blueprint for understanding how our backend operates. Whether you are adding a new business capability, authoring stored procedures, tuning Dapper queries, or spinning up local .NET Aspire profiles, this guide ensures you build software that aligns with our enterprise reliability, performance, and strict layering standards.

---

## 1. Executive Summary & Core Technology Stack

The **EMCORE Platform** serves as the enterprise cloud-native backend powering **StockOut** (Production API Domain: `api.stockout.com` | Development API Domain: `stockout.flowb.io`). Designed to handle complex marketplace workflows—including live auction bidding, escrow payments, verified grading inspections, real-time chats, and high-resolution inventory catalogs—EMCORE is partitioned into **12 specialized business microservices**, **5 API Gateways/BFFs**, and a centralized orchestration layer.

### Platform Technical Stack

| Layer / Capability | Technology Choice | Architectural Rationale & Guidance |
|---|---|---|
| **Runtime Framework** | `.NET 10.0` / ASP.NET Core 10 | Latest flagship runtime providing superior multi-platform container efficiency, low latency, advanced OpenAPI generation, and OTEL telemetry support. |
| **Architecture Pattern** | Clean Architecture + DDD | Strict boundaries ensuring pure business Domain logic remains totally agnostic to database ORMs, messaging buses, and frameworks. |
| **Data Access** | Dapper + SQL Server | Lightweight micro-ORM utilizing **Stored Procedures exclusively** for secure, database-optimized operations without heavy entity tracking overhead. |
| **Event-Driven Messaging** | MassTransit + RabbitMQ | Asynchronous pub/sub architecture utilizing transactional Inbox and Outbox patterns to guarantee exactly-once message delivery. |
| **Caching & State** | StackExchange.Redis | Distributed in-memory caching for sub-millisecond data retrieval, rate limiting, and ephemeral state persistence. |
| **Search Engine** | OpenSearch | Full-text indexing, multi-faceted marketplace filtering, and sub-second discovery feeds for catalog listings. |
| **Object Storage** | AWS S3 (Prod) / MinIO (Local) | Generates secure cryptographic signed upload/download URLs directly to clients for inspection media, listing imagery, and PDF documents. |
| **Observability & Logs** | OpenTelemetry (OTEL) | Unified distributed tracing, metrics, and structural logging exported via OTLP gRPC/HTTP to monitoring collectors. |
| **Local Orchestration** | .NET Aspire & Docker Compose | Enables modular multi-project debugging profiles without exhausting developer workstation CPU and memory. |

---

## 2. Repository Architecture & Clean Architecture Layering

EMCORE is architected as a **GitHub Private Monorepository** (`emcore-platform/`). We leverage Central Package Management (CPM) via `Directory.Packages.props` and common building directives via `Directory.Build.props` and `global.json`.

### Monorepo Directory Taxonomy

```text
emcore-platform/
├── Emcore.Platform.slnx          # Central solution referencing all services, building blocks, and tests
├── Directory.Packages.props      # Central Package Management version definitions
├── gateways/                     # External doors: API Gateway, BFFs, AI MCP Host, Realtime Hubs
├── orchestration/                # .NET Aspire AppHost and ServiceDefaults
├── services/                     # The 12 autonomous deployable business microservices
├── building-blocks/              # Reusable cross-cutting technical support packages (Zero domain code!)
├── contracts/                    # Centralized system schemas (OpenAPI, Events, Webhooks, MCP)
├── infrastructure/               # AWS ECS specifications, Docker Compose setup, and Terraform docs
├── scripts/                      # Developer onboarding, testing, & PDF compilation utilities
└── docs/                         # Architecture specifications and system manuals
```

### Clean Architecture Dependency Graph

In EMCORE, **dependency flow points strictly inwards**. Business concepts in the Domain never reference infrastructural frameworks, database drivers, or web hosting libraries. Automated architecture tests run via GitHub Actions on every pull request to enforce these immutable rules.

```text
┌─────────────────────────────────────────────────────────────┐
│                      Api & Worker Projects                  │  <-- Entry points & Hosting (Composition Root)
└──────────────────────────────┬──────────────────────────────┘
                               │ references
                               ▼
┌─────────────────────────────────────────────────────────────┐
│              Application & Infrastructure Layers            │  <-- Handlers, Repositories, Dapper Adapters
└──────────────────────────────┬──────────────────────────────┘
                               │ references
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                Domain Layer & Pure Contracts                │  <-- ZERO Infrastructure Dependencies!
└─────────────────────────────────────────────────────────────┘
```

#### Layer Responsibilities
- **Domain Layer:** Enters pure entities, value objects, domain events, domain error definitions, and abstractions. Must **NEVER** reference Dapper, EF, ASP.NET Core, or Infrastructure assemblies.
- **Application Layer:** Contains commands, queries, validation behaviors, and pipeline abstractions. Completely isolated from API web concepts and workers.
- **Infrastructure Layer:** Implements data access repositories using Dapper `IStoredProcedureExecutor`, MassTransit brokers, Redis caches, and S3 SDK integrations.
- **API & Worker Layers:** Composition roots configuring Dependency Injection, middleware, Problem Details exception mapping, and background event consumers.

---

## 3. Microservices Portfolio & Capabilities

The backend is decomposed into **12 autonomous microservices**, each possessing an `.Api` application (REST endpoints, OpenAPI) and a `.Worker` service (MassTransit RabbitMQ event consumers and scheduled timers).

| Service Key | Namespace | Port | Assigned Logical Database | Core Business Capability |
|---|---|---|---|---|
| `identity-access` | `Emcore.IdentityAccess` | **7101** | `EMCORE_IDENTITY_DB` | Authentication, JWT token issuance, security roles, MFA, and permission evaluation. |
| `user-organization` | `Emcore.UserOrganization` | **7102** | `EMCORE_ORGANIZATION_DB` | Corporate tenancies, user account profiles, buyer/seller verification onboarding workflows, and address rosters. |
| `catalog-listing` | `Emcore.CatalogListing` | **7103** | `EMCORE_CATALOG_LISTING_DB` | Foundational core. Manages item classifications, category hierarchies, product specifications, listing lifecycles, and inventory quantities. |
| `inventory-media` | `Emcore.InventoryMedia` | **7104** | `EMCORE_INVENTORY_MEDIA_DB` | Handles product imagery, inspection videos, and generates secure cryptographic signed AWS S3/MinIO upload and download tokens. |
| `search-discovery` | `Emcore.SearchDiscovery` | **7105** | `EMCORE_SEARCH_DB` | Integrates with OpenSearch for sub-millisecond keyword searching, multi-faceted filtering, and relevance recommendations. |
| `bidding-deal` | `Emcore.BiddingDeal` | **7106** | `EMCORE_BIDDING_DEAL_DB` | Executes live real-time auctions, reserve price logic, sealed bids, negotiation workflows, and binding deal agreements. |
| `inspection-trust` | `Emcore.InspectionTrust` | **7107** | `EMCORE_INSPECTION_TRUST_DB` | Enforces marketplace credibility via certified grading, quality compliance reports, trust scoring algorithms, and dispute verification evidence. |
| `subscription-payment` | `Emcore.SubscriptionPayment` | **7108** | `EMCORE_SUBSCRIPTION_PAYMENT_DB` | Manages membership tiers, recurring billing cycles, escrow payment retention, invoice creation, and payment gateway abstractions. |
| `conversation-realtime` | `Emcore.ConversationRealtime` | **7109** | `EMCORE_CONVERSATION_DB` | Facilitates live negotiation chat rooms between buyers and sellers, coordinating with the Realtime Gateway for SignalR WebSocket delivery. |
| `notification-integration`| `Emcore.NotificationIntegration`| **7110** | `EMCORE_NOTIFICATION_INTEGRATION_DB` | Multi-channel dispatch hub for transactional email, SMS OTPs, mobile push notifications, and external client webhooks. |
| `workflow-scheduler` | `Emcore.WorkflowScheduler`| **7111** | `EMCORE_WORKFLOW_DB` | Coordinates distributed background jobs, cron-style automated sweeps (e.g., expiring old auctions, batch invoicing), and saga orchestration. |
| `audit-reporting` | `Emcore.AuditReporting` | **7112** | `EMCORE_AUDIT_REPORTING_DB` | Maintains tamper-resistant audit logs of user administrative actions, financial transactions, and executive reporting feeds. |

---

## 4. Gateways & Backend-For-Frontend (BFF) Architecture

To secure internal microservices and optimize client experiences, EMCORE exposes **no internal microservice ports to external clients directly**. All ingress traffic flows through gateways situated in `gateways/`.

| Gateway Project | Port | Purpose & Target Consumers |
|---|---|---|
| **Emcore.ApiGateway** | **7000** | Built on high-performance Microsoft **YARP** (Yet Another Reverse Proxy). Acts as the primary API ingress, enforcing correlation ID injection, rate limiting, SSL termination, and systemic route health checks. |
| **Emcore.PublicBff** | **7010** | **Backend-for-Frontend** optimized for public, unauthenticated browsing. Aggregates category trees, promotional banners, and catalog search results into streamlined mobile/web views. |
| **Emcore.PortalBff** | **7020** | **Backend-for-Frontend** tailored for authenticated domain actors (Buyers, Sellers, Inspectors, Admins). Aggregates private bidding dashboards, escrow status, and analytics into unified API payloads. |
| **Emcore.McpGateway** | **7030** | Dedicated **Model Context Protocol (MCP)** host server. Exposes standardized AI tool registries and schemas, enabling trusted AI agents (such as Google Antigravity) to securely query and operate platform capabilities. |
| **Emcore.RealtimeGateway** | **7040** | Dedicated **SignalR WebSocket Hub Server**. Maintains persistent real-time connections to client applications, pushing instantaneous out-bid notifications, live negotiation chats, and operational alerts. |

---

## 5. Technical Building Blocks

Our modular architecture avoids repeating cross-cutting infrastructural code across microservices. Reusable technical capabilities reside in `building-blocks/` as modular libraries. 
> [!IMPORTANT]  
> **CRITICAL RULE:** Building blocks must contain **ZERO** domain concepts (no Listing, Bid, Deal, User, or Organization entities!).

- **`Emcore.BuildingBlocks.Core`**: Foundations including functional `Result<T>` wrappers, semantic exception abstractions (`DomainException`, `NotFoundException`, `ConflictException`), deterministic Ulid generation, and testable `IClock` services.
- **`Emcore.BuildingBlocks.Api`**: Standardized ASP.NET Core Problem Details error formatting, `GlobalExceptionHandler`, structured pagination envelopes (`PagedResponse<T>`, `CursorResponse<T>`), security headers, and Correlation ID middleware.
- **`Emcore.BuildingBlocks.Data`**: **The Heart of Data Access.** Wraps `Dapper` and `Microsoft.Data.SqlClient`. Enforces `ISqlConnectionFactory` and `IStoredProcedureExecutor`. Requires that all relational operations run via Stored Procedures with explicit command timeouts and cancellation token safety.
- **`Emcore.BuildingBlocks.Messaging`**: Event bus abstractions over `MassTransit` and RabbitMQ (`IntegrationEvent` envelopes). Provides transactional **Outbox and Inbox store pattern abstractions** to guarantee zero-data-loss and idempotent processing.
- **`Emcore.BuildingBlocks.Security`**: Identity context evaluation (`ICurrentUser`, `IOrganizationContext`), dynamic permission checking (`IPermissionChecker`), and automated sensitive data masking for safe diagnostic logging.
- **`Emcore.BuildingBlocks.Observability`**: Centralizes OpenTelemetry (OTEL) tracing, metrics, and structured logging. Ensures every request across HTTP, MassTransit, and Dapper generates distributed trace spans and uniform resource attributes.
- **`Emcore.BuildingBlocks.Caching`**: Distributed caching abstraction using `StackExchange.Redis` with robust failover capabilities and standardized cache-key builders.
- **`Emcore.BuildingBlocks.Storage`**: Cloud-agnostic object storage interface over AWS SDK S3 and local MinIO. Handles generating secure cryptographic signed upload/download tokens (`SignedUploadRequest`).
- **`Emcore.BuildingBlocks.Idempotency`**: Provides verification engines (`IdempotencyKeyValidator`) that prevent accidental duplicate execution of critical operations (e.g., auction bidding and payment submission).
- **`Emcore.BuildingBlocks.Testing`**: Standarized test application factories, deterministic mock clocks, in-memory configuration builders, and automated assembly scanners for architectural compliance verification.

---

## 6. Local Development Setup & Orchestration

Running all 24 microservice applications (12 APIs + 12 Workers) plus 5 gateways simultaneously on a developer machine is unnecessary and resource-intensive. We integrate **.NET Aspire (`Emcore.AppHost`)** and **Docker Compose profiles** to streamline development.

### A. Infrastructure Dependencies (Docker Compose)

Located in `infrastructure/docker/docker-compose.local.yml`, our Compose setup leverages selective profiles so you only start what your immediate debugging session requires.
> [!NOTE]  
> **Why is SQL Server not in Docker Compose?** We intentionally do not host SQL Server in local Docker containers! Developers connect to a shared, centrally-managed Development SQL Server instance to guarantee uniform stored procedure schema synchronicity across the team.

| Container Profile | Local Host Port(s) | Role in Local Environment |
|---|---|---|
| `rabbitmq` | **5672** (AMQP) \| **15672** (Web UI) | Message broker for MassTransit event bus integration and queue monitoring. |
| `redis` | **6379** | In-memory cache for fast lookups, token persistence, and rate limiting. |
| `opensearch` | **9200** | Local search cluster for catalog indexing and faceted marketplace filtering. |
| `minio` | **9000** (API) \| **9001** (Web Console) | Local S3-compatible object storage for testing media image signed uploads. |
| `otel` | **4317** (gRPC) \| **4318** (HTTP) | OpenTelemetry Collector for capturing distributed traces, spans, and metrics locally. |

### B. .NET Aspire Selective Launch Groups

Instead of hitting F5 on the entire solution, `Emcore.AppHost` supports targeted group execution. Specify your target subsystem profile when debugging:

| Aspire Group Profile | Services Launched | Use Case & Feature Area Focus |
|---|---|---|
| `foundation` | Gateways (API Gateway, BFFs) + Local Infra | Gateway routing, YARP proxy validation, AI MCP agent tools integration. |
| `access` | Identity Access, User Organization | Authentication workflows, user sign-up, JWT tokens, tenant role governance. |
| `marketplace-core` | Catalog Listing, Inventory Media, Inspection Trust | Core inventory lifecycle, S3 signed uploads, trust inspection reports. |
| `search` | Search Discovery + OpenSearch | Search index synchronization, multi-faceted filter testing, discovery queries. |
| `commercial` | Bidding Deal, Subscription Payment | Live real-time auctions, bidding logic, billing invoices, payment simulation. |
| `engagement` | Conversation Realtime, Notification Integration | Live chat rooms, SMS/Email outbox queues, SignalR websocket notification push. |
| `operations` | Workflow Scheduler, Audit Reporting | Background scheduled cron sagas, administrative tamper-resistant auditing trails. |

### C. Configuration & Health Checks
- **Configuration Hierarchy:** `appsettings.json` ➔ `appsettings.{Environment}.json` ➔ User Secrets (Local Only) ➔ Environment Variables ➔ AWS Secrets Manager.
- **Out-of-the-Box Compilation:** In the `Local` environment, services are preconfigured with external connections (Database, Redis, Messaging) set to `Enabled = false`. This guarantees new engineers can compile and run APIs immediately without facing database connection timeouts!
- **Standardized Health Probes:** Every deployable registers two core endpoints:
  - `/health/live`: Process runtime liveness verification.
  - `/health/ready`: Readiness probe validating active connectivity to enabled external dependencies (SQL Server, RabbitMQ, Redis). In Local mode, disabled dependencies are cleanly bypassed without signaling readiness failure.

---

## 7. CI/CD Pipelines & Developer Golden Rules

### Continuous Integration Pipelines (`.github/workflows/`)
1. **PR Validation (`pr-validation.yml`):** Triggered on every pull request to `main`. Enforces SDK lock-file checks, code formatting verification, compiles in Release mode, executes unit/architecture tests, and performs NuGet dependency vulnerability scans.
2. **Main Build & Tag (`main-validation.yml`):** Triggered upon merging to `main`. Repeats validation gates, identifies modified deployable microservices, compiles optimized multi-stage OCI Docker images under a non-root runtime user, and tags them with commit SHAs and semantic build numbers.
3. **Manual Container Build (`manual-container-build.yml`):** Allows on-demand manual container image compilation and optional deployment pushing for specific deployable projects.

---

### 🏆 EMCORE Engineering Golden Rules

To preserve system architecture integrity, every team member must strictly observe these rules:

1. **ZERO Inline SQL in Endpoints or Workers:** All relational database operations MUST flow through `IStoredProcedureExecutor` inside the Infrastructure layer utilizing explicit SQL Stored Procedures.
2. **NEVER Leak Business Entities into Building Blocks:** `Emcore.BuildingBlocks.*` libraries must remain completely generic technical utilities. Any code referencing auctions, items, or user accounts belongs inside a microservice domain!
3. **Respect Microservice Boundary Isolation:** Service A cannot directly invoke Service B's implementation assemblies or query Service B's database tables. Cross-service workflows MUST occur asynchronously via MassTransit integration event publishing or HTTP Gateway endpoints.
4. **NO Secrets in Version Control:** Never commit database credentials, encryption keys, or API passwords in configuration files. Use .NET User Secrets locally (`dotnet user-secrets set ...`).
5. **Preserve Correlation Contexts:** Always ensure OpenTelemetry distributed trace headers and Correlation IDs are propagated across HTTP headers and event message envelopes to ensure end-to-end observability across microservice hops.

---

### 🚀 Quick Start Checklist for Onboarding
- [ ] Ensure your machine has the `.NET 10.0 SDK` installed (check `global.json`) and **Docker Desktop** running.
- [ ] Open a terminal in the root directory and execute `dotnet build Emcore.Platform.slnx` to confirm zero-error compilation.
- [ ] Verify test execution by running `dotnet test Emcore.Platform.slnx`.
- [ ] Spin up local dependency containers as needed:  
  ```bash
  docker compose -f infrastructure/docker/docker-compose.local.yml --profile rabbitmq --profile redis up -d
  ```
- [ ] Obtain Development SQL Server access credentials and AWS test IAM roles from your Team Lead or designated `CODEOWNERS`.

---

*EMCORE Platform Backend Ecosystem — Built with precision by the StockOut Engineering & Antigravity AI Team.*
