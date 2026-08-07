# EMCORE API Gateway Configuration Reference

This document details all structural configuration sections, environment variable overrides, and rate limit definitions governing `Emcore.ApiGateway`.

## 1. Gateway Security & Proxy Trust Configuration

Under `appsettings.json` and `appsettings.Production.json`, the `Gateway` section defines network ingress rules, proxy trust boundaries, and Kestrel limits:

```json
"Gateway": {
  "AllowedOrigins": [],
  "ForwardedHeadersEnabled": true,
  "TrustedProxies": [ "127.0.0.1", "::1" ],
  "TrustedNetworks": [],
  "ForwardLimit": 1,
  "RequestTimeoutSeconds": 30,
  "MaxRequestBodySizeBytes": 10485760
}
```

### Key Properties & Behaviors
- `AllowedOrigins`: Array of trusted frontend origins for CORS. In Production, if this array is empty, startup terminates immediately with an `InvalidOperationException`. No `AllowAnyOrigin` wildcard fallback is permitted in Production.
- `ForwardedHeadersEnabled`: When set to true, activates `ForwardedHeadersMiddleware` to evaluate `X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Host`.
- `TrustedProxies`: Array of exact IP addresses recognized as reverse proxy endpoints (e.g., local IIS host at `127.0.0.1` or `::1`). Requests from untrusted sockets will ignore client-supplied forwarded headers.
- `TrustedNetworks`: Array of CIDR subnet blocks (e.g., `"10.0.0.0/8"`) recognized as trusted proxy infrastructures.
- `ForwardLimit`: Maximum number of proxy hops to inspect in forwarded header chains (defaults to `1` for direct IIS proxying).
- `RequestTimeoutSeconds`: Downstream request execution timeout before returning HTTP 504 Problem Details.
- `MaxRequestBodySizeBytes`: Maximum allowed request body size (defaults to `10485760` bytes / 10 MB).

---

## 2. Production Environment Variable Structure

When deploying to production environments, configuration parameters should be injected securely via environment variables or secret vaults without editing static source files:

```bash
# CORS Origins (Array syntax using double underscore notation)
Gateway__AllowedOrigins__0=https://portal.emcore.com
Gateway__AllowedOrigins__1=https://admin.emcore.com

# Forwarded Header Trusted Proxies (if hosting behind external load balancers)
Gateway__TrustedProxies__0=127.0.0.1
Gateway__TrustedProxies__1=10.0.10.5

# Rate Limiting Overrides
RateLimiting__Anonymous__PermitLimit=60
RateLimiting__Authenticated__PermitLimit=300
RateLimiting__LoginOtp__PermitLimit=10

# Production JWT Authentication Validation Parameters
Authentication__Issuer=https://identity.emcore.com
Authentication__Audience=https://api.emcore.com
Authentication__SigningKey=ENV_INJECTED_BASE64_HMAC_SECRET_KEY
```

---

## 3. Rate Limiting Policy Reference

| Policy Name | Target Endpoints | Quota Limit | Time Window | Partition Key Calculation |
| :--- | :--- | :--- | :--- | :--- |
| **AnonymousPolicy** | Public system queries (`/api/v1/system/*`) | 60 requests | 1 Minute | Verified Remote Socket IP (or Forwarded IP if from trusted proxy). |
| **AuthenticatedPolicy** | Protected general APIs (`/api/v1/users/*`, `/api/v1/organizations/*`) | 300 requests | 1 Minute | JWT Claim (`sub` / `NameIdentifier`), fallback to `client_id`, fallback to Remote IP. Never raw Authorization header string. |
| **LoginOtpPolicy** | Authentication endpoints (`/api/v1/auth/*`) | 10 requests | 1 Minute | Combined Remote IP + Endpoint Request Path (preventing brute force across targets). |
| **HealthPolicy** | Diagnostic monitoring (`/health/live`, `/health/ready`, `/health`) | Unlimited | None | Explicitly exempt from user throughput quotas (`RateLimitPartition.GetNoLimiter`). |

---

## 4. Reverse Proxy Cluster & Destination Configuration

### Localhost Single-Server Deployment Topology
In accordance with initial production release architecture, all microservices reside on a single Windows Server instance managed by IIS. Therefore, the target cluster destinations in `appsettings.Production.json` explicitly utilize reliable loopback socket addresses:

```json
"ReverseProxy": {
  "Clusters": {
    "identity-cluster": {
      "Destinations": {
        "destination1": {
          "Address": "http://127.0.0.1:5101/"
        }
      }
    },
    "organization-cluster": {
      "Destinations": {
        "destination1": {
          "Address": "http://127.0.0.1:5102/"
        }
      }
    }
  }
}
```
*Note: Do not replace localhost addresses with load balancer DNS hostnames unless downstream microservices are migrated to physically separated compute nodes.*
