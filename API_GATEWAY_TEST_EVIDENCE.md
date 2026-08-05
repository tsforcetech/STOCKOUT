# EMCORE API Gateway Test Evidence Report

## Executive Summary
This document serves as definitive proof of automated verification for `Emcore.ApiGateway`. All test cases execute against self-hosted, in-memory Kestrel test servers utilizing actual HTTP client network serialization, W3C OpenTelemetry tracking, and YARP reverse proxy forwarding.

- **Test Framework**: xUnit with FluentAssertions
- **Runtime Target**: `.NET 10.0` (Release Build)
- **Total Test Cases**: **16 Passed**, **0 Failed**, **0 Skipped**
- **Artifact Evidence Report**: `artifacts/test-results/Emcore.ApiGateway.Tests.trx`

---

## Complete Test Verification Matrix

| # | Test Method Name | Verification Objective | Assertion Result |
| :--- | :--- | :--- | :--- |
| **1** | `Gateway_Starts_Successfully_And_Returns_Version` | Confirms the gateway application boots successfully without dependency errors and exposes `/api/v1/system/version`. | **PASSED** |
| **2** | `Liveness_And_Readiness_Return_Expected_Status` | Proves diagnostic monitoring endpoints `/health/live` and `/health/ready` return HTTP 200 OK and valid JSON representations without data leakage. | **PASSED** |
| **3** | `Identity_And_Organization_Routes_Forward_Correctly` | Proves requests directed to `/api/v1/auth/*` and `/api/v1/organizations/*` are accurately forwarded to their respective downstream mock server clusters. | **PASSED** |
| **4** | `Authorization_Header_Is_Forwarded` | Verifies that client `Authorization: Bearer <token>` credentials remain completely intact and are forwarded to downstream microservices for re-validation. | **PASSED** |
| **5** | `Request_And_Correlation_Ids_Are_Generated_Preserved_And_Returned` | Proves that missing `X-Request-Id` and `X-Correlation-Id` headers are generated automatically, existing client IDs are preserved without mutation, and both IDs are echoed in client response headers. | **PASSED** |
| **6** | `Unknown_Route_Returns_404_Problem_Details` | Proves queries targeting non-existent route paths return HTTP 404 formatted as RFC 7807 problem details (`unmatched_gateway_route`). | **PASSED** |
| **7** | `Unavailable_Destination_Returns_Controlled_502_Or_503` | Simulates downstream service downtime and verifies the gateway intercepts target errors, returning controlled HTTP 502/503 problem details without stack traces. | **PASSED** |
| **8** | `Timeout_Returns_Controlled_Result` | Verifies that when a downstream destination exceeds Kestrel/YARP activity timeout limits, execution terminates gracefully with an RFC 7807 HTTP 504 response (`downstream_timeout`). | **PASSED** |
| **9** | `Rate_Limiting_Returns_429` | Evaluates throughput exhaustion under strict permit limits, confirming HTTP 429 problem responses accompanied by RFC `Retry-After` headers. | **PASSED** |
| **10** | `Public_Authentication_Route_Works_Without_Authentication` | Proves unauthenticated onboarding queries to `/api/v1/auth/*` bypass gateway authentication checks and reach target identity destinations. | **PASSED** |
| **11** | `Protected_Organization_Route_Rejects_Unauthenticated_Requests` | Confirms calls to protected resources (`/api/v1/organizations/*`) lacking valid bearer credentials are immediately terminated with HTTP 401 Unauthorized (`authentication_required`). | **PASSED** |
| **12** | `CORS_Accepts_Approved_Origins_And_Rejects_Unapproved_Origins` | Verifies preflight CORS requests matching configured trusted domains receive `Access-Control-Allow-Origin` while unlisted host origins are completely rejected. | **PASSED** |
| **13** | `Unsafe_Client_Headers_Are_Removed_Or_Overwritten` | Proves case-insensitive stripping of client-supplied `x-user-id`, `X-TENANT-ID`, and arbitrary `X-Internal-*` headers while confirming preservation of W3C `traceparent` OpenTelemetry headers. | **PASSED** |
| **14** | `Untrusted_Client_Cannot_Spoof_Source_IP_Through_Forwarded_Headers` | Proves an untrusted external client sending simulated `X-Forwarded-For: 1.1.1.1`, `2.2.2.2` headers cannot trick rate limit partition keys, ensuring quota exhaustion (HTTP 429) at request #3. | **PASSED** |
| **15** | `Protected_Identity_Administrative_Endpoint_Rejects_Unauthenticated_Requests` | Verifies administrative identity endpoints under `/api/v1/identity/*` strictly require authentication and reject anonymous access with HTTP 401 Unauthorized. | **PASSED** |
| **16** | `Production_Environment_Without_Required_Config_Throws_InvalidOperationException` | Proves the fail-fast startup guard by demonstrating that booting the gateway in Production without defined CORS origins or JWT signing secrets throws an explicit `InvalidOperationException`. | **PASSED** |

---

## Deep Dive: Security Test Validations

### 1. Forwarded-Header Spoofing Resistance (Test #14)
- **Methodology**: Configures `Gateway:TrustedProxies` to an internal subnet (`10.0.0.1`), rendering test connection packets from loopback (`127.0.0.1`) an untrusted external client connection.
- **Execution**: The client emits successive HTTP requests to an anonymous route (permit quota = 2), appending alternating false headers: `X-Forwarded-For: 1.1.1.1`, `2.2.2.2`, `3.3.3.3`.
- **Validation**: The gateway rejects the spoofed forwarded headers and evaluates rate limit quotas exclusively against the real TCP socket connection IP. Request #3 is immediately blocked with HTTP 429 Too Many Requests.

### 2. Case-Insensitive & Pattern Header Sanitization (Test #13)
- **Methodology**: Transmits an authenticated request to `/api/v1/users/profile` containing intentionally obfuscated case headers (`x-user-id`, `X-TENANT-ID`), custom pattern bypass attempts (`X-Internal-SuperSecret`), and W3C tracing headers (`traceparent: 00-4bf92f...`).
- **Validation**: Downstream mock server inspection confirms all internal headers and arbitrary `X-Internal-*` variants are stripped prior to forwarding, while `Authorization` and W3C `traceparent` (with preserved TraceID) reach the downstream destination intact.
