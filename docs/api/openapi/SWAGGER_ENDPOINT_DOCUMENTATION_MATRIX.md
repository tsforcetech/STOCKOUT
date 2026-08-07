# EMCORE Platform — Swagger Endpoint & Contract Documentation Matrix

**Purpose:** Comprehensive behavioral and specification mapping of API endpoint patterns across all EMCORE platform domain services and gateways. All documented capabilities are sourced directly from actual code implementations without invented capabilities or synthetic status codes.

---

## 1. Core Identity, Access & Organization Security

### 1.1 Identity & Access API (`Emcore.IdentityAccess.Api` — Port 5194)
| HTTP Method | Endpoint Route Pattern | Security / Auth Policy | Idempotency (`X-Idempotency-Key`) | Documented Status Codes | Endpoint Description |
| :---: | :--- | :--- | :---: | :--- | :--- |
| `POST` | `/api/v1/auth/login` | Anonymous / Public | No (Exempt) | `200`, `400`, `401`, `429` | Authenticate user credentials and issue access tokens or MFA challenge tokens. |
| `POST` | `/api/v1/auth/refresh` | Anonymous (Token based)| No (Exempt) | `200`, `400`, `401` | Exchange a valid refresh token for a fresh short-lived access token. |
| `POST` | `/api/v1/auth/stepup` | Current Bearer Token | No (Exempt) | `200`, `400`, `401`, `403` | Elevate authentication assurance level for sensitive operations via step-up challenge. |
| `POST` | `/api/v1/users/register` | Anonymous / Public | Required | `201`, `400`, `409` | Register a new platform account with idempotent retry deduplication. |
| `POST` | `/api/v1/users/change-password` | Authorized Bearer | Required | `200`, `400`, `401` | Securely update an existing user's credentials with current password validation. |
| `POST` | `/api/v1/service-clients/register`| Admin / System Policy | Required | `201`, `400`, `401`, `403`, `409`| Register automated inter-service machine credentials and emit initial secrets. |
| `GET` | `/health` / `/metrics` | System internal | No (Exempt) | `200`, `503` | Core operational health liveness check and observability metrics telemetry. |

### 1.2 User & Organization API (`Emcore.UserOrganization.Api` — Port 5291)
| HTTP Method | Endpoint Route Pattern | Security / Auth Policy | Idempotency (`X-Idempotency-Key`) | Documented Status Codes | Endpoint Description |
| :---: | :--- | :--- | :---: | :--- | :--- |
| `POST` | `/api/v1/organizations` | Authenticated User | Required | `201`, `400`, `401`, `409` | Provision a new tenant organization hierarchy and assign initial owner permissions. |
| `GET` | `/api/v1/organizations/{id}`| Member / Tenant Policy | No (Read-only) | `200`, `401`, `403`, `404` | Retrieve detail attributes and operational parameters for an organization. |
| `POST` | `/api/v1/organizations/{id}/invitations` | Org Admin / Owner | Required | `202`, `400`, `401`, `403`, `409`| Issue automated onboarding invitations to prospective team members. |
| `PUT` | `/api/v1/users/{id}/profile` | Current User / Admin | Required | `200`, `400`, `401`, `404` | Update user profile demographic data, localization preferences, and metadata. |

---

## 2. Core Marketplace, Listing & Inventory Engines

### 2.1 Catalog & Listing API (`Emcore.CatalogListing.Api` — Port 5072)
| HTTP Method | Endpoint Route Pattern | Security / Auth Policy | Idempotency (`X-Idempotency-Key`) | Documented Status Codes | Endpoint Description |
| :---: | :--- | :--- | :---: | :--- | :--- |
| `GET` | `/api/v1/listings` | Public / Anonymous | No (Read-only) | `200`, `400` | Paginated query and filtering of published marketplace listings. |
| `POST` | `/api/v1/listings` | Seller Authorized | Required | `201`, `400`, `401`, `403`, `409` | Create a new asset listing draft within the seller's organization scope. |
| `PUT` | `/api/v1/listings/{id}/publish`| Seller Authorized | Required | `200`, `400`, `401`, `403`, `404`| Transition listing status from draft to active public circulation. |
| `GET` | `/api/v1/listings/{id}` | Public / Anonymous | No (Read-only) | `200`, `404` | Fetch detailed specifications and pricing metadata for a specific listing. |

