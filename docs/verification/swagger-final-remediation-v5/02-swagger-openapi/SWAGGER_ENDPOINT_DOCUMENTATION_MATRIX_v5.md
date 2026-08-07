# Swagger Endpoint Documentation Matrix v5

| Service | Method | Runtime Route | Gateway Route | Operation ID | Implementation Type | Runtime Auth Metadata | Gateway Auth Policy | Request Type | Success Responses | Error Responses | Rate Limit | Idempotency | OpenAPI Match | Notes |
|---------|--------|---------------|---------------|--------------|---------------------|-----------------------|---------------------|--------------|-------------------|-----------------|------------|-------------|---------------|-------|
| emcore-api-gateway | GET | /health/live | /health/live | GatewayLiveHealth | GATEWAY | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-api-gateway | GET | /health/ready | /health/ready | GatewayReadyHealth | GATEWAY | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-api-gateway | GET | /health | /health | GatewayGeneralHealth | GATEWAY | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-api-gateway | GET | /api/v1/system/version | /api/v1/system/version | GatewaySystemVersion | GATEWAY | AllowAnonymous | N/A | None | 200 | 500, 429 | No | No | Yes | Actual runtime behavior |
| emcore-api-gateway | GET | /api/v1/swagger/registry | /api/v1/swagger/registry | GetSwaggerRegistry | GATEWAY | AllowAnonymous | N/A | None | 200 | 500, 429 | No | No | Yes | Actual runtime behavior |
| emcore-audit-reporting-api | GET | /health/live | /health/live | getHealthLive | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-audit-reporting-api | GET | /health/ready | /health/ready | getHealthReady | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-audit-reporting-api | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-bidding-deal-api | GET | /health/live | /health/live | getHealthLive | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-bidding-deal-api | GET | /health/ready | /health/ready | getHealthReady | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-bidding-deal-api | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-catalog-listing-api | GET | /health/live | /health/live | getHealthLive | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-catalog-listing-api | GET | /health/ready | /health/ready | getHealthReady | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-catalog-listing-api | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-conversation-realtime-api | GET | /health/live | /health/live | getHealthLive | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-conversation-realtime-api | GET | /health/ready | /health/ready | getHealthReady | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-conversation-realtime-api | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | GET | /.well-known/jwks.json | /.well-known/jwks.json | GetPublicJwks | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | GET | /api/v1/auth/.well-known/jwks.json | /api/v1/auth/.well-known/jwks.json | GetAuthJwks | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/register | /api/v1/auth/register | RegisterUser | BUSINESS | AllowAnonymous | N/A | JSON | 201 | 400, 409, 422, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/verification/email/send | /api/v1/auth/verification/email/send | SendEmailVerification | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 404, 429, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/verification/email/confirm | /api/v1/auth/verification/email/confirm | ConfirmEmailVerification | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 422, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/verification/mobile/send | /api/v1/auth/verification/mobile/send | SendMobileVerification | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 404, 429, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/verification/mobile/confirm | /api/v1/auth/verification/mobile/confirm | ConfirmMobileVerification | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 422, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/login | /api/v1/auth/login | Login | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 401, 403, 429, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/token/refresh | /api/v1/auth/token/refresh | RefreshToken | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 401, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/logout | /api/v1/auth/logout | Logout | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 401, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/logout-all | /api/v1/auth/logout-all | LogoutAll | BUSINESS | AllowAnonymous | N/A | None | 200 | 401, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/password/forgot | /api/v1/auth/password/forgot | ForgotPassword | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 429, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/password/reset | /api/v1/auth/password/reset | ResetPassword | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 422, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/password/change | /api/v1/auth/password/change | ChangePassword | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 401, 422, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | GET | /api/v1/auth/sessions | /api/v1/auth/sessions | GetSessions | BUSINESS | AllowAnonymous | N/A | None | 200 | 401, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | DELETE | /api/v1/auth/sessions/{sessionId} | /api/v1/auth/sessions/{sessionId} | RevokeSession | BUSINESS | AllowAnonymous | N/A | None | 200 | 401, 404, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | GET | /api/v1/auth/account/status | /api/v1/auth/account/status | GetAccountStatus | BUSINESS | AllowAnonymous | N/A | None | 200 | 401, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | GET | /api/v1/identity/me | /api/v1/identity/me | GetCurrentIdentity | BUSINESS | AllowAnonymous | N/A | None | 200 | 401, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/mfa/verify | /api/v1/auth/mfa/verify | VerifyMfaLogin | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 401, 422, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/mfa/register | /api/v1/auth/mfa/register | RegisterMfa | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 401, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/mfa/confirm | /api/v1/auth/mfa/confirm | ConfirmMfa | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 401, 422, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/stepup/initiate | /api/v1/auth/stepup/initiate | InitiateStepUp | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 401, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/stepup/verify | /api/v1/auth/stepup/verify | VerifyStepUp | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 401, 422, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/auth/token | /api/v1/auth/token | IssueServiceToken | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 401, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/identity/service-clients/register | /api/v1/identity/service-clients/register | RegisterServiceClient | BUSINESS | AllowAnonymous | N/A | JSON | 201 | 400, 403, 409, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/identity/service-clients/{id}/rotate | /api/v1/identity/service-clients/{id}/rotate | RotateServiceClientCredential | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 403, 404, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/identity/service-clients/credentials/revoke | /api/v1/identity/service-clients/credentials/revoke | RevokeServiceClientCredential | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 403, 404, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | GET | /api/v1/identity/service-clients/{id}/credentials | /api/v1/identity/service-clients/{id}/credentials | ListServiceClientCredentials | BUSINESS | AllowAnonymous | N/A | None | 200 | 403, 404, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/identity/admin/users/status | /api/v1/identity/admin/users/status | AdminUpdateUserStatusPost | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 403, 404, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | PUT | /api/v1/identity/admin/users/{id}/status | /api/v1/identity/admin/users/{id}/status | AdminUpdateUserStatusPut | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 403, 404, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/identity/register | /api/v1/identity/register | LegacyRegister | BUSINESS | AllowAnonymous | N/A | JSON | 201 | 400, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/identity/verify | /api/v1/identity/verify | LegacyVerify | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/identity/resend-verification | /api/v1/identity/resend-verification | LegacyResendVerification | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/identity/login | /api/v1/identity/login | LegacyLogin | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 400, 401, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/identity/refresh | /api/v1/identity/refresh | LegacyRefresh | BUSINESS | AllowAnonymous | N/A | JSON | 200 | 401, 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | POST | /api/v1/identity/logout | /api/v1/identity/logout | LegacyLogout | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-identity-access-api | GET | /api/v1/identity/users/{id} | /api/v1/identity/users/{id} | LegacyGetUserById | BUSINESS | AllowAnonymous | N/A | None | 200 | 404, 500 | No | No | Yes | Actual runtime behavior |
| emcore-inspection-trust-api | GET | /health/live | /health/live | getHealthLive | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-inspection-trust-api | GET | /health/ready | /health/ready | getHealthReady | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-inspection-trust-api | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-inventory-media-api | GET | /health/live | /health/live | getHealthLive | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-inventory-media-api | GET | /health/ready | /health/ready | getHealthReady | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-inventory-media-api | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-mcp-gateway | GET | /health/live | /health/live | getHealthLive | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-mcp-gateway | GET | /health/ready | /health/ready | getHealthReady | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-mcp-gateway | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-notification-integration-api | GET | /health/live | /health/live | getHealthLive | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-notification-integration-api | GET | /health/ready | /health/ready | getHealthReady | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-notification-integration-api | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-portal-bff | GET | /health/live | /health/live | getHealthLive | BFF | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-portal-bff | GET | /health/ready | /health/ready | getHealthReady | BFF | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-portal-bff | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BFF | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-public-bff | GET | /health/live | /health/live | getHealthLive | BFF | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-public-bff | GET | /health/ready | /health/ready | getHealthReady | BFF | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-public-bff | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BFF | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-realtime-gateway | GET | /health/live | /health/live | getHealthLive | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-realtime-gateway | GET | /health/ready | /health/ready | getHealthReady | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-realtime-gateway | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-search-discovery-api | GET | /health/live | /health/live | getHealthLive | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-search-discovery-api | GET | /health/ready | /health/ready | getHealthReady | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-search-discovery-api | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-subscription-payment-api | GET | /health/live | /health/live | getHealthLive | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-subscription-payment-api | GET | /health/ready | /health/ready | getHealthReady | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-subscription-payment-api | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-user-organization-api | GET | /health/live | /health/live | getHealthLive | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-user-organization-api | GET | /health/ready | /health/ready | getHealthReady | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-user-organization-api | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
| emcore-workflow-scheduler-api | GET | /health/live | /health/live | getHealthLive | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-workflow-scheduler-api | GET | /health/ready | /health/ready | getHealthReady | FRAMEWORK | AllowAnonymous | N/A | None | 200 |  | No | No | Yes | Actual runtime behavior |
| emcore-workflow-scheduler-api | GET | /api/v1/system/version | /api/v1/system/version | getApiV1SystemVersion | BUSINESS | AllowAnonymous | N/A | None | 200 | 500 | No | No | Yes | Actual runtime behavior |
