# EMCORE Identity & Access — Database Object & Persistence Verification Report

**Verification Method**: Direct inspection of migration T-SQL scripts (`Emcore.IdentityAccess.Migrator/Migrations/Versioned`), static repository security grep, and automated migrator execution validation (`sql_migrator_validation.log`).

---

## 1. Dirty Read & Transactional Safety Verification
- **NOLOCK & READ UNCOMMITTED Search**: Comprehensive literal and regex pattern scanning across all source files and SQL scripts confirmed **0 matches** for `NOLOCK`, `READUNCOMMITTED`, or `READ UNCOMMITTED`. All authentication, credential verification, lockout, MFA, recovery, session, and account status evaluations execute strictly under safe read committed transaction isolation levels.
- **Transactional Consistency**: All state-changing stored procedures explicitly declare `SET XACT_ABORT ON` and `SET NOCOUNT ON`, ensuring immediate runtime rollback upon constraint or deadlock failures without phantom result sets.

---

## 2. Verified Database Table Inventory

| Table Physical Name | Primary Key | Critical / Sensitive Columns | Hash / Security Treatment | Purpose |
|---|---|---|---|---|
| `IDENTITY_USER_ACCOUNT` | `Id` (Guid) | `Email`, `Username`, `Status`, `SecurityVersion` | Indexed unique email/username; enum-constrained status | Holds core authentication identity and lock status. |
| `IDENTITY_USER_CREDENTIAL` | `Id` (Guid) | `UserId` (FK), `PasswordHash`, `Salt` | Contains versioned PBKDF2-SHA512 hash; zero plaintext | Stores current user authentication credential. |
| `IDENTITY_PASSWORD_HISTORY` | `Id` (Guid) | `UserId` (FK), `PasswordHash`, `CreatedAt` | Retains past PBKDF2 hashes for reuse prevention | Enforces password history and reuse prohibition rules. |
| `IDENTITY_VERIFICATION` | `Id` (Guid) | `UserId` (FK), `HashedToken`, `ExpiresAt` | HMAC-SHA256 salted code hash; attempt counter | Tracks email/phone verification challenge lifespans. |
| `IDENTITY_RECOVERY` | `Id` (Guid) | `UserId` (FK), `HashedToken`, `Used` | One-time recovery hash; strict timestamp expiration | Governs secure account password resets and recovery codes. |
| `IDENTITY_MFA_METHOD` | `Id` (Guid) | `UserId` (FK), `Secret`, `Type`, `IsConfirmed` | Encrypted/hashed TOTP secret parameter | Manages multi-factor authentication enrollment status. |
| `IDENTITY_MFA_RECOVERY_CODE` | `Id` (Guid) | `UserId` (FK), `CodeHash`, `IsUsed` | Cryptographically derived one-time hash codes | Provides fallback access code verification for MFA users. |
| `IDENTITY_STEP_UP_CHALLENGE` | `Id` (Guid) | `UserId` (FK), `Action`, `ChallengeToken` | Action-scoped challenge tokens with strict expiry | Protects sensitive administrative workflows. |
| `IDENTITY_SESSION` | `Id` (Guid) | `UserId` (FK), `SessionTokenHash`, `IsRevoked` | Hashed device session tokens with revocation flag | Coordinates multi-device access session validity. |
| `IDENTITY_REFRESH_TOKEN` | `Id` (Guid) | `TokenHash`, `UserId`, `FamilyId`, `IsRevoked` | Hashed refresh tokens with family lineage tracing | Governs JWT refresh rotation and reuse compromise detection. |
| `IDENTITY_LOGIN_ATTEMPT` | `Id` (Guid) | `Email`, `IpAddress`, `IsSuccess`, `Timestamp` | Audit parameters for failed login counter evaluations | Powers automatic rate limiting and account lockout rules. |
| `IDENTITY_SERVICE_CLIENT` | `Id` (Guid) | `ClientIdentifier`, `Status`, `Scopes` | Scope constraint string lists; activation status | Represents machine-to-machine workload service accounts. |
| `IDENTITY_SERVICE_CLIENT_CREDENTIAL`| `Id` (Guid) | `ClientId` (FK), `SecretHash`, `IsRevoked` | Hashed machine secret; timestamps; revocation flags | Tracks active and rotated credentials for workloads. |
| `IDENTITY_OUTBOX` | `Id` (Guid) | `EventType`, `Payload`, `ProcessedAt`, `Error` | Versioned event payload structures with atomic status | Guarantees reliable publishing of messaging broker events. |
| `IDENTITY_INBOX` | `Id` (Guid) | `MessageId`, `ProcessedAt` | Idempotency message fingerprint logs | Prevents duplicate processing of asynchronous external messages. |

---

## 3. SQL Migrator Script Validation
All eight incremental database migration files were executed via `Emcore.IdentityAccess.Migrator` in `--validate` and `--dry-run` modes. The migrator successfully validated syntax, checksum integrity, and deployment sequence:
- `001_Create_Identity_Core_Tables.sql` (Checksum: `ZMmKMJ+RC/VUxPvwnZAX+yMoUrcY+ZswGwGDeolWvSQ=`) -> **VALID / RUN READY**
- `002_Create_Verification_And_Recovery_Tables.sql` -> **VALID / RUN READY**
- `003_Create_Session_And_Refresh_Token_Tables.sql` -> **VALID / RUN READY**
- `004_Create_Outbox_Inbox_And_Idempotency.sql` -> **VALID / RUN READY**
- `005_Create_Identity_Indexes_And_Constraints.sql` -> **VALID / RUN READY**
- `006_Create_Identity_Stored_Procedures.sql` -> **VALID / RUN READY**
- `007_Create_Identity_Cleanup_Procedures.sql` -> **VALID / RUN READY**
- `008_Add_Mfa_ServiceClients_And_Hardening.sql` (Checksum: `n4mRFGYnmTvW+gLEohWNrLxm8mF0fPs9R7AKVaAA48g=`) -> **VALID / RUN READY**
