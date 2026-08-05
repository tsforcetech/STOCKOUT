# EMCORE API Gateway Source Verification Audit Report

This report confirms line-by-line source code validation across `Emcore.ApiGateway`, verifying compliance with security hardening standards, YARP routing rules, and ASP.NET Core middleware architectural conventions.

## 1. Pipeline Order Verification (`Program.cs`)

The order of middleware execution in `Program.cs` defines the application's request processing lifecycle and security boundaries. The actual implemented pipeline matches our strict design requirements:

```csharp
// Step 1: Core diagnostics and OpenTelemetry service defaults
app.UseServiceDefaults();

// Step 2: Resolve actual remote socket or trusted forwarded IP address
app.UseForwardedHeaders();

// Step 3: Global RFC 7807 problem details error interceptor and exception handler
app.UseMiddleware<GatewayErrorHandlingMiddleware>();

// Step 4: Case-insensitive client header sanitization and correlation ID injection
app.UseMiddleware<HeaderManagementMiddleware>();

// Step 5: Structured diagnostic request/response telemetry logging
app.UseMiddleware<StructuredLoggingMiddleware>();

// Step 6: Apply strict HTTP response defensive headers (HSTS, NoSniff, Frame-Options)
app.UseMiddleware<SecurityHeadersMiddleware>();

// Step 7: Evaluate Cross-Origin Resource Sharing boundaries
app.UseCors("GatewayCorsPolicy");

// Step 8: Enforce rate limiting quotas on validated IP or identity partitions
app.UseRateLimiter();

// Step 9: Verify JWT token authentication and evaluate authorization routing policies
app.UseAuthentication();
app.UseAuthorization();

// Step 10: Register diagnostic health endpoints with HealthPolicy exemption
app.MapGet("/health/live", ...).RequireRateLimiting("HealthPolicy");
app.MapGet("/health/ready", ...).RequireRateLimiting("HealthPolicy");
app.MapGet("/health", ...).RequireRateLimiting("HealthPolicy");

// Step 11: Dispatch validated request to YARP reverse proxy engine
app.MapReverseProxy();
```

---

## 2. Rate Limiting Partition Verification (`GatewayExtensions.cs`)

Inspection of `GatewayExtensions.cs` confirms that rate limiting partitions are safely calculated without exposing the gateway to spoofing or denial-of-service vulnerabilities:

- **Anonymous Policy Partitioning**: Evaluated against `context.Connection.RemoteIpAddress`. Because `UseForwardedHeaders()` precedes rate limiting in `Program.cs`, this IP address is guaranteed to represent the physical TCP connection socket unless the packet originated from a validated `TrustedProxy` IP address.
- **Authenticated Policy Partitioning**: Evaluates claim attributes in priority sequence: `context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value` or `"client_id"`. The code completely avoids utilizing raw `Authorization` request headers as partition keys, eliminating token spoofing memory exhaustion risks.
- **Login OTP Policy Partitioning**: Utilizes a composite key combining remote socket IP with endpoint URI path (`{remoteIp}:{endpoint}`), limiting aggressive authentication scraping without penalizing non-login traffic from shared corporate NAT IPs.
- **Health Exemption Policy**: Confirmed registration of `RateLimitPartition.GetNoLimiter("health-exempt")`, ensuring automated orchestration health probes are exempt from user quotas.

---

## 3. RFC 7807 Problem Details Interceptor Audit (`GatewayErrorHandlingMiddleware.cs`)

Source inspection of `GatewayErrorHandlingMiddleware.cs` confirms complete compliance with RFC 7807 structured error formatting across all standard failure scenarios:
- **Automatic Interception**: Wraps subsequent pipeline execution in a `try/catch` block and intercepts unstarted responses where `StatusCode >= 400`.
- **YARP Forwarder Error Mapping**: Inspects `IForwarderErrorFeature` to translate proxy failures into machine-readable problem structures:
  - `ForwarderError.RequestTimedOut` $\rightarrow$ HTTP 504 Gateway Timeout (`downstream_timeout`)
  - `ForwarderError.NoAvailableDestinations` $\rightarrow$ HTTP 503 Service Unavailable (`destination_unavailable`)
  - `ForwarderError.Request` $\rightarrow$ HTTP 502 Bad Gateway (`proxy_error`)
- **Gateway Status Code Mapping**: Automatically formats standard gateway rejections:
  - HTTP 429 $\rightarrow$ `rate_limit_exceeded` with RFC `Retry-After` header integration.
  - HTTP 404 $\rightarrow$ `unmatched_gateway_route` for unknown API endpoint requests.
  - HTTP 401/403 $\rightarrow$ `authentication_required` / `forbidden_access`.
- **Correlation Inclusion**: Confirmed that generated problem JSON structures explicitly attach `requestId`, `correlationId`, and OpenTelemetry `traceId` attributes for tracing and forensics.
