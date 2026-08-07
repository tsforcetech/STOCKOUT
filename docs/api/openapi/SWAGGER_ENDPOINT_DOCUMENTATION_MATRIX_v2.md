# EMCORE Swagger/OpenAPI Endpoint Matrix (v2 - Hardened)

**Date:** 2026-08-06
**Status:** Verified & Remediated

This matrix documents the verified mapping between Emcore API Gateway registry prefixes, backing services, and their hardened OpenAPI metadata status.

| Service Name | Registry Gateway Prefix(es) | Documentation Contract Path | Verified Status |
|---|---|---|---|
| Identity & Access | `/api/v1/auth`, `/api/v1/identity` | `contracts/openapi/emcore-identity-access-api/v1/openapi.json` | :white_check_mark: |
| User & Organization | `/api/v1/users`, `/api/v1/organizations` | `contracts/openapi/emcore-user-organization-api/v1/openapi.json` | :white_check_mark: |
| Catalog & Listing | `/api/v1/catalog`, `/api/v1/listings` | `contracts/openapi/emcore-catalog-listing-api/v1/openapi.json` | :white_check_mark: |
| Inventory & Media | `/api/v1/inventory`, `/api/v1/media` | `contracts/openapi/emcore-inventory-media-api/v1/openapi.json` | :white_check_mark: |
| Search & Discovery | `/api/v1/search` | `contracts/openapi/emcore-search-discovery-api/v1/openapi.json` | :white_check_mark: |
| Bidding & Deal | `/api/v1/bidding`, `/api/v1/deals` | `contracts/openapi/emcore-bidding-deal-api/v1/openapi.json` | :white_check_mark: |
| Inspection & Trust | `/api/v1/inspection`, `/api/v1/trust` | `contracts/openapi/emcore-inspection-trust-api/v1/openapi.json` | :white_check_mark: |
| Subscription & Payment | `/api/v1/subscription`, `/api/v1/payments` | `contracts/openapi/emcore-subscription-payment-api/v1/openapi.json` | :white_check_mark: |
| Conversation & Realtime | `/api/v1/conversations`, `/api/v1/messages` | `contracts/openapi/emcore-conversation-realtime-api/v1/openapi.json` | :white_check_mark: |
| Notification & Integration | `/api/v1/webhooks` | `contracts/openapi/emcore-notification-integration-api/v1/openapi.json` | :white_check_mark: |
| Workflow & Scheduler | `/api/v1/workflows` | `contracts/openapi/emcore-workflow-scheduler-api/v1/openapi.json` | :white_check_mark: |
| Audit & Reporting | `/api/v1/audit` | `contracts/openapi/emcore-audit-reporting-api/v1/openapi.json` | :white_check_mark: |

## Remediation Checklist

- [x] **Gateway Port Alignment:** ApiGateway verified at deterministic HTTP `5000`.
- [x] **Gateway Multi-Prefix Support:** Registry schema upgraded to `gatewayPrefixes` array, eliminating hidden endpoints.
- [x] **Production Exposure Disabled:** `/openapi/v1.json` and `/swagger/registry` return 404/401 by default in Production environments.
- [x] **False Schema Claims Removed:** HTTP `500` is documented accurately as unformatted exception output without enforcing the RFC 7807 EMCORE Problem Details schema.
- [x] **Idempotency Accuracy:** Mutation `X-Idempotency-Key` headers accurately state they are reserved but currently unenforced by the NoOp store.
- [x] **Missing Validations Corrected:** Automatic 422 and 409 codes removed from endpoints lacking validation or state concurrency paths.
