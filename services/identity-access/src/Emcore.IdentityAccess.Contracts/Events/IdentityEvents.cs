using System;

namespace Emcore.IdentityAccess.Contracts.Events;

public static class IdentityEventTypes
{
    public const string UserRegisteredV1 = "identity.user.registered.v1";
    public const string UserEmailVerifiedV1 = "identity.user.email-verified.v1";
    public const string UserMobileVerifiedV1 = "identity.user.mobile-verified.v1";
    public const string UserPasswordChangedV1 = "identity.user.password-changed.v1";
    public const string UserSessionRevokedV1 = "identity.user.session-revoked.v1";
    public const string UserLockedV1 = "identity.user.locked.v1";
    public const string UserUnlockedV1 = "identity.user.unlocked.v1";
}

public abstract record IdentityEventBase
{
    public string EventId { get; init; } = Guid.NewGuid().ToString("N");
    public string SchemaVersion { get; init; } = "1.0.0";
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
    public string? TraceId { get; init; }
}

public record UserRegisteredV1Event : IdentityEventBase
{
    public string UserId { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string MobileNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public record UserEmailVerifiedV1Event : IdentityEventBase
{
    public string UserId { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public DateTime VerifiedAtUtc { get; init; }
}

public record UserMobileVerifiedV1Event : IdentityEventBase
{
    public string UserId { get; init; } = string.Empty;
    public string MobileNumber { get; init; } = string.Empty;
    public DateTime VerifiedAtUtc { get; init; }
}

public record UserPasswordChangedV1Event : IdentityEventBase
{
    public string UserId { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty; // e.g., "Reset", "AuthenticatedChange"
    public DateTime ChangedAtUtc { get; init; }
}

public record UserSessionRevokedV1Event : IdentityEventBase
{
    public string UserId { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string RevocationReason { get; init; } = string.Empty;
    public DateTime RevokedAtUtc { get; init; }
}

public record UserLockedV1Event : IdentityEventBase
{
    public string UserId { get; init; } = string.Empty;
    public string LockoutReason { get; init; } = string.Empty;
    public DateTime? LockoutEndUtc { get; init; }
}

public record UserUnlockedV1Event : IdentityEventBase
{
    public string UserId { get; init; } = string.Empty;
    public string UnlockedBy { get; init; } = string.Empty;
    public DateTime UnlockedAtUtc { get; init; }
}
