# IdentityAccess Step-Up Authentication Hardening Report

## Executive Summary
This report summarizes the security hardening measures implemented for the Step-Up Authentication flow within the Identity Access service. The objective was to implement secure, one-time Step-Up verification that binds to the user session and target action, eliminating the leakage of ChallengeTokens and generating secure proofs.

## Changes Implemented

### API Gateway & Session Id
- Extracted the `sid` claim from the incoming JWT token at the API Gateway.
- Forwarded the Session ID downstream via the `X-Session-Id` header.
- Updated `ICurrentUser` interface in `Emcore.BuildingBlocks.Security` to include `SessionId` and populate it from the forwarded header.

### Identity Access Domain & Migrations
- Added `SessionId` to the `STEP_UP_CHALLENGE` table (Migration `010`).
- Created a new `STEP_UP_PROOF` table with `SessionId`, `TargetAction`, and `ProofHash` constraints for secure proof issuance.
- Added corresponding entities and stored procedure calls to `IdentityRepository`.

### Application Logic Hardening
- **Token Exposure Removed:** `InitiateStepUpAsync` no longer returns the `ChallengeToken` in its API response payload. It has been replaced with an empty string, and clients now rely on `ExpiresInSeconds`.
- **Session Binding:** Step-Up challenges now capture and bind to the `SessionId` of the user initiating the challenge. Verification fails if the session ID differs.
- **Atomic Attempts & Max Retry:** `ConsumeStepUpChallengeAsync` tracks attempt counts inside the database via `UPDLOCK, ROWLOCK` hints, locking out verification after a set number of failures to prevent brute force attacks.
- **Cryptographic Proofs:** After successful verification, instead of returning an insecure, deterministic `STEPUP_OK_*` string, the system generates a 32-byte cryptographic nonce, stores its hash in `STEP_UP_PROOF`, and returns the raw base64 string as the proof.

### Tests
- Added `HandlersStepUpTests.cs` to verify that `ChallengeToken` is not exposed and that proofs are generated securely.
- Added `StepUpIntegrationTests.cs` that performs end-to-end testing of the secure Step-Up flow, including correct verification and lockout mechanisms.

## Future Recommendations
- Implement automatic expiration sweepers for `STEP_UP_PROOF` and `STEP_UP_CHALLENGE` records.
- Standardize the `SessionId` extraction pattern across other authentication middleware.
