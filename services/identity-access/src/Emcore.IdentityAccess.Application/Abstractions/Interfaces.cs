using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Domain.Entities;
using Emcore.BuildingBlocks.Core;
using System.Collections.Generic;

namespace Emcore.IdentityAccess.Application.Abstractions;

public interface IIdentityRepository
{
    Task<Result<UserAccount>> RegisterUserAsync(string email, string mobile, string passwordHash, string hashAlgorithm, CancellationToken cancellationToken);
    Task<Result> CreateVerificationAsync(AccountVerification verification, CancellationToken cancellationToken);
    Task<Result> VerifyAccountAsync(string userId, string verificationId, CancellationToken cancellationToken);
    Task<Result> ResendVerificationAsync(string userId, AccountVerification newVerification, CancellationToken cancellationToken);
    
    Task<Result<UserCredential>> GetUserCredentialAsync(string emailOrMobile, CancellationToken cancellationToken);
    Task<Result<LoginAttempt>> GetLoginAttemptAsync(string userId, CancellationToken cancellationToken);
    Task<Result> RecordLoginAttemptAsync(string userId, bool isSuccess, DateTime? lockoutEndUtc, CancellationToken cancellationToken);
    
    Task<Result> CreateSessionAsync(UserSession session, RefreshToken refreshToken, CancellationToken cancellationToken);
    Task<Result> RotateRefreshTokenAsync(string oldTokenFamily, RefreshToken newRefreshToken, CancellationToken cancellationToken);
    Task<Result> RevokeSessionAsync(string sessionId, string userId, CancellationToken cancellationToken);
    Task<Result> RevokeAllSessionsAsync(string userId, CancellationToken cancellationToken);
    Task<Result<List<UserSession>>> GetSessionsAsync(string userId, CancellationToken cancellationToken);
    
    Task<Result> CreateRecoveryRequestAsync(PasswordRecovery recovery, CancellationToken cancellationToken);
    Task<Result> ResetPasswordAsync(string recoveryId, string newPasswordHash, string hashAlgorithm, CancellationToken cancellationToken);
    
    Task<Result> BeginIdempotentRequestAsync(string idempotencyKey, string requestHash, CancellationToken cancellationToken);
    Task<Result> CompleteIdempotentRequestAsync(string idempotencyKey, int statusCode, string responseBody, CancellationToken cancellationToken);
}

public interface ITokenGenerator
{
    string GenerateAccessToken(string userId, string email, string sessionId);
    (string Token, string Hash) GenerateRefreshToken();
    (string Token, string Hash) GenerateVerificationToken();
    (string Token, string Hash) GeneratePasswordResetToken();
}

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string AlgorithmName { get; }
}
