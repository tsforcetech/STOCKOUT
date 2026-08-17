namespace Emcore.IdentityAccess.Application.Configuration;

public class IdentityOptions
{
    public const string SectionName = "Identity";

    public int RefreshTokenLifetimeDays { get; set; } = 30;
    public int VerificationLifetimeMinutes { get; set; } = 10;
    public int PasswordResetLifetimeMinutes { get; set; } = 15;
    public int MaximumFailedLoginAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public int MinimumPasswordLength { get; set; } = 12;
    public int IdempotencyRetentionHours { get; set; } = 24;
}
