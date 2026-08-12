# Endpoint Inventory Before Refactoring

## emcore-api-gateway

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | GatewayLiveHealth |
| /health/ready | GET | GatewayReadyHealth |
| /health | GET | GatewayGeneralHealth |
| /api/v1/system/version | GET | GatewaySystemVersion |
| /api/v1/swagger/registry | GET | GetSwaggerRegistry |

## emcore-audit-reporting-api

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

## emcore-bidding-deal-api

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

## emcore-catalog-listing-api

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

## emcore-conversation-realtime-api

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

## emcore-identity-access-api

| Route | Method | Contract (OpId) |
|---|---|---|
| /.well-known/jwks.json | GET | GetPublicJwks |
| /api/v1/auth/.well-known/jwks.json | GET | GetAuthJwks |
| /api/v1/auth/register | POST | RegisterUser |
| /api/v1/auth/verification/email/send | POST | SendEmailVerification |
| /api/v1/auth/verification/email/confirm | POST | ConfirmEmailVerification |
| /api/v1/auth/verification/mobile/send | POST | SendMobileVerification |
| /api/v1/auth/verification/mobile/confirm | POST | ConfirmMobileVerification |
| /api/v1/auth/login | POST | Login |
| /api/v1/auth/token/refresh | POST | RefreshToken |
| /api/v1/auth/logout | POST | Logout |
| /api/v1/auth/logout-all | POST | LogoutAll |
| /api/v1/auth/password/forgot | POST | ForgotPassword |
| /api/v1/auth/password/reset | POST | ResetPassword |
| /api/v1/auth/password/change | POST | ChangePassword |
| /api/v1/auth/sessions | GET | GetSessions |
| /api/v1/auth/sessions/{sessionId} | DELETE | RevokeSession |
| /api/v1/auth/account/status | GET | GetAccountStatus |
| /api/v1/identity/me | GET | GetCurrentIdentity |
| /api/v1/auth/mfa/verify | POST | VerifyMfaLogin |
| /api/v1/auth/mfa/register | POST | RegisterMfa |
| /api/v1/auth/mfa/confirm | POST | ConfirmMfa |
| /api/v1/auth/stepup/initiate | POST | InitiateStepUp |
| /api/v1/auth/stepup/verify | POST | VerifyStepUp |
| /api/v1/auth/token | POST | IssueServiceToken |
| /api/v1/identity/service-clients/register | POST | RegisterServiceClient |
| /api/v1/identity/service-clients/{id}/rotate | POST | RotateServiceClientCredential |
| /api/v1/identity/service-clients/credentials/revoke | POST | RevokeServiceClientCredential |
| /api/v1/identity/service-clients/{id}/credentials | GET | ListServiceClientCredentials |
| /api/v1/identity/admin/users/status | POST | AdminUpdateUserStatusPost |
| /api/v1/identity/admin/users/{id}/status | PUT | AdminUpdateUserStatusPut |
| /api/v1/identity/register | POST | LegacyRegister |
| /api/v1/identity/verify | POST | LegacyVerify |
| /api/v1/identity/resend-verification | POST | LegacyResendVerification |
| /api/v1/identity/login | POST | LegacyLogin |
| /api/v1/identity/refresh | POST | LegacyRefresh |
| /api/v1/identity/logout | POST | LegacyLogout |
| /api/v1/identity/users/{id} | GET | LegacyGetUserById |

## emcore-inspection-trust-api

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

## emcore-inventory-media-api

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

## emcore-mcp-gateway

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

## emcore-notification-integration-api

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

## emcore-portal-bff

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

## emcore-public-bff

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

## emcore-realtime-gateway

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

## emcore-search-discovery-api

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

## emcore-subscription-payment-api

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

## emcore-user-organization-api

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

## emcore-workflow-scheduler-api

| Route | Method | Contract (OpId) |
|---|---|---|
| /health/live | GET | getHealthLive |
| /health/ready | GET | getHealthReady |
| /api/v1/system/version | GET | getApiV1SystemVersion |

