# EMCORE Shared Support Classes and Dependencies

## 1. Principle

Shared libraries contain technical cross-cutting capabilities only. They must not contain EMCORE marketplace entities, rules or stored-procedure names.

## 2. `Emcore.BuildingBlocks.Core`

Create compile-ready versions of:

- `Result`
- `Result<T>`
- `Error`
- `ErrorType`
- `DomainException`
- `NotFoundException`
- `ConflictException`
- `ForbiddenException`
- `ValidationException`
- `IClock`
- `SystemClock`
- `IIdGenerator`
- `UlidGenerator` or UUID implementation
- `Guard`

Do not add business error codes.

## 3. `Emcore.BuildingBlocks.Api`

Create:

- `ApiResponse<T>` or a documented decision to use plain responses plus Problem Details.
- `PagedResponse<T>`
- `CursorResponse<T>`
- `ProblemCode`
- `ValidationErrorItem`
- `GlobalExceptionHandler`
- `CorrelationIdMiddleware`
- `RequestIdMiddleware`
- `SecurityHeadersMiddleware`
- `EndpointConventionExtensions`
- `HealthEndpointExtensions`
- `VersionEndpointExtensions`
- `OpenApiExtensions`

Use ASP.NET Core Problem Details. Do not expose stack traces outside Local.

## 4. `Emcore.BuildingBlocks.Data`

Required packages:

- `Dapper`
- `Microsoft.Data.SqlClient`
- `Microsoft.Extensions.Options.ConfigurationExtensions`
- `Microsoft.Extensions.Diagnostics.HealthChecks`

Create:

```csharp
public sealed class SqlDatabaseOptions
{
    public const string SectionName = "Database";
    public string? ConnectionString { get; init; }
    public int CommandTimeoutSeconds { get; init; } = 30;
    public bool Enabled { get; init; }
}
```

Interfaces/classes:

- `ISqlConnectionFactory`
- `SqlConnectionFactory`
- `IStoredProcedureExecutor`
- `StoredProcedureExecutor`
- `StoredProcedureCommand`
- `DatabaseDependencyState`
- `DatabaseNotConfiguredException`
- `DatabaseRegistrationExtensions`

Executor capabilities should be compile-ready for later use:

- `ExecuteAsync`
- `QueryAsync<T>`
- `QuerySingleOrDefaultAsync<T>`
- `QueryMultipleAsync<TResult>`

Rules:

- Require `CommandType.StoredProcedure`.
- Accept cancellation tokens.
- Use explicit timeout.
- Open connections per operation and dispose correctly.
- Do not open a connection during DI registration or startup.
- When `Database:Enabled=false`, database readiness is reported as disabled, not failed, in Local only.
- No service-specific stored-procedure constants in this package.

## 5. `Emcore.BuildingBlocks.Messaging`

Required packages:

- `MassTransit`
- `MassTransit.RabbitMQ`

Create foundations:

- `IntegrationEvent`
- `EventEnvelope<T>`
- `MessageContext`
- `IEventPublisher`
- `IMessageConsumer<T>`
- `IOutboxWriter`
- `IInboxStore`
- `OutboxMessageState`
- `InboxMessageState`
- `MessagingOptions`
- `MessagingDependencyState`
- `NoOpEventPublisher`
- `MessagingRegistrationExtensions`

Do not declare business queues or business event types. Local mode must support `Messaging:Enabled=false`.

## 6. `Emcore.BuildingBlocks.Security`

Create:

- `ICurrentUser`
- `CurrentUserContext`
- `IOrganizationContext`
- `OrganizationContext`
- `IPermissionChecker`
- `PermissionDecision`
- `IServiceIdentity`
- `SensitiveValueMasker`
- `AuthenticationOptions`
- `AuthorizationRegistrationExtensions`

At setup stage, use safe placeholder implementations. Do not invent actual roles, permissions or token issuers.

## 7. `Emcore.BuildingBlocks.Observability`

Required packages should cover:

- OpenTelemetry hosting.
- ASP.NET Core instrumentation.
- HTTP client instrumentation.
- Runtime/process instrumentation where supported.
- OTLP exporter.

Create:

- `TelemetryOptions`
- `TelemetryConstants`
- `ActivitySources`
- `MetricNames`
- `CorrelationContext`
- `ServiceTelemetryExtensions`
- Structured logging helpers.

Every service must emit service name, service version and environment resource attributes.

## 8. `Emcore.BuildingBlocks.Caching`

Required package:

- `StackExchange.Redis`

Create:

- `RedisOptions`
- `ICacheService`
- `NoOpCacheService`
- `RedisCacheService` skeleton
- `CacheKeyBuilder`
- `CacheRegistrationExtensions`

No permanent business state may be defined here.

## 9. `Emcore.BuildingBlocks.Storage`

Required package:

- AWS SDK for S3 or a compatible abstraction package.

Create:

- `ObjectStorageOptions`
- `IObjectStorage`
- `SignedUploadRequest`
- `SignedUploadResult`
- `SignedDownloadResult`
- `ObjectMetadata`
- `NoOpObjectStorage`
- `ObjectStorageRegistrationExtensions`

Do not implement production buckets or access policies.

## 10. `Emcore.BuildingBlocks.Idempotency`

Create:

- `IdempotencyOptions`
- `IIdempotencyStore`
- `IdempotencyRequest`
- `IdempotencyResult`
- `IdempotencyStatus`
- `IdempotencyKeyValidator`
- `NoOpIdempotencyStore`

Persistence will be added after database design.

## 11. `Emcore.BuildingBlocks.Testing`

Create helpers for:

- Test application factory.
- Fake clock.
- Deterministic ID generator.
- Configuration test builder.
- Health endpoint assertions.
- Architecture-test assembly scanning.

## 12. Service-specific support classes

Each service Infrastructure project must include empty/compile-ready service-owned types:

- `<Service>InfrastructureOptions`
- `DependencyInjection`
- `Persistence/README.md`
- `Messaging/README.md`
- `Integrations/README.md`

Each service Application project must include:

- `DependencyInjection`
- `AssemblyReference`
- `I<Service>ApplicationMarker` or equivalent marker.

Each service Contracts project must include:

- `SystemVersionResponse`
- Contract namespace marker.

## 13. Central package management

Use `Directory.Packages.props`. Resolve and pin mutually compatible stable package versions. At minimum include:

- Microsoft ASP.NET Core/OpenAPI packages required by the chosen template.
- Dapper.
- Microsoft.Data.SqlClient.
- MassTransit.
- MassTransit.RabbitMQ.
- StackExchange.Redis.
- OpenTelemetry packages.
- YARP for the API Gateway.
- AWS SDK S3.
- FluentValidation if used.
- Microsoft.Extensions.Http.Resilience.
- xUnit.
- FluentAssertions or equivalent.
- Architecture-test library such as NetArchTest or equivalent.
- Testcontainers packages only in optional integration-test projects.

The completion report must list every resolved version and explain any prerelease package. Prefer stable packages only.

## 14. Disallowed shared code

Do not put any of the following in building blocks:

- Listing, Product, Bid, Deal, Delivery, Inspection, Green Points, Subscription, Payment or Organization entities.
- Service-specific database models.
- Service-specific stored-procedure names.
- Service-specific error codes.
- Service-specific queue names.
- Service-specific permission constants.
- Direct references to another service.
