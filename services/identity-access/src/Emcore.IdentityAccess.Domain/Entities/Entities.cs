using System;
using System.Collections.Generic;
using Emcore.IdentityAccess.Domain.Enums;
using Emcore.IdentityAccess.Domain.ValueObjects;

namespace Emcore.IdentityAccess.Domain.Entities;

public class UserAccount
{
    public string Id { get; private set; } = Guid.NewGuid().ToString("N");
    public string UlidId { get; private set; } = Guid.NewGuid().ToString("N")[..26].ToUpperInvariant();
    public UserEmail Email { get; private set; }
    public UserMobile Mobile { get; private set; }
    public AccountStatus Status { get; private set; }
    public string? StatusReason { get; private set; }
    public int SecurityVersion { get; private set; } = 1;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public UserAccount(UserEmail email, UserMobile mobile)
    {
        Email = email;
        Mobile = mobile;
        Status = AccountStatus.PendingVerification;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public void Verify()
    {
        if (Status != AccountStatus.PendingVerification) throw new Exception("Account is not pending verification.");
        Status = AccountStatus.Active;
        Email = Email with { IsVerified = true };
        if (Mobile != null) Mobile = Mobile with { IsVerified = true };
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void VerifyEmail()
    {
        Email = Email with { IsVerified = true };
        if (Status == AccountStatus.PendingVerification) Status = AccountStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void VerifyMobile()
    {
        if (Mobile != null) Mobile = Mobile with { IsVerified = true };
        if (Status == AccountStatus.PendingVerification) Status = AccountStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void IncrementSecurityVersion() { SecurityVersion++; UpdatedAtUtc = DateTime.UtcNow; }
    public void Lock(string? reason = null) { Status = AccountStatus.Locked; StatusReason = reason; UpdatedAtUtc = DateTime.UtcNow; }
    public void Suspend(string? reason = null) { Status = AccountStatus.Suspended; StatusReason = reason; UpdatedAtUtc = DateTime.UtcNow; }
    public void Restore(string? reason = null) { Status = AccountStatus.Active; StatusReason = reason; UpdatedAtUtc = DateTime.UtcNow; }
    public void Unlock(string? reason = null) { Status = AccountStatus.Active; StatusReason = reason; UpdatedAtUtc = DateTime.UtcNow; }
}

public class UserCredential
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string HashAlgorithm { get; set; } = "PBKDF2-SHA512-V1";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AccountVerification
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public VerificationStatus Status { get; set; } = VerificationStatus.Issued;
    public string Channel { get; set; } = "Email"; // Email or Mobile
    public int AttemptCount { get; set; } = 0;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsExpired(DateTime utcNow) => utcNow > ExpiresAtUtc || Status == VerificationStatus.Expired;
    
    public void RecordFailedAttempt(int maxAttempts = 5)
    {
        AttemptCount++;
        if (AttemptCount >= maxAttempts)
        {
            Status = VerificationStatus.Expired;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Consume()
    {
        if (Status != VerificationStatus.Issued) throw new InvalidOperationException("Verification challenge already consumed or expired.");
        Status = VerificationStatus.Verified;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class UserSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public string TokenFamilyId { get; set; } = string.Empty;
    public int SecurityVersion { get; set; } = 1;
    public string? DeviceLabel { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime LastActivityAtUtc { get; set; } = DateTime.UtcNow;

    public void Revoke()
    {
        if (Status == SessionStatus.Active)
        {
            Status = SessionStatus.Revoked;
            RevokedAtUtc = DateTime.UtcNow;
        }
    }
}

public class RefreshToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SessionId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string FamilyId { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public void Revoke(string? replacementHash = null)
    {
        IsRevoked = true;
        RevokedAtUtc = DateTime.UtcNow;
        ReplacedByTokenHash = replacementHash;
    }

    public bool IsExpired(DateTime utcNow) => utcNow > ExpiresAtUtc;
}

public class PasswordRecovery
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public RecoveryStatus Status { get; set; } = RecoveryStatus.Created;
    public int AttemptCount { get; set; } = 0;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsExpired(DateTime utcNow) => utcNow > ExpiresAtUtc || Status == RecoveryStatus.Expired;

    public void Consume()
    {
        if (Status != RecoveryStatus.Created) throw new InvalidOperationException("Recovery challenge is no longer valid.");
        Status = RecoveryStatus.Completed;
    }
}

public class LoginAttempt
{
    public string UserId { get; set; } = string.Empty;
    public int FailedCount { get; set; } = 0;
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsLocked(DateTime utcNow) => LockoutEndUtc.HasValue && LockoutEndUtc.Value > utcNow;

    public void RecordFailure(int maxFailures, int lockoutMinutes, DateTime utcNow)
    {
        FailedCount++;
        UpdatedAtUtc = utcNow;
        if (FailedCount >= maxFailures)
        {
            LockoutEndUtc = utcNow.AddMinutes(lockoutMinutes);
        }
    }

    public void Reset()
    {
        FailedCount = 0;
        LockoutEndUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class PasswordHistory
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ServiceClient
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecretHash { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class OutboxMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }
    public int AttemptCount { get; set; } = 0;
    public string? LastError { get; set; }
}

public class MfaMethod
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = "TOTP";
    public string EncryptedSecret { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public void Enable() { IsEnabled = true; UpdatedAtUtc = DateTime.UtcNow; }
    public void Disable() { IsEnabled = false; UpdatedAtUtc = DateTime.UtcNow; }
}

public class MfaRecoveryCode
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public bool IsConsumed { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ConsumedAtUtc { get; set; }

    public void Consume()
    {
        if (IsConsumed) throw new InvalidOperationException("Recovery code has already been consumed.");
        IsConsumed = true;
        ConsumedAtUtc = DateTime.UtcNow;
    }
}

public class StepUpChallenge
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string TargetAction { get; set; } = string.Empty;
    public string Status { get; set; } = "Issued";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsValid(DateTime utcNow, string targetAction) =>
        Status == "Issued" && utcNow <= ExpiresAtUtc && string.Equals(TargetAction, targetAction, StringComparison.OrdinalIgnoreCase);

    public void Verify() { Status = "Verified"; }
}

public class ServiceClientCredential
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ServiceClientId { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }

    public bool IsActive(DateTime utcNow) => !IsRevoked && utcNow <= ExpiresAtUtc;
    public void Revoke() { IsRevoked = true; RevokedAtUtc = DateTime.UtcNow; }
}

public class ServiceClientScope
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ServiceClientId { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class SecurityEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string TargetUserId { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? RequestId { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
