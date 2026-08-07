# Gateway Swagger CSP Fix for Development

## Root Cause
The `SecurityHeadersMiddleware.cs` in the API Gateway was applying a globally restrictive Content-Security-Policy (CSP) of `default-src 'none'; frame-ancestors 'none'; upgrade-insecure-requests;` to all HTTP responses. While this is the correct security posture for standard JSON API endpoints, it inadvertently blocked the browser from loading static assets required by Swagger UI (such as `.css`, `.js`, and image files) in the Development environment. 

Because the strict CSP was sent even when accessing `/swagger`, the browser refused to evaluate inline scripts or load the Swagger bundle, resulting in a broken Swagger UI page on deployments such as `https://stockout.flowb.io/swagger`.

## Source File Changed
- `gateways/Emcore.ApiGateway/Middleware/SecurityHeadersMiddleware.cs`

## Development CSP (Swagger Only)
For requests starting with `/swagger` in non-Production environments, the CSP has been relaxed to:
```http
Content-Security-Policy: default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self' data:; connect-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self';
```

## Normal API CSP Behavior
For any other API routes (e.g., `/health/live`, `/api/v1/system/version`) in Development, the restrictive API CSP remains exactly as before.

## Production Behavior
**Unchanged.** The Production environment always enforces the restrictive API CSP (`default-src 'none'`) regardless of the path. The Swagger-specific relaxation is explicitly gated behind `!environment.IsProduction()`.

## Test Commands
Run the Gateway tests to ensure CSP behavior works for both Swagger endpoints and normal endpoints:
```bash
dotnet test gateways/Emcore.ApiGateway.Tests/Emcore.ApiGateway.Tests.csproj --configuration Release
```

## Deployment Verification URL
After the Gateway is republished to DEV IIS, you can verify the fix at:
[https://stockout.flowb.io/swagger](https://stockout.flowb.io/swagger)