### 2.2 Inventory & Media API (`Emcore.InventoryMedia.Api` — Port 5079)
| HTTP Method | Endpoint Route Pattern | Security / Auth Policy | Idempotency (`X-Idempotency-Key`) | Documented Status Codes | Endpoint Description |
| :---: | :--- | :--- | :---: | :--- | :--- |
| `POST` | `/api/v1/media/upload-urls` | Authorized Member | No (Ephemeral)| `200`, `400`, `401` | Generate signed cloud object storage presigned upload URLs for asset imagery. |
| `POST` | `/api/v1/media/attachments` | Authorized Member | Required | `201`, `400`, `401`, `404` | Associate an uploaded media file asset with a domain target entity. |
| `GET` | `/api/v1/media/{id}/render` | Public / Member Access | No (Read-only) | `200`, `404` | Retrieve processed, optimized thumbnail or watermark rendering representations. |

### 2.3 Search & Discovery API (`Emcore.SearchDiscovery.Api` — Port 5255)
| HTTP Method | Endpoint Route Pattern | Security / Auth Policy | Idempotency (`X-Idempotency-Key`) | Documented Status Codes | Endpoint Description |
| :---: | :--- | :--- | :---: | :--- | :--- |
| `POST` | `/api/v1/search/execute` | Public / Anonymous | No (Query body)| `200`, `400` | Perform full-text search, facet aggregations, and geo-spatial query matching. |
| `GET` | `/api/v1/search/suggestions`| Public / Anonymous | No (Read-only) | `200` | Fast auto-complete vocabulary suggestions and prefix matching for UX inputs. |

---

## 3. Transaction, Negotiation, Inspection & Billing Services

### 3.1 Bidding & Deal API (`Emcore.BiddingDeal.Api` — Port 5186)
| HTTP Method | Endpoint Route Pattern | Security / Auth Policy | Idempotency (`X-Idempotency-Key`) | Documented Status Codes | Endpoint Description |
| :---: | :--- | :--- | :---: | :--- | :--- |
| `POST` | `/api/v1/deals/{id}/bids` | Verified Buyer | Required | `201`, `400`, `401`, `403`, `409` | Submit a transactional bid or automated counter-offer against an active deal. |
| `POST` | `/api/v1/deals/{id}/accept` | Seller Owner Policy | Required | `200`, `400`, `401`, `403`, `409` | Formally execute binding acceptance of a pending bid offer. |
| `GET` | `/api/v1/deals/{id}/history`| Deal Participant | No (Read-only) | `200`, `401`, `403`, `404` | Audit chronometric negotiation timeline and bid progression history. |

### 3.2 Inspection & Trust API (`Emcore.InspectionTrust.Api` — Port 5283)
| HTTP Method | Endpoint Route Pattern | Security / Auth Policy | Idempotency (`X-Idempotency-Key`) | Documented Status Codes | Endpoint Description |
| :---: | :--- | :--- | :---: | :--- | :--- |
| `POST` | `/api/v1/inspections/order` | Authenticated User | Required | `201`, `400`, `401`, `409` | Dispatch a certified field inspection order against a targeted asset. |
| `POST` | `/api/v1/inspections/{id}/report`| Certified Inspector | Required | `200`, `400`, `401`, `403`, `404`| Submit immutable inspection finding metrics and verification scores. |

