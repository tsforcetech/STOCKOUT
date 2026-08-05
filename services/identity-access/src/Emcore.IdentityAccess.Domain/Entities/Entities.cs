using System;
using System.Collections.Generic;
using Emcore.IdentityAccess.Domain.Enums;
using Emcore.IdentityAccess.Domain.ValueObjects;

namespace Emcore.IdentityAccess.Domain.Entities;

public class UserAccount
{
    public string Id { get; private set; } = Guid.NewGuid().ToString("N");
    public UserEmail Email { get; private set; }
    public UserMobile Mobile { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public UserAccount(UserEmail email, UserMobile mobile)
    {
        Email = email;
        Mobile = mobile;
        Status = AccountStatus.PendingVerification;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Verify()
    {
        if (Status != AccountStatus.PendingVerification) throw new Exception("Account is not pending verification.");
        Status = AccountStatus.Active;
        Email = Email with { IsVerified = true };
        if (Mobile != null) Mobile = Mobile with { IsVerified = true };
    }

    public void Lock() => Status = AccountStatus.Locked;
    public void Suspend() => Status = AccountStatus.Suspended;
    public void Unlock() => Status = AccountStatus.Active;
}

public class UserCredential
{
    public string UserId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string HashAlgorithm { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class AccountVerification
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public VerificationStatus Status { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class UserSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public SessionStatus Status { get; set; }
    public string TokenFamilyId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}

public class RefreshToken
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SessionId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string FamilyId { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsRevoked { get; set; }
}

public class PasswordRecovery
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public RecoveryStatus Status { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

public class LoginAttempt
{
    public string UserId { get; set; } = string.Empty;
    public int FailedCount { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
}
