using System;
using System.Collections.Generic;

namespace Emcore.IdentityAccess.Application.DTOs;

// Registration & General Verification
public record RegisterRequest(string Email, string Mobile, string Password);
public record RegisterResponse(string UserId, string Email, string Mobile, string Status, string Message = "Registration successful. Please verify your email or mobile number.");

public record VerifyRequest(string UserId, string Token, string Channel);
public record ResendVerificationRequest(string UserId, string Channel);

// Dedicated Verification Endpoints
public record SendEmailVerificationRequest(string Email);
public record ConfirmEmailVerificationRequest(string Email, string Token);

public record SendMobileVerificationRequest(string Mobile);
public record ConfirmMobileVerificationRequest(string Mobile, string Token);

// Authentication & Tokens
public record LoginRequest(string EmailOrMobile, string Password);
public record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, string TokenType = "Bearer", bool MfaRequired = false, string? MfaChallengeToken = null);

public record RefreshRequest(string RefreshToken);
public record RefreshResponse(string AccessToken, string RefreshToken, int ExpiresIn, string TokenType = "Bearer");

public record LogoutRequest(string? RefreshToken);

// Password Lifecycle
public record ForgotPasswordRequest(string EmailOrMobile);
public record ForgotPasswordResponse(string Message = "If an account with that identifier exists, password recovery instructions have been initiated.");

public record ResetPasswordRequest(string Token, string NewPassword, string? EmailOrMobile = null, string? StepUpToken = null);
public record ResetPasswordResponse(string Message = "Password has been reset successfully.");

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string? StepUpToken = null);
public record ChangePasswordResponse(string Message = "Password changed successfully. All other sessions have been revoked.");

// Sessions & Account Status
public record SessionDto(string SessionId, string Status, DateTime CreatedAtUtc, DateTime? RevokedAtUtc, bool IsCurrentSession = false, string? DeviceLabel = null, string? IpAddress = null);

public record AccountStatusResponse(
    string UserId,
    string Email,
    bool EmailVerified,
    string Mobile,
    bool MobileVerified,
    string Status,
    int FailedLoginAttempts,
    DateTime? LockoutEndUtc
);

public record CurrentIdentityResponse(
    string UserId,
    string Email,
    bool EmailVerified,
    string Mobile,
    bool MobileVerified,
    string Status,
    string AuthTime,
    string Amr = "pwd"
);

public record StandardSuccessResponse(string Message);

// MFA & Step-Up Authentication
public record RegisterMfaRequest(string Type = "EMAIL_OTP");
public record RegisterMfaResponse(string Secret, string QrCodeUri, List<string> RecoveryCodes, string Message = "MFA factor registered. Please confirm with OTP to enable.", string? ChallengeId = null);
public record ConfirmMfaRequest(string Type, string Code, string? ChallengeId = null);
public record MfaLoginVerifyRequest(string UserId, string ChallengeToken, string Code, string? RecoveryCode = null);
public record ResendMfaRequest(string UserId, string ChallengeId);
public record ResendMfaResponse(string Message, string? ChallengeId = null);
public record InitiateStepUpRequest(string TargetAction);
public record InitiateStepUpResponse(string StepUpId, string ChallengeToken, string Message = "Step-up challenge issued. Please verify second factor.");
public record VerifyStepUpRequest(string StepUpId, string Code);
public record VerifyStepUpResponse(string VerificationToken, string Message = "Step-up verified successfully.");

// Workload & Service Client Identities
public record RegisterServiceClientRequest(string ClientId, List<string> Scopes, int ExpiryDays = 365);
public record RegisterServiceClientResponse(string Id, string ClientId, string ClientSecret, string KeyId, DateTime ExpiresAtUtc, List<string> Scopes);
public record RotateServiceClientCredentialRequest(string ServiceClientId, int ExpiryDays = 365);
public record RotateServiceClientCredentialResponse(string ServiceClientId, string NewClientSecret, string KeyId, DateTime ExpiresAtUtc);
public record RevokeServiceClientCredentialRequest(string CredentialId);
public record ServiceClientCredentialDto(string Id, string ServiceClientId, string KeyId, DateTime ExpiresAtUtc, bool IsRevoked, DateTime CreatedAtUtc);
public record ServiceTokenRequest(string ClientId, string ClientSecret, string? Scope = null);
public record ServiceTokenResponse(string AccessToken, int ExpiresIn, string TokenType = "Bearer");

// Administrative Security & Lockout
public record AdminUpdateUserStatusRequest(string UserId, string Status, string Reason);

