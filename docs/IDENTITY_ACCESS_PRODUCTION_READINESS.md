# Identity Access Production Readiness

This document confirms that Identity Access has been standardized for production runtime.

## Projects

* **Emcore.IdentityAccess.Migrator**: Run once during deployment to upgrade the database schema.
* **Emcore.IdentityAccess.Api**: Hosted in IIS, handles registration, login, verification, MFA, Step-Up, password recovery, JWT issuance, and outbox record creation.
* **Emcore.IdentityAccess.Worker**: Hosted as a Windows Service, manages RabbitMQ outbox publishing and cleanups.
* **Emcore.ApiGateway**: Hosted in IIS, acts as the edge router and validates JWTs from Identity Access.

## Required API Settings (ppsettings.Production.json)

* ConnectionStrings:IdentityDatabase
* Database:Enabled
* Jwt:Enabled
* Jwt:Issuer
* Jwt:Audience
* Jwt:KeyId
* Jwt:SigningKey
* Jwt:AccessTokenLifetimeMinutes
* Otp:HmacPepper
* Identity:RefreshTokenLifetimeDays
* Identity:VerificationLifetimeMinutes
* Identity:PasswordResetLifetimeMinutes
* Identity:MaximumFailedLoginAttempts
* Identity:LockoutMinutes
* Identity:MinimumPasswordLength
* Identity:IdempotencyRetentionHours
* Email:Provider
* Email:Host
* Email:Port
* Email:UseSsl
* Email:Username
* Email:Password
* Email:FromAddress
* Email:FromName

## Required Worker Settings (ppsettings.Production.json)

* ConnectionStrings:IdentityDatabase
* RabbitMq:Enabled
* RabbitMq:HostName
* RabbitMq:Port
* RabbitMq:VirtualHost
* RabbitMq:UserName
* RabbitMq:Password
* RabbitMq:Exchange
* RabbitMq:ConnectionName
* RabbitMq:PublisherConfirmTimeoutSeconds
* Outbox:Enabled
* Outbox:BatchSize
* Outbox:PollingIntervalSeconds
* Outbox:MaxPublishAttempts
* Cleanup:IntervalHours
* Cleanup:RetentionHours

## Required Gateway Settings (ppsettings.Production.json)

* Jwt:Enabled
* Jwt:Issuer
* Jwt:Audience
* Jwt:JwksUrl
* Gateway:AllowedOrigins
* ReverseProxy:Clusters:identity-access-cluster:Destinations:destination1:Address (IdentityAccess BaseUrl)
