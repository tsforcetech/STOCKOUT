using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Domain.Entities;
using Emcore.IdentityAccess.Domain.Enums;
using Emcore.BuildingBlocks.Core;

namespace Emcore.IdentityAccess.Application.Abstractions;

public record UserLookupResult(
    string Id,
    string UlidId,
    string Status,
    string Email,
    string NormalizedEmail,
    bool EmailVerified,
    string? Mobile,
    string? NormalizedMobile,
    bool MobileVerified,
    string? PasswordHash,
    string? HashAlgorithm,
    int FailedCount,
    DateTime? LockoutEndUtc,
    int SecurityVersion = 1,
    bool MfaEnabled = false
);

public interface IIdentityRepository
{
    Task<Result<UserAccount>> RegisterUserAsync(
        string email, 
        string mobile, 
        string passwordHash, 
        string hashAlgorithm, 
        AccountVerification verification,
        string? outboxPayload,
        CancellationToken cancellationToken);

    Task<Result<UserLookupResult>> GetUserByIdentifierAsync(string emailOrMobile, CancellationToken cancellationToken);
    Task<Result<UserLookupResult>> GetUserByIdAsync(string userId, CancellationToken cancellationToken);

    Task<Result> CreateVerificationAsync(AccountVerification verification, CancellationToken cancellationToken);
    Task<Result> VerifyAccountAsync(string userId, string channel, string tokenHash, string? outboxPayload, CancellationToken cancellationToken);
    
    Task<Result> RecordLoginAttemptAsync(string userId, bool isSuccess, int lockoutMinutes, int maxFailures, string? outboxPayload, CancellationToken cancellationToken);
    
    Task<Result> CreateSessionAsync(UserSession session, RefreshToken refreshToken, CancellationToken cancellationToken);
    Task<Result<(string UserId, string SessionId)>> RotateRefreshTokenAsync(string oldTokenHash, RefreshToken newRefreshToken, string? outboxPayload, CancellationToken cancellationToken);
    Task<Result> RevokeSessionAsync(string sessionId, string userId, string? outboxPayload, CancellationToken cancellationToken);
    Task<Result> RevokeAllSessionsAsync(string userId, CancellationToken cancellationToken);
    Task<Result<List<UserSession>>> GetSessionsAsync(string userId, CancellationToken cancellationToken);
    
    Task<Result> CreateRecoveryRequestAsync(PasswordRecovery recovery, CancellationToken cancellationToken);
    Task<Result> ResetPasswordAsync(string userId, string tokenHash, string newPasswordHash, string hashAlgorithm, string? outboxPayload, CancellationToken cancellationToken);
    Task<Result> ChangePasswordAsync(string userId, string oldPasswordHash, string newPasswordHash, string hashAlgorithm, string? outboxPayload, CancellationToken cancellationToken);
    
    Task<Result<(bool IsCompleted, int StatusCode, string ResponseBody)>> BeginIdempotentRequestAsync(string idempotencyKey, string requestHash, CancellationToken cancellationToken);
    Task<Result> CompleteIdempotentRequestAsync(string idempotencyKey, int statusCode, string responseBody, CancellationToken cancellationToken);

    // MFA & Step-Up Support
    Task<Result> SaveMfaMethodAsync(MfaMethod mfaMethod, string? outboxPayload, CancellationToken cancellationToken);
    Task<Result<MfaMethod?>> GetMfaMethodAsync(string userId, string type, CancellationToken cancellationToken);
    Task<Result> DeleteMfaMethodAsync(string userId, string type, string? outboxPayload, CancellationToken cancellationToken);
    Task<Result> SaveRecoveryCodesAsync(List<MfaRecoveryCode> codes, string? outboxPayload, CancellationToken cancellationToken);
    Task<Result<List<MfaRecoveryCode>>> GetRecoveryCodesAsync(string userId, CancellationToken cancellationToken);
    Task<Result> ConsumeRecoveryCodeAsync(string id, string? outboxPayload, CancellationToken cancellationToken);
    Task<Result> CreateStepUpChallengeAsync(StepUpChallenge challenge, CancellationToken cancellationToken);
    Task<Result<StepUpChallenge?>> GetStepUpChallengeAsync(string id, string userId, CancellationToken cancellationToken);
    Task<Result> UpdateStepUpChallengeAsync(StepUpChallenge challenge, CancellationToken cancellationToken);
    
    // Service Clients & Workload Identities
    Task<Result<ServiceClient>> CreateServiceClientAsync(ServiceClient client, ServiceClientCredential credential, List<ServiceClientScope> scopes, string? outboxPayload, CancellationToken cancellationToken);
    Task<Result<ServiceClientCredential>> RotateServiceClientCredentialAsync(string serviceClientId, ServiceClientCredential newCredential, string? outboxPayload, CancellationToken cancellationToken);
    Task<Result> RevokeServiceClientCredentialAsync(string credentialId, string? outboxPayload, CancellationToken cancellationToken);
    Task<Result<ServiceClientCredential?>> GetServiceClientCredentialAsync(string clientSecretHash, CancellationToken cancellationToken);
    Task<Result<List<ServiceClientCredential>>> ListServiceClientCredentialsAsync(string serviceClientId, CancellationToken cancellationToken);
    Task<Result<List<string>>> GetServiceClientScopesAsync(string serviceClientId, CancellationToken cancellationToken);

    // Administrative Security Actions & Auditing
    Task<Result> UpdateUserStatusAsync(string userId, AccountStatus status, string? reason, string actor, string? outboxPayload, CancellationToken cancellationToken);
    Task<Result> SaveSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken);
}

public interface ITokenGenerator
{
    string GenerateAccessToken(string userId, string email, string sessionId, bool emailVerified, string amr = "pwd");
    (string Token, string Hash) GenerateRefreshToken();
    (string Token, string Hash) GenerateVerificationToken();
    (string Token, string Hash) GenerateKeyedVerificationToken(string verificationId, string normalizedDestination);
    (string Token, string Hash) GeneratePasswordResetToken();
    string HashToken(string rawToken);
    string HashKeyedToken(string verificationId, string normalizedDestination, string rawOtp);
}

public interface IJwksService
{
    string GetJwksJson();
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string AlgorithmName { get; }
}

public interface IVerificationDeliveryService
{
    Task SendVerificationOtpAsync(string destination, string channel, string plaintextOtp, CancellationToken ct);
    Task SendRecoveryTokenAsync(string destination, string plaintextToken, CancellationToken ct);
}
