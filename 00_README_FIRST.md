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
