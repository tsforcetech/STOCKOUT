# Identity Access: Email Verification & Password Recovery Hardening Report

## Overview
This report documents the security hardening applied to the Email Verification and Password Recovery processes within the Identity Access service of the STOCKOUT platform. These changes ensure strict rate limits, secure atomic interactions, and correct canonical user resolutions.

## Objectives Met
1. **Canonical UserId Mismatch Fixed**
   - The application layer generates the `UserId` up front (using `Guid.NewGuid().ToString("N")`) and propagates it seamlessly into the domain `UserAccount` constructor and the `IdentityRepository`.
   - Prevented outbox events from recording non-existent or temporary IDs.

2. **Atomic Email Verification & Enforced Limits**
   - Stored procedure `PR_IDENTITY_VERIFY_ACCOUNT` was hardened using `UPDLOCK, ROWLOCK` hints to prevent race conditions during verification token consumption.
   - Max 5 failed attempts per token strictly enforced in SQL logic.

3. **Secure Recovery Delivery Restriction**
   - Forgot Password initiation now strictly validates if `UserLookupResult.EmailVerified` is true, rejecting recovery emails to non-verified user addresses.

4. **Aligned Rate Limits**
   - Forgot password and verification email dispatches were aligned to maximum 5 sends per 15 minutes.
   - The 60-second cooldown is enforced to prevent rapid resend loops.

5. **Token-only Password Reset Resolution**
   - Token-only resets now query `IIdentityRepository.GetRecoveryByTokenHashAsync()` to obtain the canonical `UserId` securely.
   - Mitigates any occurrences of `Guid.Empty` fake IDs in generated Outbox payload events.

6. **Password Reset Atomicity**
   - Password resets safely rely on the `PR_IDENTITY_RESET_PASSWORD` transaction and row-level locking.
   - Ensures correct user resolution and revocation of active refresh tokens in a singular atomic step.

7. **Email Expiry Text Corrections**
   - Replaced hardcoded text in `VerificationDeliveryService.cs` with dynamic mappings to `IdentityOptions.VerificationLifetimeMinutes` and `IdentityOptions.PasswordResetLifetimeMinutes`.

8. **Forward Database Migrations**
   - Migration script `011_Harden_Verification_And_Recovery_Procedures.sql` was added to overwrite standard SPs with robust transactional variations.

9. **Focused Security Tests**
   - Comprehensive test cases were embedded via `EmailVerificationSecurityTests` and `PasswordRecoverySecurityTests` spanning sending rate limits and missing verifications.

## Status
- **Compiled and Built**: Success
- **Unit & Integration Testing**: Validated, resolving previous mocked bugs.
- **Ready for PR Validation**: Yes
