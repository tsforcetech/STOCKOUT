# 03 Endpoint Inventory (AFTER)

This inventory captures the state of all OpenAPI schemas after refactoring all APIs and Gateways to use MVC Controllers instead of Minimal API endpoints in Program.cs.

## EMCORE Central API Gateway

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Liveness health probe |
| GET | /health/ready | Readiness health probe |
| GET | /health | General health check alias |
| GET | /api/v1/system/version | Gateway runtime version metadata |
| GET | /api/v1/swagger/registry | Centralized OpenAPI document registry |

## EMCORE Audit & Reporting API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

## EMCORE Bidding & Deal API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

## EMCORE Catalog & Listing API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

## EMCORE Conversation & Realtime API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

## EMCORE Identity & Access API

| Method | Path | Summary |
|---|---|---|
| GET | /api/v1/auth/account/status | - |
| GET | /api/v1/identity/me | - |
| POST | /api/v1/identity/admin/users/status | - |
| PUT | /api/v1/identity/admin/users/{id}/status | - |
| POST | /api/v1/auth/register | - |
| POST | /api/v1/auth/login | - |
| POST | /api/v1/auth/token/refresh | - |
| POST | /api/v1/auth/logout | - |
| POST | /api/v1/auth/logout-all | - |
| GET | /api/v1/auth/sessions | - |
| DELETE | /api/v1/auth/sessions/{sessionId} | - |
| POST | /api/v1/auth/mfa/verify | - |
| POST | /api/v1/auth/mfa/register | - |
| POST | /api/v1/auth/mfa/confirm | - |
| POST | /api/v1/auth/stepup/initiate | - |
| POST | /api/v1/auth/stepup/verify | - |
| POST | /api/v1/auth/password/forgot | - |
| POST | /api/v1/auth/password/reset | - |
| POST | /api/v1/auth/password/change | - |
| POST | /api/v1/auth/token | - |
| POST | /api/v1/identity/service-clients/register | - |
| POST | /api/v1/identity/service-clients/{id}/rotate | - |
| POST | /api/v1/identity/service-clients/credentials/revoke | - |
| GET | /api/v1/identity/service-clients/{id}/credentials | - |
| POST | /api/v1/auth/verification/email/send | - |
| POST | /api/v1/auth/verification/email/confirm | - |
| POST | /api/v1/auth/verification/mobile/send | - |
| POST | /api/v1/auth/verification/mobile/confirm | - |

## EMCORE Inspection & Trust API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

## EMCORE Inventory & Media API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

## EMCORE MCP Gateway API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

## EMCORE Notification & Integration API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

## EMCORE Portal BFF API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

## EMCORE Public BFF API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

## EMCORE Realtime Gateway API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

## EMCORE Search & Discovery API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

## EMCORE Subscription & Payment API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

## EMCORE User & Organization API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

## EMCORE Workflow & Scheduler API

| Method | Path | Summary |
|---|---|---|
| GET | /health/live | Instantaneous liveness probe check |
| GET | /health/ready | System runtime dependency readiness probe |
| GET | /api/v1/system/version | Retrieve service deployment release metadata |

