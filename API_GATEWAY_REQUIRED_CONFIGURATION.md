# EMCORE API Gateway Required Production Configuration Guide

This document outlines the essential pre-requisites, configuration protections, and mandatory environment variables required to successfully execute `Emcore.ApiGateway` in a production environment.

## 1. Single-Server Localhost Topology Validation

In the current production release architecture, the API Gateway and all core backend microservices (`Emcore.IdentityAccess`, `Emcore.UserOrganization`) are hosted co-located on a single compute host managed by Windows IIS. 

### Localhost Destination Rule
Under this deployment model, downstream YARP destinations defined in `appsettings.Production.json` must strictly utilize reliable loopback socket bindings:
- **Identity & Authentication Service**: `http://127.0.0.1:5101/`
- **User & Organization Service**: `http://127.0.0.1:5102/`

**CRITICAL DIRECTIVE**: Do not modify these target loopback destinations to use internal DNS hostnames or external load-balancer IP addresses unless downstream services are migrated to separated network nodes. Utilizing loopback bindings eliminates network routing hops and guarantees secure internal socket communication.

---

## 2. Mandatory Production Fail-Fast Safeguards

To prevent accidental exposure of production services under insecure development defaults, `GatewayExtensions.cs` enforces strict fail-fast startup validations when running under `ASPNETCORE_ENVIRONMENT=Production`:

### A. CORS Origin Enforcement
- **Constraint**: Static `appsettings.Production.json` configuration explicitly defaults `Gateway:AllowedOrigins` to an empty array (`[]`).
- **Fail-Fast Behavior**: If the application boots in Production without at least one valid CORS origin supplied via environment variables, startup terminates instantly with an `InvalidOperationException`.
- **Prohibited Rule**: Wildcard (`AllowAnyOrigin` / `*`) CORS fallbacks are permanently disabled in Production.

### B. Authentication Secret Enforcement
- **Constraint**: Development authentication schemes (`TestAuthHandler`) are prohibited in Production.
- **Fail-Fast Behavior**: If `Authentication:Issuer`, `Authentication:Audience`, or `Authentication:SigningKey` are omitted or empty in Production, application booting aborts immediately with an `InvalidOperationException`.

---

## 3. Production Environment Variable Checklist

Prior to launching `EMCORE-ApiGateway` in IIS, operations teams must configure the following system environment variables (or inject them via secure vaults) within the target server runtime:

| Environment Variable Key | Sample Production Value | Description | Required? |
| :--- | :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Activates strict production security validations. | **YES** |
| `Gateway__AllowedOrigins__0` | `https://portal.emcore.com` | Primary user-facing portal frontend domain. | **YES** |
| `Gateway__AllowedOrigins__1` | `https://admin.emcore.com` | Administrative internal desktop dashboard domain. | Optional |
| `Gateway__TrustedProxies__0` | `127.0.0.1` | Loopback IIS host proxy IP address. | **YES** |
| `RateLimiting__Anonymous__PermitLimit` | `60` | Max unauthenticated queries per IP per minute. | Optional |
| `RateLimiting__Authenticated__PermitLimit` | `300` | Max authenticated queries per user per minute. | Optional |
| `RateLimiting__LoginOtp__PermitLimit` | `10` | Max login/OTP attempts per IP+endpoint per minute. | Optional |
| `Authentication__Issuer` | `https://identity.emcore.com` | Expected JWT issuer token URI. | **YES** |
| `Authentication__Audience` | `https://api.emcore.com` | Expected JWT target service audience. | **YES** |
| `Authentication__SigningKey` | `(High-Entropy Base64 Key)` | Secret HMAC signing key or JWKS trust material. | **YES** |
