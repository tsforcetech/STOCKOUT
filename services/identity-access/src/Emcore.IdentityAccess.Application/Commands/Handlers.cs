using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application.Abstractions;
using Emcore.IdentityAccess.Application.DTOs;
using Emcore.IdentityAccess.Contracts.Events;
using Emcore.IdentityAccess.Domain.Entities;
using Emcore.IdentityAccess.Domain.Enums;

namespace Emcore.IdentityAccess.Application.Commands;

public record AppResult<T>(bool IsSuccess, int StatusCode, string? ErrorTitle, string? ErrorDetail, T? Data)
{
    public static AppResult<T> Success(T data, int statusCode = 200) => new(true, statusCode, null, null, data);
    public static AppResult<T> Failure(int statusCode, string title, string detail) => new(false, statusCode, title, detail, default);
}

public class IdentityApplicationService
{
    private readonly IIdentityRepository _repository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IVerificationDeliveryService? _deliveryService;

    public IdentityApplicationService(
        IIdentityRepository repository,
        ITokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher,
        IVerificationDeliveryService? deliveryService = null)
    {
        _repository = repository;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
        _deliveryService = deliveryService;
    }

    public async Task<AppResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Mobile))
        {
            return AppResult<RegisterResponse>.Failure(400, "Validation Error", "At least an email address or mobile number must be provided.");
        }
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return AppResult<RegisterResponse>.Failure(400, "Invalid Password", "Password must be at least 8 characters long.");
        }

        string passwordHash = _passwordHasher.HashPassword(request.Password);
        var (verificationOtp, otpHash) = _tokenGenerator.GenerateVerificationToken();
        var verification = new AccountVerification
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = Guid.NewGuid().ToString("N"), // placeholder before registration assigns actual ID if needed
            TokenHash = otpHash,
            Channel = !string.IsNullOrWhiteSpace(request.Email) ? "Email" : "Mobile",
            Status = Domain.Enums.VerificationStatus.Issued,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(2)
        };

        var evt = new UserRegisteredV1Event
        {
            UserId = verification.UserId,
            EmailAddress = request.Email ?? string.Empty,
            MobileNumber = request.Mobile ?? string.Empty,
            Status = "PendingVerification"
        };
        string outboxPayload = JsonSerializer.Serialize(evt);

        var result = await _repository.RegisterUserAsync(
            request.Email ?? string.Empty,
            request.Mobile ?? string.Empty,
            passwordHash,
            _passwordHasher.AlgorithmName,
            verification,
            outboxPayload,
            cancellationToken);

        if (result == null || result.Value == null || string.IsNullOrEmpty(result.Value.Id))
        {
            return AppResult<RegisterResponse>.Failure(409, "Conflict", "An account with this email address or mobile number already exists.");
        }

        var user = result.Value;
        if (_deliveryService != null)
        {
            string dest = !string.IsNullOrEmpty(user.Email.Original) ? user.Email.Original : (user.Mobile?.Original ?? string.Empty);
            await _deliveryService.SendVerificationOtpAsync(dest, verification.Channel, verificationOtp, cancellationToken);
        }
        return AppResult<RegisterResponse>.Success(
            new RegisterResponse(user.Id, user.Email.Original, user.Mobile?.Original ?? string.Empty, "PendingVerification"),
            201);
    }

    public async Task<AppResult<StandardSuccessResponse>> SendEmailVerificationAsync(SendEmailVerificationRequest request, CancellationToken cancellationToken)
    {
        var userRes = await _repository.GetUserByIdentifierAsync(request.Email, cancellationToken);
        if (userRes == null || userRes.Value == null)
        {
            // Generic response to prevent enumeration
            return AppResult<StandardSuccessResponse>.Success(new StandardSuccessResponse("If an account exists with this email, a verification code has been sent."));
        }

        var (otp, hash) = _tokenGenerator.GenerateVerificationToken();
        var verification = new AccountVerification
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userRes.Value.Id,
            TokenHash = hash,
            Channel = "Email",
            Status = Domain.Enums.VerificationStatus.Issued,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(2)
        };

        await _repository.CreateVerificationAsync(verification, cancellationToken);
        if (_deliveryService != null)
        {
            await _deliveryService.SendVerificationOtpAsync(userRes.Value.Email, "Email", otp, cancellationToken);
        }
        return AppResult<StandardSuccessResponse>.Success(new StandardSuccessResponse("Verification code sent successfully."));
    }

    public async Task<AppResult<StandardSuccessResponse>> ConfirmEmailVerificationAsync(ConfirmEmailVerificationRequest request, CancellationToken cancellationToken)
    {
        var userRes = await _repository.GetUserByIdentifierAsync(request.Email, cancellationToken);
        if (userRes == null || userRes.Value == null)
        {
            return AppResult<StandardSuccessResponse>.Failure(400, "Verification Failed", "Invalid verification attempt.");
        }

        string tokenHash = _tokenGenerator.HashToken(request.Token.Trim());
        var evt = new UserEmailVerifiedV1Event { UserId = userRes.Value.Id, EmailAddress = request.Email, VerifiedAtUtc = DateTime.UtcNow };

        var verifyRes = await _repository.VerifyAccountAsync(userRes.Value.Id, "Email", tokenHash, JsonSerializer.Serialize(evt), cancellationToken);
        if (verifyRes == null)
        {
            return AppResult<StandardSuccessResponse>.Failure(400, "Invalid Verification Code", "The verification code is incorrect or has expired.");
        }

        return AppResult<StandardSuccessResponse>.Success(new StandardSuccessResponse("Email verified successfully."));
    }

    public async Task<AppResult<StandardSuccessResponse>> SendMobileVerificationAsync(SendMobileVerificationRequest request, CancellationToken cancellationToken)
    {
        var userRes = await _repository.GetUserByIdentifierAsync(request.Mobile, cancellationToken);
        if (userRes == null || userRes.Value == null)
        {
            return AppResult<StandardSuccessResponse>.Success(new StandardSuccessResponse("If an account exists with this mobile number, a verification code has been sent."));
        }

        var (otp, hash) = _tokenGenerator.GenerateVerificationToken();
        var verification = new AccountVerification
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userRes.Value.Id,
            TokenHash = hash,
            Channel = "Mobile",
            Status = Domain.Enums.VerificationStatus.Issued,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(2)
        };

        await _repository.CreateVerificationAsync(verification, cancellationToken);
        if (_deliveryService != null)
        {
            await _deliveryService.SendVerificationOtpAsync(userRes.Value.Mobile ?? string.Empty, "Mobile", otp, cancellationToken);
        }
        return AppResult<StandardSuccessResponse>.Success(new StandardSuccessResponse("Verification code sent successfully."));
    }

    public async Task<AppResult<StandardSuccessResponse>> ConfirmMobileVerificationAsync(ConfirmMobileVerificationRequest request, CancellationToken cancellationToken)
    {
        var userRes = await _repository.GetUserByIdentifierAsync(request.Mobile, cancellationToken);
        if (userRes == null || userRes.Value == null)
        {
            return AppResult<StandardSuccessResponse>.Failure(400, "Verification Failed", "Invalid verification attempt.");
        }

        string tokenHash = _tokenGenerator.HashToken(request.Token.Trim());
        var evt = new UserMobileVerifiedV1Event { UserId = userRes.Value.Id, MobileNumber = request.Mobile, VerifiedAtUtc = DateTime.UtcNow };

        var verifyRes = await _repository.VerifyAccountAsync(userRes.Value.Id, "Mobile", tokenHash, JsonSerializer.Serialize(evt), cancellationToken);
        if (verifyRes == null)
        {
            return AppResult<StandardSuccessResponse>.Failure(400, "Invalid Verification Code", "The verification code is incorrect or has expired.");
        }

        return AppResult<StandardSuccessResponse>.Success(new StandardSuccessResponse("Mobile number verified successfully."));
    }

    public async Task<AppResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var userRes = await _repository.GetUserByIdentifierAsync(request.EmailOrMobile, cancellationToken);
        if (userRes == null || userRes.Value == null || string.IsNullOrEmpty(userRes.Value.Id))
        {
            return AppResult<LoginResponse>.Failure(401, "Unauthorized", "Invalid credentials provided.");
        }

        var u = userRes.Value;
        if (u.LockoutEndUtc.HasValue && u.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            return AppResult<LoginResponse>.Failure(403, "Account Locked", "Account is temporarily locked due to excessive failed login attempts.");
        }
        if (!string.Equals(u.Status, "Active", StringComparison.OrdinalIgnoreCase) && !string.Equals(u.Status, "PendingVerification", StringComparison.OrdinalIgnoreCase))
        {
            return AppResult<LoginResponse>.Failure(403, "Account Disabled", $"Account status is currently {u.Status}. Access is restricted.");
        }

        bool valid = _passwordHasher.VerifyPassword(request.Password, u.PasswordHash ?? string.Empty);
        if (!valid)
        {
            var lockEvt = new UserLockedV1Event { UserId = u.Id, LockoutReason = "Max failed password attempts exceeded", LockoutEndUtc = DateTime.UtcNow.AddMinutes(15) };
            await _repository.RecordLoginAttemptAsync(u.Id, false, 15, 5, JsonSerializer.Serialize(lockEvt), cancellationToken);
            return AppResult<LoginResponse>.Failure(401, "Unauthorized", "Invalid credentials provided.");
        }

        await _repository.RecordLoginAttemptAsync(u.Id, true, 15, 5, null, cancellationToken);

        var mfaRes = await _repository.GetMfaMethodAsync(u.Id, MfaMethodTypes.EmailOtp, cancellationToken);
        if (mfaRes != null && mfaRes.Value != null && mfaRes.Value.IsEnabled)
        {
            int recentSends = await _repository.GetRecentStepUpChallengesCountAsync(u.Id, "MfaLogin", TimeSpan.FromMinutes(15), cancellationToken);
            if (recentSends >= 5)
            {
                return AppResult<LoginResponse>.Failure(429, "Too Many Requests", "Maximum OTP send limit reached for login. Try again later.");
            }

            var (mfaToken, mfaHash) = _tokenGenerator.GenerateVerificationToken();
            var challenge = new StepUpChallenge
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = u.Id,
                TokenHash = mfaHash,
                TargetAction = "MfaLogin",
                Status = "Issued",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
                CreatedAtUtc = DateTime.UtcNow
            };
            await _repository.CreateStepUpChallengeAsync(challenge, cancellationToken);

            if (_deliveryService != null)
            {
                await _deliveryService.SendVerificationOtpAsync(u.Email, MfaMethodTypes.EmailOtp, mfaToken, cancellationToken);
            }

            return AppResult<LoginResponse>.Success(new LoginResponse(string.Empty, string.Empty, 0, "Bearer", true, challenge.Id));
        }

        string sessionId = Guid.NewGuid().ToString("N");
        string tokenFamilyId = Guid.NewGuid().ToString("N");
        var (refToken, refHash) = _tokenGenerator.GenerateRefreshToken();

        var session = new UserSession
        {
            Id = sessionId,
            UserId = u.Id,
            TokenFamilyId = tokenFamilyId,
            Status = Domain.Enums.SessionStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        var refreshTokenObj = new RefreshToken
        {
            Id = Guid.NewGuid().ToString("N"),
            SessionId = sessionId,
            TokenHash = refHash,
            FamilyId = tokenFamilyId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            IsRevoked = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.CreateSessionAsync(session, refreshTokenObj, cancellationToken);

        string accessToken = _tokenGenerator.GenerateAccessToken(u.Id, u.Email, sessionId, u.EmailVerified, "pwd");
        return AppResult<LoginResponse>.Success(new LoginResponse(accessToken, refToken, 900, "Bearer"));
    }

    public async Task<AppResult<RefreshResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return AppResult<RefreshResponse>.Failure(400, "Invalid Request", "Refresh token must be provided.");
        }

        string oldHash = _tokenGenerator.HashToken(request.RefreshToken.Trim());
        var (newRefToken, newRefHash) = _tokenGenerator.GenerateRefreshToken();

        var newRefObj = new RefreshToken
        {
            Id = Guid.NewGuid().ToString("N"),
            TokenHash = newRefHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        };

        var rotRes = await _repository.RotateRefreshTokenAsync(oldHash, newRefObj, null, cancellationToken);
        if (rotRes == null || string.IsNullOrEmpty(rotRes.Value.UserId))
        {
            return AppResult<RefreshResponse>.Failure(401, "Unauthorized", "The refresh token is invalid, expired, or revoked due to reuse detection.");
        }

        var uRes = await _repository.GetUserByIdAsync(rotRes.Value.UserId, cancellationToken);
        if (uRes == null || uRes.Value == null)
        {
            return AppResult<RefreshResponse>.Failure(401, "Unauthorized", "User associated with session not found.");
        }
        var u = uRes.Value;

        string accessToken = _tokenGenerator.GenerateAccessToken(u.Id, u.Email, rotRes.Value.SessionId, u.EmailVerified, "ref");
        return AppResult<RefreshResponse>.Success(new RefreshResponse(accessToken, newRefToken, 900, "Bearer"));
    }

    public async Task<AppResult<StandardSuccessResponse>> LogoutAsync(string? refreshToken, string? userId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken) && !string.IsNullOrWhiteSpace(userId))
        {
            string hash = _tokenGenerator.HashToken(refreshToken);
            await _repository.RevokeSessionAsync(hash, userId, null, cancellationToken);
        }
        return AppResult<StandardSuccessResponse>.Success(new StandardSuccessResponse("Logged out successfully."));
    }

    public async Task<AppResult<StandardSuccessResponse>> LogoutAllAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) return AppResult<StandardSuccessResponse>.Failure(401, "Unauthorized", "User identity required.");

        var evt = new UserSessionRevokedV1Event { UserId = userId, RevocationReason = "LogoutAll", RevokedAtUtc = DateTime.UtcNow };
        await _repository.RevokeAllSessionsAsync(userId, cancellationToken);

        return AppResult<StandardSuccessResponse>.Success(new StandardSuccessResponse("All user sessions have been revoked successfully."));
    }

    public async Task<AppResult<ForgotPasswordResponse>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var response = new ForgotPasswordResponse();
        if (string.IsNullOrWhiteSpace(request.EmailOrMobile)) return AppResult<ForgotPasswordResponse>.Success(response);

        var uRes = await _repository.GetUserByIdentifierAsync(request.EmailOrMobile, cancellationToken);
        if (uRes != null && uRes.Value != null && !string.IsNullOrEmpty(uRes.Value.Id))
        {
            var (resetToken, hash) = _tokenGenerator.GeneratePasswordResetToken();
            var recovery = new PasswordRecovery
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = uRes.Value.Id,
                TokenHash = hash,
                Status = Domain.Enums.RecoveryStatus.Created,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
            };
            await _repository.CreateRecoveryRequestAsync(recovery, cancellationToken);
            if (_deliveryService != null)
            {
                await _deliveryService.SendRecoveryTokenAsync(uRes.Value.Email, resetToken, cancellationToken);
            }
        }

        return AppResult<ForgotPasswordResponse>.Success(response);
    }

    public async Task<AppResult<ResetPasswordResponse>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return AppResult<ResetPasswordResponse>.Failure(400, "Invalid Request", "Token and new password are required.");
        }

        string tokenHash = _tokenGenerator.HashToken(request.Token.Trim());
        string newPassHash = _passwordHasher.HashPassword(request.NewPassword);

        // Find user if identifier passed, otherwise repository procedure locates active recovery challenge by TokenHash
        string userId = request.EmailOrMobile ?? Guid.Empty.ToString();
        if (!string.IsNullOrWhiteSpace(request.EmailOrMobile))
        {
            var uRes = await _repository.GetUserByIdentifierAsync(request.EmailOrMobile, cancellationToken);
            if (uRes != null && uRes.Value != null) userId = uRes.Value.Id;
        }

        var evt = new UserPasswordChangedV1Event { UserId = userId, Reason = "Reset", ChangedAtUtc = DateTime.UtcNow };
        var res = await _repository.ResetPasswordAsync(userId, tokenHash, newPassHash, _passwordHasher.AlgorithmName, JsonSerializer.Serialize(evt), cancellationToken);

        return AppResult<ResetPasswordResponse>.Success(new ResetPasswordResponse());
    }

    public async Task<AppResult<ChangePasswordResponse>> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) return AppResult<ChangePasswordResponse>.Failure(401, "Unauthorized", "Authentication required.");
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return AppResult<ChangePasswordResponse>.Failure(400, "Invalid Request", "Both current and new password must be provided.");
        }

        var uRes = await _repository.GetUserByIdAsync(userId, cancellationToken);
        if (uRes == null || uRes.Value == null || string.IsNullOrEmpty(uRes.Value.Id))
        {
            return AppResult<ChangePasswordResponse>.Failure(404, "Not Found", "User account not found.");
        }

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, uRes.Value.PasswordHash ?? string.Empty))
        {
            return AppResult<ChangePasswordResponse>.Failure(400, "Invalid Password", "Current password provided is incorrect.");
        }

        string newHash = _passwordHasher.HashPassword(request.NewPassword);
        var evt = new UserPasswordChangedV1Event { UserId = userId, Reason = "AuthenticatedChange", ChangedAtUtc = DateTime.UtcNow };
        await _repository.ChangePasswordAsync(userId, uRes.Value.PasswordHash!, newHash, _passwordHasher.AlgorithmName, JsonSerializer.Serialize(evt), cancellationToken);

        return AppResult<ChangePasswordResponse>.Success(new ChangePasswordResponse());
    }

    public async Task<AppResult<List<SessionDto>>> GetSessionsAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) return AppResult<List<SessionDto>>.Failure(401, "Unauthorized", "Authentication required.");

        var res = await _repository.GetSessionsAsync(userId, cancellationToken);
        var dtos = new List<SessionDto>();
        if (res != null && res.Value != null)
        {
            foreach (var s in res.Value)
            {
                dtos.Add(new SessionDto(s.Id, s.Status.ToString(), s.CreatedAtUtc, s.RevokedAtUtc, false, s.DeviceLabel, s.IpAddress));
            }
        }
        return AppResult<List<SessionDto>>.Success(dtos);
    }

    public async Task<AppResult<StandardSuccessResponse>> RevokeSessionAsync(string userId, string sessionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) return AppResult<StandardSuccessResponse>.Failure(401, "Unauthorized", "Authentication required.");

        var evt = new UserSessionRevokedV1Event { UserId = userId, SessionId = sessionId, RevocationReason = "UserRevoked", RevokedAtUtc = DateTime.UtcNow };
        await _repository.RevokeSessionAsync(sessionId, userId, JsonSerializer.Serialize(evt), cancellationToken);

        return AppResult<StandardSuccessResponse>.Success(new StandardSuccessResponse("Session revoked successfully."));
    }

    public async Task<AppResult<AccountStatusResponse>> GetAccountStatusAsync(string userId, CancellationToken cancellationToken)
    {
        var uRes = await _repository.GetUserByIdAsync(userId, cancellationToken);
        if (uRes == null || uRes.Value == null || string.IsNullOrEmpty(uRes.Value.Id))
        {
            return AppResult<AccountStatusResponse>.Failure(404, "Not Found", "User account not found.");
        }

        var u = uRes.Value;
        return AppResult<AccountStatusResponse>.Success(new AccountStatusResponse(
            u.Id, u.Email, u.EmailVerified, u.Mobile ?? string.Empty, u.MobileVerified, u.Status, u.FailedCount, u.LockoutEndUtc));
    }

    public async Task<AppResult<CurrentIdentityResponse>> GetCurrentIdentityAsync(string userId, CancellationToken cancellationToken)
    {
        var uRes = await _repository.GetUserByIdAsync(userId, cancellationToken);
        if (uRes == null || uRes.Value == null || string.IsNullOrEmpty(uRes.Value.Id))
        {
            return AppResult<CurrentIdentityResponse>.Failure(404, "Not Found", "User identity not found.");
        }

        var u = uRes.Value;
        return AppResult<CurrentIdentityResponse>.Success(new CurrentIdentityResponse(
            u.Id, u.Email, u.EmailVerified, u.Mobile ?? string.Empty, u.MobileVerified, u.Status, DateTime.UtcNow.ToUniversalTime().ToString("O"), "pwd"));
    }

    public async Task<AppResult<LoginResponse>> VerifyMfaLoginAsync(MfaLoginVerifyRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.ChallengeToken))
        {
            return AppResult<LoginResponse>.Failure(400, "Invalid Request", "UserId and ChallengeToken are required.");
        }

        var uRes = await _repository.GetUserByIdAsync(request.UserId, cancellationToken);
        if (uRes == null || uRes.Value == null || string.IsNullOrEmpty(uRes.Value.Id))
        {
            return AppResult<LoginResponse>.Failure(401, "Unauthorized", "User not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.RecoveryCode))
        {
            var codes = await _repository.GetRecoveryCodesAsync(request.UserId, cancellationToken);
            var matching = codes?.Value?.Find(c => c.CodeHash == _tokenGenerator.HashToken(request.RecoveryCode.Trim()));
            if (matching == null)
            {
                return AppResult<LoginResponse>.Failure(401, "Unauthorized", "Invalid or consumed recovery code.");
            }
            await _repository.ConsumeRecoveryCodeAsync(matching.Id, null, cancellationToken);

            var challengeRes = await _repository.GetStepUpChallengeAsync(request.ChallengeToken, request.UserId, cancellationToken);
            if (challengeRes?.Value != null)
            {
                challengeRes.Value.Verify();
                await _repository.UpdateStepUpChallengeAsync(challengeRes.Value, cancellationToken);
            }
        }
        else
        {
            string codeHash = _tokenGenerator.HashToken(request.Code?.Trim() ?? string.Empty);
            var consumeRes = await _repository.ConsumeStepUpChallengeAsync(request.ChallengeToken, request.UserId, "MfaLogin", codeHash, 5, cancellationToken);
            if (consumeRes == null)
            {
                return AppResult<LoginResponse>.Failure(401, "Unauthorized", "Invalid MFA verification code.");
            }
        }

        string sessionId = Guid.NewGuid().ToString("N");
        string tokenFamilyId = Guid.NewGuid().ToString("N");
        var (refToken, refHash) = _tokenGenerator.GenerateRefreshToken();

        var session = new UserSession
        {
            Id = sessionId,
            UserId = uRes.Value.Id,
            TokenFamilyId = tokenFamilyId,
            Status = Domain.Enums.SessionStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        var refreshTokenObj = new RefreshToken
        {
            Id = Guid.NewGuid().ToString("N"),
            SessionId = sessionId,
            TokenHash = refHash,
            FamilyId = tokenFamilyId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            IsRevoked = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.CreateSessionAsync(session, refreshTokenObj, cancellationToken);

        string accessToken = _tokenGenerator.GenerateAccessToken(uRes.Value.Id, uRes.Value.Email, sessionId, uRes.Value.EmailVerified, "mfa");
        return AppResult<LoginResponse>.Success(new LoginResponse(accessToken, refToken, 900, "Bearer", false, null));
    }

    public async Task<AppResult<ResendMfaResponse>> ResendMfaAsync(string userId, ResendMfaRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) return AppResult<ResendMfaResponse>.Failure(401, "Unauthorized", "Authentication required.");
        if (string.IsNullOrWhiteSpace(request.ChallengeId)) return AppResult<ResendMfaResponse>.Failure(400, "Invalid Request", "ChallengeId is required.");

        var challengeRes = await _repository.GetStepUpChallengeAsync(request.ChallengeId, userId, cancellationToken);
        if (challengeRes == null || challengeRes.Value == null || challengeRes.Value.Status != "Issued")
        {
            return AppResult<ResendMfaResponse>.Failure(404, "Not Found", "Challenge not found or already processed.");
        }

        if (DateTime.UtcNow - challengeRes.Value.CreatedAtUtc < TimeSpan.FromSeconds(60))
        {
            return AppResult<ResendMfaResponse>.Failure(429, "Too Many Requests", "Please wait before requesting a new code.");
        }

        var uRes = await _repository.GetUserByIdAsync(userId, cancellationToken);
        if (uRes == null || uRes.Value == null) return AppResult<ResendMfaResponse>.Failure(404, "Not Found", "User not found.");

        int recentSends = await _repository.GetRecentStepUpChallengesCountAsync(userId, challengeRes.Value.TargetAction, TimeSpan.FromMinutes(15), cancellationToken);
        if (recentSends >= 5)
        {
            return AppResult<ResendMfaResponse>.Failure(429, "Too Many Requests", "Maximum OTP send limit reached for this purpose. Try again later.");
        }

        challengeRes.Value.Status = "Cancelled";
        await _repository.UpdateStepUpChallengeAsync(challengeRes.Value, cancellationToken);

        var (otp, otpHash) = _tokenGenerator.GenerateVerificationToken();
        var newChallenge = new StepUpChallenge
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            TokenHash = otpHash,
            TargetAction = challengeRes.Value.TargetAction,
            Status = "Issued",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            CreatedAtUtc = DateTime.UtcNow
        };
        await _repository.CreateStepUpChallengeAsync(newChallenge, cancellationToken);

        if (_deliveryService != null)
        {
            await _deliveryService.SendVerificationOtpAsync(uRes.Value.Email, MfaMethodTypes.EmailOtp, otp, cancellationToken);
        }

        return AppResult<ResendMfaResponse>.Success(new ResendMfaResponse("Verification code resent successfully.", newChallenge.Id));
    }

    public async Task<AppResult<RegisterMfaResponse>> RegisterMfaAsync(string userId, RegisterMfaRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) return AppResult<RegisterMfaResponse>.Failure(401, "Unauthorized", "Authentication required.");

        var uRes = await _repository.GetUserByIdAsync(userId, cancellationToken);
        if (uRes == null || uRes.Value == null) return AppResult<RegisterMfaResponse>.Failure(404, "Not Found", "User not found.");

        if (!uRes.Value.EmailVerified) return AppResult<RegisterMfaResponse>.Failure(400, "Validation Error", "Email must be verified before enrolling in Email OTP MFA.");

        int recentSends = await _repository.GetRecentStepUpChallengesCountAsync(userId, "MfaEnrollment", TimeSpan.FromMinutes(15), cancellationToken);
        if (recentSends >= 5)
        {
            return AppResult<RegisterMfaResponse>.Failure(429, "Too Many Requests", "Maximum OTP send limit reached for enrollment. Try again later.");
        }

        string secret = string.Empty;
        string qrUri = string.Empty;

        var mfaMethod = new MfaMethod
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            Type = MfaMethodTypes.EmailOtp,
            EncryptedSecret = "NO_SECRET_FOR_EMAIL_OTP",
            IsEnabled = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        await _repository.SaveMfaMethodAsync(mfaMethod, null, cancellationToken);

        var recoveryCodes = new List<string>();

        var (otp, otpHash) = _tokenGenerator.GenerateVerificationToken();
        var challenge = new StepUpChallenge
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            TokenHash = otpHash,
            TargetAction = "MfaEnrollment",
            Status = "Issued",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            CreatedAtUtc = DateTime.UtcNow
        };
        await _repository.CreateStepUpChallengeAsync(challenge, cancellationToken);

        if (_deliveryService != null)
        {
            await _deliveryService.SendVerificationOtpAsync(uRes.Value.Email, MfaMethodTypes.EmailOtp, otp, cancellationToken);
        }

        return AppResult<RegisterMfaResponse>.Success(new RegisterMfaResponse(secret, qrUri, recoveryCodes, "MFA factor registered. Please confirm with OTP to enable.", challenge.Id));
    }

    public async Task<AppResult<StandardSuccessResponse>> ConfirmMfaAsync(string userId, ConfirmMfaRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) return AppResult<StandardSuccessResponse>.Failure(401, "Unauthorized", "Authentication required.");
        if (string.IsNullOrWhiteSpace(request.ChallengeId)) return AppResult<StandardSuccessResponse>.Failure(400, "Invalid Request", "ChallengeId is required.");

        var mfaRes = await _repository.GetMfaMethodAsync(userId, request.Type, cancellationToken);
        if (mfaRes == null || mfaRes.Value == null)
        {
            return AppResult<StandardSuccessResponse>.Failure(404, "Not Found", "MFA registration not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length != 6)
        {
            return AppResult<StandardSuccessResponse>.Failure(400, "Invalid Code", "Please provide a valid 6-digit verification code.");
        }

        string codeHash = _tokenGenerator.HashToken(request.Code.Trim());
        var consumeRes = await _repository.ConsumeStepUpChallengeAsync(request.ChallengeId, userId, "MfaEnrollment", codeHash, 5, cancellationToken);
        if (consumeRes == null)
        {
            return AppResult<StandardSuccessResponse>.Failure(400, "Invalid Code", "The code is incorrect, expired, or belongs to a different purpose.");
        }

        mfaRes.Value.Enable();
        await _repository.SaveMfaMethodAsync(mfaRes.Value, null, cancellationToken);

        return AppResult<StandardSuccessResponse>.Success(new StandardSuccessResponse("MFA factor successfully confirmed and enabled."));
    }

    public async Task<AppResult<InitiateStepUpResponse>> InitiateStepUpAsync(string userId, InitiateStepUpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) return AppResult<InitiateStepUpResponse>.Failure(401, "Unauthorized", "Authentication required.");

        var (token, hash) = _tokenGenerator.GenerateVerificationToken();
        var challenge = new StepUpChallenge
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            TokenHash = hash,
            TargetAction = request.TargetAction,
            Status = "Issued",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.CreateStepUpChallengeAsync(challenge, cancellationToken);
        var uRes = await _repository.GetUserByIdAsync(userId, cancellationToken);
        if (uRes?.Value != null && _deliveryService != null)
        {
            await _deliveryService.SendVerificationOtpAsync(uRes.Value.Email, "StepUp", token, cancellationToken);
        }

        return AppResult<InitiateStepUpResponse>.Success(new InitiateStepUpResponse(challenge.Id, token));
    }

    public async Task<AppResult<VerifyStepUpResponse>> VerifyStepUpAsync(string userId, VerifyStepUpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) return AppResult<VerifyStepUpResponse>.Failure(401, "Unauthorized", "Authentication required.");

        var challengeRes = await _repository.GetStepUpChallengeAsync(request.StepUpId, userId, cancellationToken);
        if (challengeRes?.Value == null || challengeRes.Value.Status != "Issued" || challengeRes.Value.ExpiresAtUtc < DateTime.UtcNow)
        {
            return AppResult<VerifyStepUpResponse>.Failure(401, "Unauthorized", "Step-up challenge invalid or expired.");
        }

        string codeHash = _tokenGenerator.HashToken(request.Code?.Trim() ?? string.Empty);
        if (codeHash != challengeRes.Value.TokenHash)
        {
            return AppResult<VerifyStepUpResponse>.Failure(401, "Unauthorized", "Invalid step-up verification code.");
        }

        challengeRes.Value.Verify();
        await _repository.UpdateStepUpChallengeAsync(challengeRes.Value, cancellationToken);

        string verificationToken = $"STEPUP_OK_{challengeRes.Value.TargetAction}_{Guid.NewGuid():N}";
        return AppResult<VerifyStepUpResponse>.Success(new VerifyStepUpResponse(verificationToken));
    }

    public async Task<AppResult<RegisterServiceClientResponse>> RegisterServiceClientAsync(RegisterServiceClientRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            return AppResult<RegisterServiceClientResponse>.Failure(400, "Invalid Request", "ClientId is required.");
        }

        string id = Guid.NewGuid().ToString("N");
        string keyId = $"key_{Guid.NewGuid():N}"[..16];
        string rawSecret = $"emsec_{Guid.NewGuid():N}{Guid.NewGuid():N}";
        string secretHash = _tokenGenerator.HashToken(rawSecret);
        var expiresAtUtc = DateTime.UtcNow.AddDays(request.ExpiryDays > 0 ? request.ExpiryDays : 365);

        var client = new ServiceClient
        {
            Id = id,
            ClientId = request.ClientId,
            ClientSecretHash = secretHash,
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        };

        var cred = new ServiceClientCredential
        {
            Id = Guid.NewGuid().ToString("N"),
            ServiceClientId = id,
            KeyId = keyId,
            SecretHash = secretHash,
            ExpiresAtUtc = expiresAtUtc,
            IsRevoked = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        var scopes = new List<ServiceClientScope>();
        foreach (var s in request.Scopes ?? new List<string>())
        {
            scopes.Add(new ServiceClientScope { Id = Guid.NewGuid().ToString("N"), ServiceClientId = id, Scope = s, CreatedAtUtc = DateTime.UtcNow });
        }
        if (scopes.Count == 0)
        {
            scopes.Add(new ServiceClientScope { Id = Guid.NewGuid().ToString("N"), ServiceClientId = id, Scope = "service.default", CreatedAtUtc = DateTime.UtcNow });
        }

        await _repository.CreateServiceClientAsync(client, cred, scopes, null, cancellationToken);

        return AppResult<RegisterServiceClientResponse>.Success(
            new RegisterServiceClientResponse(id, request.ClientId, rawSecret, keyId, expiresAtUtc, scopes.Select(x => x.Scope).ToList()), 201);
    }

    public async Task<AppResult<RotateServiceClientCredentialResponse>> RotateServiceClientCredentialAsync(RotateServiceClientCredentialRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceClientId))
        {
            return AppResult<RotateServiceClientCredentialResponse>.Failure(400, "Invalid Request", "ServiceClientId is required.");
        }

        string keyId = $"key_{Guid.NewGuid():N}"[..16];
        string rawSecret = $"emsec_{Guid.NewGuid():N}{Guid.NewGuid():N}";
        string secretHash = _tokenGenerator.HashToken(rawSecret);
        var expiresAtUtc = DateTime.UtcNow.AddDays(request.ExpiryDays > 0 ? request.ExpiryDays : 365);

        var newCred = new ServiceClientCredential
        {
            Id = Guid.NewGuid().ToString("N"),
            ServiceClientId = request.ServiceClientId,
            KeyId = keyId,
            SecretHash = secretHash,
            ExpiresAtUtc = expiresAtUtc,
            IsRevoked = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _repository.RotateServiceClientCredentialAsync(request.ServiceClientId, newCred, null, cancellationToken);

        return AppResult<RotateServiceClientCredentialResponse>.Success(
            new RotateServiceClientCredentialResponse(request.ServiceClientId, rawSecret, keyId, expiresAtUtc));
    }

    public async Task<AppResult<StandardSuccessResponse>> RevokeServiceClientCredentialAsync(RevokeServiceClientCredentialRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CredentialId))
        {
            return AppResult<StandardSuccessResponse>.Failure(400, "Invalid Request", "CredentialId is required.");
        }

        await _repository.RevokeServiceClientCredentialAsync(request.CredentialId, null, cancellationToken);
        return AppResult<StandardSuccessResponse>.Success(new StandardSuccessResponse("Credential revoked successfully."));
    }

    public async Task<AppResult<List<ServiceClientCredentialDto>>> ListServiceClientCredentialsAsync(string serviceClientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serviceClientId))
        {
            return AppResult<List<ServiceClientCredentialDto>>.Failure(400, "Invalid Request", "ServiceClientId is required.");
        }

        var res = await _repository.ListServiceClientCredentialsAsync(serviceClientId, cancellationToken);
        var list = new List<ServiceClientCredentialDto>();
        if (res?.Value != null)
        {
            foreach (var c in res.Value)
            {
                list.Add(new ServiceClientCredentialDto(c.Id, c.ServiceClientId, c.KeyId, c.ExpiresAtUtc, c.IsRevoked, c.CreatedAtUtc));
            }
        }
        return AppResult<List<ServiceClientCredentialDto>>.Success(list);
    }

    public async Task<AppResult<ServiceTokenResponse>> IssueServiceTokenAsync(ServiceTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            return AppResult<ServiceTokenResponse>.Failure(401, "Unauthorized", "ClientId and ClientSecret are required.");
        }

        string secretHash = _tokenGenerator.HashToken(request.ClientSecret.Trim());
        var credRes = await _repository.GetServiceClientCredentialAsync(secretHash, cancellationToken);
        if (credRes?.Value == null)
        {
            return AppResult<ServiceTokenResponse>.Failure(401, "Unauthorized", "Invalid service client credentials.");
        }

        var cred = credRes.Value;
        var scopesRes = await _repository.GetServiceClientScopesAsync(cred.ServiceClientId, cancellationToken);
        var scopes = scopesRes?.Value ?? new List<string>();

        if (!string.IsNullOrWhiteSpace(request.Scope) && !scopes.Contains(request.Scope))
        {
            return AppResult<ServiceTokenResponse>.Failure(403, "Forbidden", $"Requested scope '{request.Scope}' is not granted to this service client.");
        }

        string accessToken = _tokenGenerator.GenerateAccessToken(cred.ServiceClientId, request.ClientId, cred.KeyId, true, "s2s");
        return AppResult<ServiceTokenResponse>.Success(new ServiceTokenResponse(accessToken, 3600, "Bearer"));
    }

    public async Task<AppResult<StandardSuccessResponse>> AdminUpdateUserStatusAsync(AdminUpdateUserStatusRequest request, string actor, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Status))
        {
            return AppResult<StandardSuccessResponse>.Failure(400, "Invalid Request", "UserId and Status are required.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return AppResult<StandardSuccessResponse>.Failure(400, "Validation Error", "A valid status reason must be provided for administrative status modifications.");
        }

        if (!Enum.TryParse<Domain.Enums.AccountStatus>(request.Status, true, out var parsedStatus))
        {
            return AppResult<StandardSuccessResponse>.Failure(400, "Invalid Status", "Status must be a valid AccountStatus (e.g., Active, Locked, Suspended, Closed).");
        }

        var uRes = await _repository.GetUserByIdAsync(request.UserId, cancellationToken);
        if (uRes?.Value == null || string.IsNullOrEmpty(uRes.Value.Id))
        {
            return AppResult<StandardSuccessResponse>.Failure(404, "Not Found", "User account not found.");
        }

        await _repository.UpdateUserStatusAsync(request.UserId, parsedStatus, request.Reason, actor, null, cancellationToken);
        return AppResult<StandardSuccessResponse>.Success(new StandardSuccessResponse($"User status updated to {parsedStatus}."));
    }
}
