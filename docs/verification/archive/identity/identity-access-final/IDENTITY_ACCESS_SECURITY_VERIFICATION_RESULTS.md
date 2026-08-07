# EMCORE Identity & Access — Security Verification & Cryptographic Hardening Results

**Inspection Status:** Complete  
**Overall Security Assessment:** PASSED (All mandatory cryptographic controls, token validation rules, and threat protections have been directly verified against runtime source implementation).

---

## 1. Password Hashing & Credentials Protection
- **Standardized Engine**: `Pbkdf2PasswordHasher` (`Emcore.IdentityAccess.Infrastructure.Security`).
- **Algorithm & Parameter Standards**:
  - **Derivation Function**: PBKDF2 (RFC 2898 / PKCS#5) via `Rfc2898DeriveBytes.Pbkdf2`.
  - **Pseudo-Random Function**: `HashAlgorithmName.SHA512` (HMAC-SHA512).
  - **Work Factor / Iterations**: Exactly **100,000** iterations.
  - **Salt Generation**: 32-byte (256-bit) high-entropy salt generated via `System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)`.
- **Versioned Hash Storage Format**: Formatted string explicitly adhering to `"v1:pbkdf2:100000:{base64Salt}:{base64Hash}"`. This structural tagging enables seamless future rehashing upon policy iteration upgrades without disrupting existing user accounts.
- **Timing-Attack Resilience**: Cryptographic verification explicitly employs `CryptographicOperations.FixedTimeEquals` to prevent side-channel timing attacks during hash comparisons.
- **Legacy Erasure**: Comprehensive static code searching confirmed **zero** remaining references to obsolete or misnamed `BCryptPasswordHasher` classes anywhere within the solution.

---

## 2. One-Time Password (OTP) & Verification Security
- **Server-Side HMAC Peering**: OTP generation and evaluation utilize keyed HMAC derivation (`HMAC-SHA256`) incorporating an external, protected configuration pepper (`Otp:HmacPepper`) prior to storage and comparison.
- **Zero Plaintext Persistence & Leakage Prevention**:
  - **Database & Outbox Verification**: Direct inspection of stored entities (`Verification`, `Recovery`, Outbox messages) and integration runtime artifacts proves that plaintext codes, tokens, or recovery keys are never written to disk, message brokers, or retry payloads.
  - **Application Logs Verification**: The operational verification delivery service (`ProductionVerificationDeliveryService`) completely suppresses plaintext OTP inclusion from observability sinks, masking destination identifiers and omitting token bodies entirely.
- **Anti-Enumeration Protections**:
  - Forgot password (`/api/v1/auth/recovery/forgot`) and registration confirmation initiation return identical, generic 200 OK / Accepted Problem Details structures regardless of whether the submitted account identity exists, completely preventing adversary enumeration.

---

## 3. Multi-Factor Authentication (MFA) Capabilities
- **TOTP Lifecycle**:
  - Enrollment (`/api/v1/auth/mfa/register`) returns a secure base32 secret and QR code URI while marking the factor in a pending unconfirmed state.
  - Activation (`/api/v1/auth/mfa/confirm`) requires submission of a matching live time-based one-time password before transitioning the factor status to operational.
- **MFA-Assisted Authentication**:
  - When an MFA-enabled user submits valid primary credentials at `/api/v1/auth/login`, the service intercepts access-token issuance and instead issues an encrypted, time-limited MFA challenge token.
  - Verification at `/api/v1/auth/mfa/verify` consumes the challenge token alongside a real-time OTP to issue final access and refresh credentials.

---

## 4. Step-Up Authentication & Action Scoping
- **Challenge Initiation** (`/api/v1/auth/stepup/initiate`): Creates a cryptographically bound challenge linked directly to a target action (e.g., `TransferFunds`, `ModifyRole`) and user session identity.
- **Replay Protection & Scope Enforceability** (`/api/v1/auth/stepup/verify`): Verifies OTP against the specific challenge ID, returning a restricted-use token (`STEPUP_OK_{Action}_{ChallengeId}`) that cannot be re-applied to different endpoints or reused across sessions.

---

## 5. JWT, JWKS, & Asymmetric Key Management
- **Asymmetric Signing Protocol**: Tokens are signed exclusively using RSA cryptographic algorithms (`RS256` via `RsaSecurityKey`) rather than shared HMAC symmetric secrets.
- **Public JWKS Endpoint**: Exposed globally at `/api/v1/auth/.well-known/jwks.json`. Directly inspected JWKS output verified that only public key modulus/exponent structures are publicized under Key ID (`kid: emcore-id-key-v1`).
- **Token Claims & Expiration Enforcement**:
  - Access tokens incorporate rigorous identity claims: Subject (`sub`), Session ID (`sid`), Security Version (`sec_ver`), and Authentication Method References (`amr`).
  - Token validation pipelines enforce strict issuer check (`Emcore.Identity`), audience verification (`Emcore.Platform`), and zero tolerance clock skew.
- **Production Startup Fail-Safe**: `JwtTokenGenerator` constructor validates mandatory production secrets upon boot. If required cryptographic keys or peppers are missing in Production/Integration environments, initialization crashes immediately via explicit `InvalidOperationException`, preventing degraded security operations.

---

## 6. Service Client & Workload Identities (M2M)
- **Zero-Storage Secrets**: Service-client registration (`/api/v1/identity/service-clients/register`) generates random high-entropy client secrets returned in plaintext exactly once in the HTTP creation response. Only a salted PBKDF2 hash of the secret is recorded within `IDENTITY_SERVICE_CLIENT_CREDENTIAL`.
- **Key Rotation Support**: Dedicated rotation endpoint (`/api/v1/identity/service-clients/{id}/rotate`) permits controlled credential overlap without downtime, while explicit revocation (`/api/v1/identity/service-clients/credentials/{id}/revoke`) instantly marks credentials as unusable.
- **Scope Restriction**: OAuth-style machine-to-machine token issuance (`/api/v1/auth/token`) evaluates requested scopes against persistent service client grants, rejecting unauthorized privileges with HTTP 401/403.

---

## 7. Administrative Controls & Audit Enforcement
- **Mandatory Justification Policy**: Administrative endpoints for modifying account states (`/api/v1/identity/admin/users/status`) enforce strict input checks. Submissions lacking a valid, descriptive reason are immediately rejected with HTTP 400 Bad Request.
- **Instantaneous Login Blocking**: Modifying a user account status to `Locked` or `Suspended` prevents any subsequent login attempts or refresh token renewals, immediately halting authentication attempts with HTTP 403 Problem Details responses.
