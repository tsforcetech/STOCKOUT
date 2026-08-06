# EMCORE Platform — OpenAPI Contract Change & Refinement Report

**Purpose:** Comprehensive architectural changelog documenting all structural refinements, semantic improvements, and error-model prunings introduced into EMCORE OpenAPI specification generation during the final completion initiative.

---

## 1. Executive Summary of Contract Refinements

During the initial phases of OpenAPI adoption, declarative Swashbuckle operation filters utilized simplistic universal injection rules. This resulted in bloated, inaccurate API specifications where every endpoint arbitrarily advertised identical idempotency headers and exhaustive HTTP error code arrays regardless of real backend behavior. 

This initiative executed systematic contract remediation across all 17 platform hosts, pruning artificial noise and aligning generated specifications strictly with true verified runtime implementations.

---

## 2. Before vs. After Structural Comparisons

### 2.1 Universal Idempotency Header Pruning
* **Previous State (Before Refinement):** Every single API endpoint—including `GET /listings`, `/health`, `/auth/login`, and `/auth/refresh`—was universally injected with an `X-Idempotency-Key` header parameter requirement. This created severe developer confusion regarding whether read-only or ephemeral token exchanges required idempotent deduplication keys.
* **Reconciled State (Current Verified Contract):** `IdempotencyHeaderOperationFilter` dynamically parses HTTP method semantics and controller attributes. Idempotency keys are now documented exclusively on state-modifying business commands (`POST`, `PUT`, `PATCH` across transactions, bidding, invoicing, and asset creations). Authentication endpoints and read queries are explicitly clean of idempotency artifacts.

### 2.2 Precise Error Status Code Mapping
* **Previous State (Before Refinement):** All operations exhibited an automated injection of nine universal error status codes (`400`, `401`, `403`, `404`, `409`, `422`, `429`, `500`, `503`). This falsely implied that public anonymous endpoints could throw `401 Unauthorized`, or simple scalar reads could yield `409 Conflict`.
* **Reconciled State (Current Verified Contract):** `ProblemDetailsOperationFilter` applies high-precision evaluation:
  * `401 Unauthorized` / `403 Forbidden` appear exclusively on methods protected by active JWT authorization policies.
  * `409 Conflict` appears exclusively on stateful mutation endpoints supporting idempotent conflict deduplication.
  * `404 Not Found` is restricted to resource retrieval operations taking persistent entity identifier parameters.
  * All error responses bind structurally to uniform RFC 7807 Problem Details schemas (`https://emcore.platform/errors/{statusCode}`).

### 2.3 Dynamic Try-It-Out Server Base URL Rewrites
* **Previous State (Before Refinement):** Generated JSON contracts fixed their root `servers` array strictly to whatever host originally launched the service process (e.g., `http://localhost:5194`). When viewed through the Central Gateway Swagger UI (`http://localhost:5000/swagger`), executing Try-It-Out commands failed due to browser CORS violations attempting to call port 5194 directly instead of remaining within the secure gateway tunnel.
* **Reconciled State (Current Verified Contract):** `ServerUrlDocumentFilter` leverages `IHttpContextAccessor` to inspect incoming YARP forwarding proxy headers (`X-Forwarded-Host`, `X-Forwarded-Proto`). When accessed via the Central API Gateway portal, contracts dynamically rewrite their base server definitions to `http://localhost:5000/`, routing all Try-It-Out executions seamlessly through central reverse-proxy clusters.

---

## 3. Baseline Contract Export Sign-off

All refined specification behaviors have been verified, compiled via continuous integration test suites, and exported directly into permanent repository baselines under `contracts/openapi/<service-key>/v1/openapi.json`. Automated CI checks lock these files against regression.