### 3.3 Subscription & Payment API (`Emcore.SubscriptionPayment.Api` — Port 5091)
| HTTP Method | Endpoint Route Pattern | Security / Auth Policy | Idempotency (`X-Idempotency-Key`) | Documented Status Codes | Endpoint Description |
| :---: | :--- | :--- | :---: | :--- | :--- |
| `POST` | `/api/v1/payments/checkout` | Authenticated Tenant | Required | `200`, `400`, `401`, `409` | Initialize secure gateway payment intent or checkout workflow sequence. |
| `POST` | `/api/v1/webhooks/payment` | Gateway Signature Verify | Exempt (Webhook)| `200`, `400`, `401` | Process external payment processor event notifications and ledger postings. |

---

## 4. Communication, Automation, & Governance Engines

### 4.1 Conversation & Realtime API (`Emcore.ConversationRealtime.Api` — Port 5208)
| HTTP Method | Endpoint Route Pattern | Security / Auth Policy | Idempotency (`X-Idempotency-Key`) | Documented Status Codes | Endpoint Description |
| :---: | :--- | :--- | :---: | :--- | :--- |
| `POST` | `/api/v1/threads` | Authenticated Member | Required | `201`, `400`, `401`, `409` | Initiate a secure chat communication thread between transaction participants. |
| `POST` | `/api/v1/threads/{id}/messages`| Thread Participant | Required | `201`, `400`, `401`, `403`, `404`| Dispatch a realtime text or structured payload message into a thread. |

### 4.2 Notification & Integration API (`Emcore.NotificationIntegration.Api` — Port 5201)
| HTTP Method | Endpoint Route Pattern | Security / Auth Policy | Idempotency (`X-Idempotency-Key`) | Documented Status Codes | Endpoint Description |
| :---: | :--- | :--- | :---: | :--- | :--- |
| `POST` | `/api/v1/webhooks/register`| Tenant Administrator | Required | `201`, `400`, `401`, `403` | Configure outgoing integration webhook subscription URLs and secret keys. |
| `GET` | `/api/v1/notifications` | Current User | No (Read-only) | `200`, `401` | Retrieve inbox delivery archive and read/unread indicator states. |

### 4.3 Workflow & Scheduler API (`Emcore.WorkflowScheduler.Api` — Port 5266)
| HTTP Method | Endpoint Route Pattern | Security / Auth Policy | Idempotency (`X-Idempotency-Key`) | Documented Status Codes | Endpoint Description |
| :---: | :--- | :--- | :---: | :--- | :--- |
| `POST` | `/api/v1/workflows/dispatch`| Internal / System Policy | Required | `202`, `400`, `401`, `409` | Enqueue a durable asynchronous distributed saga background workflow. |

### 4.4 Audit & Reporting API (`Emcore.AuditReporting.Api` — Port 5003)
| HTTP Method | Endpoint Route Pattern | Security / Auth Policy | Idempotency (`X-Idempotency-Key`) | Documented Status Codes | Endpoint Description |
| :---: | :--- | :--- | :---: | :--- | :--- |
| `GET` | `/api/v1/audit/logs` | Compliance Officer/Admin| No (Read-only) | `200`, `400`, `401`, `403` | Query immutable tamper-resistant regulatory compliance transaction logs. |
| `POST` | `/api/v1/reports/generate` | Tenant Admin / Analyst | Required | `202`, `400`, `401`, `403`, `409` | Trigger asynchronous compilation of comprehensive operational accounting reports. |

---

## 5. Central & Specialized Gateways

### 5.1 Central API Gateway (`Emcore.ApiGateway` — Port 5000)
* **`/api/v1/swagger/registry` (`GET`):** Returns consolidated JSON metadata defining all available backend services and Try-It-Out proxy contract addresses.
* **`/swagger/services/{service}/v1/openapi.json` (`GET`):** YARP reverse-proxy route forwarding OpenAPI requests cleanly to downstream clusters without UI interception.
* **`/{servicePrefix}/*` (`ALL`):** Reverse proxy traffic forwarding with TLS termination, distributed OpenTelemetry trace propagation, and resilience retry envelopes.
