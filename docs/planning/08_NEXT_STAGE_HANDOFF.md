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
