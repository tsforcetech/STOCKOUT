namespace Emcore.IdentityAccess.Domain.Events;

public record UserRegisteredEvent(string UserId, string Email, string Mobile);
public record UserVerifiedEvent(string UserId);
public record LoginFailedEvent(string UserId, string Reason);
public record SessionRevokedEvent(string SessionId, string UserId);
public record PasswordResetRequestedEvent(string UserId, string RecoveryId);
public record PasswordResetCompletedEvent(string UserId);
public record AccountLockedEvent(string UserId, string Reason);
public record RefreshTokenReuseDetectedEvent(string SessionId, string UserId, string FamilyId);
