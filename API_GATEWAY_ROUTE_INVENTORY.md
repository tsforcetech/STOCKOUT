# EMCORE API Gateway Route Inventory & Transform Specification

This document catalogues all inbound endpoints, reverse proxy routing rules, authorization policies, and URL transform behaviors executed by `Emcore.ApiGateway`.

## 1. Gateway Route Mapping Table

| Route Identifier | Match Path Pattern | Downstream Target Cluster | Downstream Destination Address | Authorization Policy | Rate Limiting Policy | Path Transform Behavior |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **auth-route** | `/api/v1/auth/{**catch-all}` | `identity-cluster` | `http://127.0.0.1:5101/` | `PublicPolicy` | `LoginOtpPolicy` | Default Path Preservation (No Prefix Strip) |
| **identity-route** | `/api/v1/identity/{**catch-all}` | `identity-cluster` | `http://127.0.0.1:5101/` | `AuthenticatedRoutePolicy` | `AuthenticatedPolicy` | Default Path Preservation (No Prefix Strip) |
| **users-route** | `/api/v1/users/{**catch-all}` | `organization-cluster` | `http://127.0.0.1:5102/` | `AuthenticatedRoutePolicy` | `AuthenticatedPolicy` | Default Path Preservation (No Prefix Strip) |
| **organizations-route** | `/api/v1/organizations/{**catch-all}` | `organization-cluster` | `http://127.0.0.1:5102/` | `AuthenticatedRoutePolicy` | `AuthenticatedPolicy` | Default Path Preservation (No Prefix Strip) |
| **system-version** | `/api/v1/system/version` | Internal Gateway Handler | N/A (Handled Directly) | `AnonymousPolicy` | `AnonymousPolicy` | Returns Gateway Service Metadata |
| **health-live** | `/health/live` | Internal Gateway Handler | N/A (Handled Directly) | `PublicPolicy` | `HealthPolicy` (Exempt) | Returns Liveness Status (`200 OK`) |
| **health-ready** | `/health/ready` | Internal Gateway Handler | N/A (Handled Directly) | `PublicPolicy` | `HealthPolicy` (Exempt) | Returns Readiness Status (`200 OK`) |
| **health-general** | `/health` | Internal Gateway Handler | N/A (Handled Directly) | `PublicPolicy` | `HealthPolicy` (Exempt) | Returns Basic Health Status (`200 OK`) |

---

## 2. YARP Path Transform Specification

### Default Preservation Behavior
By design, all reverse proxy routes defined in `appsettings.json` intentionally omit explicit `Transforms` instructions. Under YARP 2.2 specifications, when no explicit path transformation rules are present, the gateway applies the default **PathCopy** behavior:
- **Inbound Request**: `POST /api/v1/auth/login`
- **Forwarded Target**: `http://127.0.0.1:5101/api/v1/auth/login`
- **Downstream Expectation**: Downstream services (`Emcore.IdentityAccess` and `Emcore.UserOrganization`) must map their API endpoints inclusive of the `/api/v1/service-name/` prefix.
- **Contract Status**: Confirmed as aligned with current single-server deployment contracts. Any future requirement to strip prefixes (e.g., converting `/api/v1/auth/*` to `/*` downstream) requires adding an explicit `PathStrip` transform entry after confirming downstream API contracts.

---

## 3. Security Review: Broad Public Authentication Routing

### Option B Safety Validation: Public Gateway Route Support
The route pattern `/api/v1/auth/{**catch-all}` operates under `PublicPolicy` to seamlessly accommodate unauthenticated onboarding flows, including user registration, credential authentication, multifactor OTP challenges, and public key discovery.

#### Architectural Safeguards & Defense-in-Depth
1. **Administrative Isolation**: Privileged account operations, session revocations, role assignments, and tenant administrations are strictly barred from the `/api/v1/auth/*` route path. All administrative identity operations are located under `/api/v1/identity/*`, which strictly mandates `AuthenticatedRoutePolicy` at the gateway perimeter.
2. **Downstream Re-Verification**: Gateway authorization serves purely as an ingress gate; it does not relieve downstream services of responsibility. `Emcore.IdentityAccess` explicitly reapplies attribute, role, and claim validation on all incoming endpoint executions.
3. **Automated Verification**: Integrated test suites verify that attempts to access administrative identity resources (e.g., `/api/v1/identity/admin/revoke`) without a valid bearer token are terminated directly at the gateway with HTTP 401 Unauthorized.

---

## 4. Health Check Exemption & Privacy Rules

All health monitoring endpoints (`/health/live`, `/health/ready`, and `/health`) execute under **HealthPolicy**, which is explicitly registered as a `NoLimiter` partition. This guarantees that internal load balancer health polling or rapid container orchestrator probes will never trigger false 429 rate limit rejections or consume user quotas.

### Data Leakage Protections
Health endpoint responses are restricted to minimal JSON status assertions (`{"status":"Healthy","service":"Emcore.ApiGateway"}`). Public health executions are verified to prevent leaking internal database server names, RabbitMQ credentials, internal loopback URL sockets, stack traces, or configuration properties.
