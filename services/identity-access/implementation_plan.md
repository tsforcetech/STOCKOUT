# Identity Access: Email Verification & Password Recovery Remediation Plan

## Proposed Changes

### 1. Fix Email Verification Anti-Enumeration & Already-Verified Email Suppression
I will update `SendEmailVerificationAsync` in `Handlers.cs`:
- Early exit with the generic response if the user is already verified (`UserLookupResult.EmailVerified`). No OTP generated, no challenge created, no email sent.
- Unify all early-exit and success returns to use the exact same generic public response: `"If an account exists and requires verification, a verification code has been sent."`
- Ensure unknown emails also receive this generic response without creating any database rows.
- Ensure that accounts in cooldown or at the rate limit receive the generic response without leaking status.

### 2. Fix Purpose-Specific Email Expiry Wording
I will update the `IVerificationDeliveryService` signatures:
#### [MODIFY] [Interfaces.cs](file:///c:/DEV/API%20PROJECT/STOCKOUT/services/identity-access/src/Emcore.IdentityAccess.Application/Abstractions/Interfaces.cs)
- Add `int expiryMinutes` to `SendVerificationOtpAsync` and `SendRecoveryTokenAsync`.

#### [MODIFY] [VerificationDeliveryService.cs](file:///c:/DEV/API%20PROJECT/STOCKOUT/services/identity-access/src/Emcore.IdentityAccess.Infrastructure/Integrations/VerificationDeliveryService.cs)
- Use the passed `expiryMinutes` instead of relying on the globally injected `IdentityOptions`.
- Update HTML and Text email templates to use the passed `expiryMinutes`.

#### [MODIFY] [Handlers.cs](file:///c:/DEV/API%20PROJECT/STOCKOUT/services/identity-access/src/Emcore.IdentityAccess.Application/Commands/Handlers.cs)
- Update all invocations of `SendVerificationOtpAsync` and `SendRecoveryTokenAsync` to pass their respective lifetime limits:
  - Registration/Email Verification: `_options.VerificationLifetimeMinutes`
  - Password Recovery: `_options.PasswordResetLifetimeMinutes`
  - MFA / Step-Up flows: `5` minutes

### 3. Add Missing Security & Integration Tests
I will add the required exhaustive test matrices as specified in the objectives.
#### [MODIFY] [EmailVerificationSecurityTests.cs](file:///c:/DEV/API%20PROJECT/STOCKOUT/services/identity-access/tests/Emcore.IdentityAccess.UnitTests/Application/Security/EmailVerificationSecurityTests.cs)
- Anti-enumeration: Unknown vs Known Unverified vs Known Verified.
- Rate-limiting & cooldown response consistency.
- Email expiry texts (Verifying FakeEmailSender assertions).

#### [MODIFY] [PasswordRecoverySecurityTests.cs](file:///c:/DEV/API%20PROJECT/STOCKOUT/services/identity-access/tests/Emcore.IdentityAccess.UnitTests/Application/Security/PasswordRecoverySecurityTests.cs)
- Recovery expiry texts.
- Anti-enumeration behaviors.

#### [MODIFY] [SecurityHardeningTests.cs](file:///c:/DEV/API%20PROJECT/STOCKOUT/services/identity-access/tests/Emcore.IdentityAccess.IntegrationTests/SecurityHardeningTests.cs)
- SQL Result Semantics (Wrong token, expired, exhausted, consumed).
- Concurrent resets and verification attempts.
- Password policy and session revocation verifications.

## Verification Plan

### Automated Tests
I will run the full validation pipeline specified:
- `dotnet format Emcore.Platform.slnx`
- `dotnet restore Emcore.Platform.slnx`
- `dotnet build Emcore.Platform.slnx -c Release --no-restore`
- `dotnet test ... UnitTests`
- `dotnet test ... IntegrationTests`
- `dotnet test ... ApiGateway.Tests`
- `dotnet test Emcore.Platform.slnx` (Full Regression)
- OpenAPI Generation and Compatibility tests.
- Linux WSL/Docker validation.

### Final Reporting
Update `IDENTITY_ACCESS_EMAIL_VERIFICATION_PASSWORD_RECOVERY_HARDENING_REPORT.md` to indicate final completion of the remediation, appending the required result matrix format.
