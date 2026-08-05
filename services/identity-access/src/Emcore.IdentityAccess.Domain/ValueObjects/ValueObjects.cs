namespace Emcore.IdentityAccess.Domain.ValueObjects;

public record NormalizedEmail(string Value);
public record NormalizedMobile(string Value);
public record PasswordPolicyResult(bool IsValid, string[] Errors);

public record UserEmail(string Original, NormalizedEmail Normalized, bool IsVerified);
public record UserMobile(string Original, NormalizedMobile Normalized, bool IsVerified);
