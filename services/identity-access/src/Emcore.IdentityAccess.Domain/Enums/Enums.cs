namespace Emcore.IdentityAccess.Domain.Enums;

public enum AccountStatus { PendingVerification, Active, Locked, Suspended, Closed }
public enum VerificationStatus { Issued, Verified, Expired, Cancelled }
public enum SessionStatus { Active, Revoked, Expired, Compromised }
public enum RecoveryStatus { Created, Verified, Completed, Expired, Cancelled }

public static class MfaMethodTypes
{
    public const string EmailOtp = "EMAIL_OTP";
}
