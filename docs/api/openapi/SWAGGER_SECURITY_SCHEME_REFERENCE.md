# EMCORE Platform — Swagger Security Scheme & Idempotency Reference

**Scope:** All EMCORE microservices, public BFF gateways, and central API routing hosts.
**Standard:** RFC 6750 Bearer Tokens, OAuth2/OIDC Token Formats, RFC 7807 Problem Details, and Custom EMCORE Idempotency Protocols.

---

## 1. OpenAPI Security Definitions

The centralized OpenAPI building block (`Emcore.BuildingBlocks.Api/OpenApiExtensions.cs`) injects standard OpenAPI 3.0 security components into all compiled JSON contract specifications.

### 1.1 JWT Bearer Authentication (`Bearer`)
* **Scheme Type:** `http`, Scheme: `bearer`, Bearer Format: `JWT`.
* **Description:** Primary user and operational tenant authentication mechanism. Tokens must be passed in the HTTP Authorization header (`Authorization: Bearer <token>`).
* **Applicability:** Documented automatically on any controller or action method decorated with `[Authorize]`, custom requirement attributes, or tenant security filters.

### 1.2 Inter-Service API Client Secret (`ServiceClientHeader`)
* **Scheme Type:** `apiKey`, In: `header`, Name: `X-Emcore-Service-Client-Key` / `X-Service-Client-Secret`.
* **Description:** Secure server-to-server daemon authentication token utilized for background workflow orchestration and inter-cluster BFF traffic.

---

## 2. Tenant Context & Zero-Trust Verification Header Model

To ensure strict logical tenant separation across SaaS multi-tenant databases without introducing header vulnerability vectors, OpenAPI contracts document organization and tenant metadata headers under a **Zero-Trust Input Model**:

* **`X-Tenant-Id` & `X-Organization-Id` (ULID / UUID Formats):**
  * **Documented Behavior:** These optional or targeted headers allow callers to specify active operational scope when executing requests on multi-tenant domain models.
  * **Security Constraint & Zero-Trust Validation:** The OpenAPI description explicitly highlights that **headers do not confer authorization trust**. ASP.NET Core middleware and application command handlers extract the provided header IDs strictly as target verification inputs. The platform validates that the authenticated subject's JWT claims (`org`, `tenant`, or `role_scopes`) strictly permit access to the requested target ID. If a user tampers with the header without corresponding cryptographic token claims, the gateway immediately returns `403 Forbidden`.

---

## 3. Targeted Idempotency Protocol (`X-Idempotency-Key`)

To eliminate transactional double-execution across flaky distributed networks, EMCORE enforces an idempotent retry architectural protocol. During previous refinement phases, universal idempotency header injection was replaced with high-precision operational rules:

1. **Inclusion Criteria:** The `X-Idempotency-Key` string parameter is documented exclusively as an optional or required header on **State-Modifying Core Operations** (`POST`, `PUT`, `PATCH` operations acting on financial transactions, inventory counts, auction bids, work orders, or user registrations).
2. **Exclusion Criteria:** Idempotency documentation is strictly excluded from:
   * **Read-only queries:** (`GET`, `HEAD`, `OPTIONS`).
   * **Authentication & Ephemeral Token Routes:** (`/auth/login`, `/auth/refresh`, `/auth/stepup`, `/media/upload-urls`).
   * **Operational & Diagnostic Probes:** (`/health`, `/metrics`, `/swagger/*`).
3. **Idempotency Execution Behavior (Documented Contracts):**
   * **First Submission:** Executes full business processing; caches final response body and headers against the provided idempotency key; returns primary success status code (`200 OK`, `201 Created`, or `202 Accepted`).
   * **Duplicate Submission (Same Payload & Key):** Bypasses command execution entirely; retrieves cached result directly; returns the originally stored response code and payload.
   * **Conflict Submission (Modified Payload, Same Key):** Rejects request immediately with `409 Conflict` ("An idempotent request with the specified key has already been executed with differing input parameters").

---

## 4. Rate Limiting & Throttling Specifications

The Central API Gateway and Public BFF gateways enforce protective concurrency and token-bucket throttling limits to prevent abuse and denial-of-service degradation.

* **OpenAPI Response Documentation:** High-traffic endpoints and public authentication interfaces document potential `429 Too Many Requests` error occurrences.
* **Throttling Telemetry Headers:** When rate limits apply, contracts specify that response headers contain standard throttling indicators:
  * `X-RateLimit-Limit`: Maximum allowable requests per time window.
  * `X-RateLimit-Remaining`: Current remaining call allowance within the active window.
  * `X-RateLimit-Reset`: UTC epoch timestamp when the current rate-limit window resets.
  * `Retry-After`: Total delay in seconds required before re-invoking the rejected endpoint.
