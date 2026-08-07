# EMCORE API Gateway Complete Security Review

This document consolidates the architectural security audit of `Emcore.ApiGateway`, validating defense-in-depth measures against network spoofing, header injection, cross-origin unauthorized access, and authentication bypass attempts.

## 1. Forwarded Header Spoofing Defense

### Threat Vector
When reverse proxies operate behind public load balancers or IIS servers, client applications can insert fraudulent HTTP headers (`X-Forwarded-For: 1.2.3.4`) into network packets. If an application indiscriminately trusts these headers (such as when clearing known networks and proxies without replacement), malicious actors can spoof arbitrary remote source IPs to evade rate-limiting restrictions, impersonate whitelisted subnets, or corrupt security logging audit trails.

### Implemented Remediation
In `GatewayExtensions.cs`, ASP.NET Core's `ForwardedHeadersOptions` is configured with strict network trust verification:
- `KnownProxies` and `KnownIPNetworks` are populated exclusively from `Gateway:TrustedProxies` (defaulting to loopback `127.0.0.1` and `::1`) and `Gateway:TrustedNetworks`.
- When a client connects from a TCP socket address not explicitly registered in `TrustedProxies`, ASP.NET Core instantly rejects client-supplied `X-Forwarded-For` headers, utilizing the true network TCP connection socket address for all rate limit calculations and audit telemetry.
- **Automated Evidence**: Integration Test #14 (`Untrusted_Client_Cannot_Spoof_Source_IP_Through_Forwarded_Headers`) verifies this defense in automated execution, confirming HTTP 429 rate limit rejection when untrusted sockets attempt IP address randomization.

---

## 2. Case-Insensitive Header Sanitization & Pattern Stripping

### Threat Vector
Downstream microservices frequently utilize internally injected headers (e.g., `X-User-Id`, `X-Tenant-Id`, `X-Internal-Role`) passed by perimeter security services to establish identity context. If an external attacker directly submits these headers in an incoming HTTP payload—or modifies casing (`x-uSeR-iD`) to evade case-sensitive string matching—they could trigger privilege escalation or cross-tenant data access.

### Implemented Remediation
`HeaderManagementMiddleware.cs` intercepts every inbound request prior to downstream forwarding and executes exhaustive case-insensitive sanitization:
- Enumerates all incoming HTTP request header keys and evaluates them against an immutable blocklist using `StringComparison.OrdinalIgnoreCase`.
- Universal wildcard protection: Identifies and strips any header beginning with the string prefix `"X-Internal-"` case-insensitively, neutralizing future internal communication channels from external exploitation.
- **Automated Evidence**: Integration Test #13 (`Unsafe_Client_Headers_Are_Removed_Or_Overwritten`) confirms complete stripping of `x-user-id`, `X-TENANT-ID`, and `X-Internal-SuperSecret` while validating uninterrupted propagation of standard W3C tracing headers (`traceparent`, `tracestate`).

---

## 3. Public vs. Protected Route Architecture (Option B Safety)

### Route Strategy Analysis
The gateway route pattern `/api/v1/auth/{**catch-all}` is configured with `PublicPolicy`, permitting unauthenticated traffic to reach the downstream Identity Access service cluster (`http://127.0.0.1:5101/`).

### Architectural Justification & Defense-in-Depth Validation (Option B)
Maintaining public perimeter ingress for `/api/v1/auth/*` is structurally necessary to accommodate external onboarding workflows (registration, login authentication, password reset, and multifactor OTP verification). This configuration is certified safe under the following strict architectural invariants:
1. **Administrative Segregation**: Privileged user administration and identity management endpoints are structurally barred from existing under the `/api/v1/auth/*` routing prefix. All privileged operations reside under `/api/v1/identity/*` (e.g., `/api/v1/identity/admin/*`), which enforces `AuthenticatedRoutePolicy` at the gateway perimeter.
2. **Layer 7 Downstream Re-Validation**: Gateway route authorization acts as an initial perimeter filter; it does not substitute for downstream application authorization. `Emcore.IdentityAccess` explicitly executes token evaluation and policy enforcement on all incoming requests.
3. **Automated Evidence**: Integration Test #15 (`Protected_Identity_Administrative_Endpoint_Rejects_Unauthenticated_Requests`) verifies that unauthenticated queries targeting administrative identity resources are immediately blocked by the gateway with HTTP 401 Unauthorized.

---

## 4. Production Fail-Fast Validation & Default Deny Policies

### Threat Vector
Misconfiguration during deployment staging—such as failing to supply production CORS domains or omitting JWT verification keys—can lead applications to silently fall back to insecure developer defaults (e.g., allowing `AllowAnyOrigin` or enabling mock test authentication handlers in production).

### Implemented Remediation
`GatewayExtensions.cs` enforces strict fail-fast validation when `ASPNETCORE_ENVIRONMENT=Production`:
- **CORS Protection**: Static production configuration defaults to an empty allowed origin array (`[]`). If production startup executes without environment variable injection (`Gateway__AllowedOrigins__0=...`), an `InvalidOperationException` terminates application startup immediately. Wildcard CORS fallbacks are structurally impossible in Production.
- **Authentication Protection**: The development `TestAuthHandler` is strictly restricted to non-production testing environments. In Production, missing JWT signing parameters (`Authentication:Issuer`, `Audience`, `SigningKey`) triggers immediate startup termination via `InvalidOperationException`.
- **Automated Evidence**: Integration Test #16 (`Production_Environment_Without_Required_Config_Throws_InvalidOperationException`) confirms immediate startup exception generation when either CORS origins or authentication parameters are absent in simulated production boots.
