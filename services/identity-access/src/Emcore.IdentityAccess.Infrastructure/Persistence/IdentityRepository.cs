using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application.Abstractions;
using Emcore.IdentityAccess.Domain.Entities;
using Emcore.IdentityAccess.Domain.Enums;
using Emcore.IdentityAccess.Domain.ValueObjects;
using Emcore.BuildingBlocks.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Dapper;

namespace Emcore.IdentityAccess.Infrastructure.Persistence;

public class IdentityRepository : IIdentityRepository
{
    private readonly string? _connectionString;
    private readonly bool _useInMemoryFallback;
    private static readonly Dictionary<string, UserAccount> InMemoryUsers = new();
    private static readonly Dictionary<string, string> InMemoryCredentials = new();
    private static readonly Dictionary<string, UserLookupResult> InMemoryLookups = new();
    private static readonly Dictionary<string, List<UserSession>> InMemorySessions = new();
    private static readonly Dictionary<string, (string UserId, string SessionId, RefreshToken Token)> InMemoryRefreshTokens = new();
    private static readonly Dictionary<string, MfaMethod> InMemoryMfaMethods = new();
    private static readonly List<MfaRecoveryCode> InMemoryRecoveryCodes = new();
    private static readonly Dictionary<string, StepUpChallenge> InMemoryStepUpChallenges = new();
    private static readonly Dictionary<string, StepUpProof> InMemoryStepUpProofs = new();
    private static readonly Dictionary<string, (ServiceClient Client, List<ServiceClientCredential> Credentials, List<ServiceClientScope> Scopes)> InMemoryServiceClients = new();
    private static readonly List<SecurityEvent> InMemorySecurityEvents = new();
    private static readonly Dictionary<string, AccountVerification> InMemoryVerifications = new();
    private static readonly Dictionary<string, PasswordRecovery> InMemoryRecoveries = new();

    public IdentityRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("IdentityDatabase") ?? configuration["ConnectionStrings__IdentityDatabase"];
        _useInMemoryFallback = string.IsNullOrWhiteSpace(_connectionString) || _connectionString.Contains("dummy") || _connectionString.Contains("inmemory");
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public async Task<Result<UserAccount>> RegisterUserAsync(
        string email,
        string mobile,
        string passwordHash,
        string hashAlgorithm,
        AccountVerification verification,
        string? outboxPayload,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = new NormalizedEmail(email.Trim().ToLowerInvariant());
        var normalizedMobile = new NormalizedMobile(mobile.Trim());
        var emailObj = new UserEmail(email, normalizedEmail, false);
        var mobileObj = new UserMobile(mobile, normalizedMobile, false);
        var userAccount = new UserAccount(emailObj, mobileObj);

        if (_useInMemoryFallback)
        {
            lock (InMemoryUsers)
            {
                if (InMemoryUsers.Values.Any(u => u.Email.Normalized.Value == normalizedEmail.Value || u.Mobile.Normalized.Value == normalizedMobile.Value))
                {
                    return new Result<UserAccount>(); // Duplicate simulation
                }
                InMemoryUsers[userAccount.Id] = userAccount;
                InMemoryCredentials[userAccount.Id] = passwordHash;
                InMemoryLookups[normalizedEmail.Value] = new UserLookupResult(userAccount.Id, userAccount.UlidId, "PendingVerification", email, normalizedEmail.Value, false, mobile, normalizedMobile.Value, false, passwordHash, hashAlgorithm, 0, null);
                if (!string.IsNullOrEmpty(normalizedMobile.Value))
                {
                    InMemoryLookups[normalizedMobile.Value] = InMemoryLookups[normalizedEmail.Value];
                }
            }
            return userAccount;
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        var parameters = new DynamicParameters();
        parameters.Add("Id", Guid.Parse(userAccount.Id));
        parameters.Add("UlidId", userAccount.UlidId);
        parameters.Add("EmailAddress", email);
        parameters.Add("NormalizedEmail", normalizedEmail.Value);
        parameters.Add("MobileNumber", mobile);
        parameters.Add("NormalizedMobile", normalizedMobile.Value);
        parameters.Add("PasswordHash", passwordHash);
        parameters.Add("HashAlgorithm", hashAlgorithm);
        parameters.Add("VerificationId", Guid.Parse(verification.Id));
        parameters.Add("VerificationTokenHash", verification.TokenHash);
        parameters.Add("VerificationChannel", verification.Channel);
        parameters.Add("VerificationExpiresAtUtc", verification.ExpiresAtUtc);
        parameters.Add("OutboxId", Guid.NewGuid());
        parameters.Add("OutboxMessageType", "identity.user.registered.v1");
        parameters.Add("OutboxPayload", outboxPayload ?? "{}");
        parameters.Add("ReturnValue", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);

        await connection.ExecuteAsync(
            "dbo.PR_IDENTITY_REGISTER_USER",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        int returnCode = parameters.Get<int>("ReturnValue");

        if (returnCode == -1 || returnCode == -2)
        {
            return new Result<UserAccount>(); // Duplicate identifier conflict
        }

        return userAccount;
    }

    public async Task<Result<UserLookupResult>> GetUserByIdentifierAsync(string emailOrMobile, CancellationToken cancellationToken)
    {
        var normalized = emailOrMobile.Trim().ToLowerInvariant();
        if (_useInMemoryFallback)
        {
            lock (InMemoryLookups)
            {
                if (InMemoryLookups.TryGetValue(normalized, out var result))
                {
                    return result;
                }
                if (InMemoryLookups.Values.FirstOrDefault(v => v.Id == emailOrMobile) is var byId && byId != null)
                {
                    return byId;
                }
            }
            return new Result<UserLookupResult>();
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        var lookup = await connection.QuerySingleOrDefaultAsync<UserLookupResult>(
            "dbo.PR_IDENTITY_GET_USER_BY_EMAIL_OR_MOBILE",
            new { Identifier = normalized },
            commandType: System.Data.CommandType.StoredProcedure);

        return lookup != null ? lookup : new Result<UserLookupResult>();
    }

    public async Task<Result<UserLookupResult>> GetUserByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryLookups)
            {
                var lookup = InMemoryLookups.Values.FirstOrDefault(u => u.Id == userId);
                return lookup != null ? lookup : new Result<UserLookupResult>();
            }
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        if (!Guid.TryParse(userId, out var guidId)) return new Result<UserLookupResult>();

        var res = await connection.QuerySingleOrDefaultAsync<UserLookupResult>(
            "dbo.PR_IDENTITY_GET_USER_BY_ID",
            new { UserId = guidId },
            commandType: System.Data.CommandType.StoredProcedure);

        return res != null ? res : new Result<UserLookupResult>();
    }

    public async Task<Result> CreateVerificationAsync(AccountVerification verification, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryVerifications)
            {
                foreach (var v in InMemoryVerifications.Values.Where(x => x.UserId == verification.UserId && x.Channel == verification.Channel && x.Status == VerificationStatus.Issued))
                {
                    v.Status = VerificationStatus.Cancelled;
                }
                InMemoryVerifications[verification.Id] = verification;
            }
            return new Result();
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            "dbo.PR_IDENTITY_CREATE_VERIFICATION",
            new
            {
                Id = Guid.Parse(verification.Id),
                UserId = Guid.Parse(verification.UserId),
                TokenHash = verification.TokenHash,
                Channel = verification.Channel,
                ExpiresAtUtc = verification.ExpiresAtUtc
            },
            commandType: System.Data.CommandType.StoredProcedure);

        return new Result();
    }

    public async Task<Result> VerifyAccountAsync(string userId, string channel, string tokenHash, string? outboxPayload, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryVerifications)
            {
                var v = InMemoryVerifications.Values
                    .Where(x => x.UserId == userId && x.Channel == channel && x.Status == VerificationStatus.Issued)
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .FirstOrDefault();

                if (v == null || v.ExpiresAtUtc < DateTime.UtcNow || v.TokenHash != tokenHash || v.AttemptCount >= 5)
                {
                    if (v != null) v.AttemptCount++;
                    return null!; // fail
                }
                v.Status = VerificationStatus.Verified;
            }

            lock (InMemoryLookups)
            {
                var lookup = InMemoryLookups.Values.FirstOrDefault(l => l.Id == userId);
                if (lookup != null)
                {
                    var updated = lookup with { Status = "Active", EmailVerified = (channel == "Email") || lookup.EmailVerified, MobileVerified = (channel == "Mobile") || lookup.MobileVerified };
                    if (!string.IsNullOrEmpty(lookup.NormalizedEmail)) InMemoryLookups[lookup.NormalizedEmail] = updated;
                    if (!string.IsNullOrEmpty(lookup.NormalizedMobile)) InMemoryLookups[lookup.NormalizedMobile] = updated;
                }
            }
            return new Result();
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        var parameters = new DynamicParameters();
        parameters.Add("UserId", Guid.Parse(userId));
        parameters.Add("Channel", channel);
        parameters.Add("TokenHash", tokenHash);
        parameters.Add("OutboxId", outboxPayload != null ? (Guid?)Guid.NewGuid() : null);
        parameters.Add("OutboxMessageType", channel == "Email" ? "identity.user.email-verified.v1" : "identity.user.mobile-verified.v1");
        parameters.Add("OutboxPayload", outboxPayload);
        parameters.Add("ReturnValue", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);

        await connection.ExecuteAsync(
            "dbo.PR_IDENTITY_VERIFY_ACCOUNT",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        int returnCode = parameters.Get<int>("ReturnValue");
        if (returnCode == -1) return null!;

        return new Result();
    }

    public async Task<Result> RecordLoginAttemptAsync(string userId, bool isSuccess, int lockoutMinutes, int maxFailures, string? outboxPayload, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback) return new Result();

        using var connection = await OpenConnectionAsync(cancellationToken);
        if (!Guid.TryParse(userId, out var guidId)) return new Result();

        await connection.ExecuteAsync(
            "dbo.PR_IDENTITY_RECORD_LOGIN_ATTEMPT",
            new
            {
                UserId = guidId,
                IsSuccess = isSuccess,
                LockoutMinutes = lockoutMinutes,
                MaxFailures = maxFailures,
                OutboxId = outboxPayload != null ? (Guid?)Guid.NewGuid() : null,
                OutboxMessageType = "identity.user.locked.v1",
                OutboxPayload = outboxPayload
            },
            commandType: System.Data.CommandType.StoredProcedure);

        return new Result();
    }

    public async Task<Result> CreateSessionAsync(UserSession session, RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemorySessions)
            {
                if (!InMemorySessions.ContainsKey(session.UserId)) InMemorySessions[session.UserId] = new();
                InMemorySessions[session.UserId].Add(session);
                InMemoryRefreshTokens[refreshToken.TokenHash] = (session.UserId, session.Id, refreshToken);
            }
            return new Result();
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            "dbo.PR_IDENTITY_CREATE_SESSION",
            new
            {
                SessionId = Guid.Parse(session.Id),
                UserId = Guid.Parse(session.UserId),
                TokenFamilyId = session.TokenFamilyId,
                DeviceLabel = session.DeviceLabel ?? "Web Client",
                IpAddress = session.IpAddress ?? "127.0.0.1",
                RefreshTokenId = Guid.Parse(refreshToken.Id),
                RefreshTokenHash = refreshToken.TokenHash,
                ExpiresAtUtc = refreshToken.ExpiresAtUtc
            },
            commandType: System.Data.CommandType.StoredProcedure);

        return new Result();
    }

    public async Task<Result<(string UserId, string SessionId)>> RotateRefreshTokenAsync(string oldTokenHash, RefreshToken newRefreshToken, string? outboxPayload, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemorySessions)
            {
                if (InMemoryRefreshTokens.TryGetValue(oldTokenHash, out var info) && !info.Token.IsRevoked)
                {
                    info.Token.IsRevoked = true;
                    InMemoryRefreshTokens[newRefreshToken.TokenHash] = (info.UserId, info.SessionId, newRefreshToken);
                    return (info.UserId, info.SessionId);
                }
                return new Result<(string UserId, string SessionId)>();
            }
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync(
            "dbo.PR_IDENTITY_ROTATE_REFRESH_TOKEN",
            new
            {
                OldTokenHash = oldTokenHash,
                NewTokenId = Guid.Parse(newRefreshToken.Id),
                NewTokenHash = newRefreshToken.TokenHash,
                NewExpiresAtUtc = newRefreshToken.ExpiresAtUtc,
                OutboxId = outboxPayload != null ? (Guid?)Guid.NewGuid() : null,
                OutboxMessageType = "identity.user.session-revoked.v1",
                OutboxPayload = outboxPayload
            },
            commandType: System.Data.CommandType.StoredProcedure);

        if (row is System.Collections.Generic.IDictionary<string, object> dict && dict.TryGetValue("UserId", out var uid) && dict.TryGetValue("SessionId", out var sid) && uid != null && sid != null)
        {
            return (uid.ToString()!, sid.ToString()!);
        }

        return new Result<(string UserId, string SessionId)>();
    }

    public async Task<Result> RevokeSessionAsync(string sessionId, string userId, string? outboxPayload, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemorySessions)
            {
                if (InMemorySessions.TryGetValue(userId, out var list))
                {
                    var s = list.FirstOrDefault(x => x.Id == sessionId);
                    if (s != null) s.Status = Emcore.IdentityAccess.Domain.Enums.SessionStatus.Revoked;
                }
            }
            return new Result();
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        await connection.ExecuteScalarAsync<int>(
            "dbo.PR_IDENTITY_REVOKE_SESSION",
            new
            {
                SessionId = Guid.Parse(sessionId),
                UserId = Guid.Parse(userId),
                OutboxId = outboxPayload != null ? (Guid?)Guid.NewGuid() : null,
                OutboxMessageType = "identity.user.session-revoked.v1",
                OutboxPayload = outboxPayload
            },
            commandType: System.Data.CommandType.StoredProcedure);

        return new Result();
    }

    public async Task<Result> RevokeAllSessionsAsync(string userId, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemorySessions)
            {
                if (InMemorySessions.TryGetValue(userId, out var list))
                {
                    foreach (var s in list) s.Status = Emcore.IdentityAccess.Domain.Enums.SessionStatus.Revoked;
                }
            }
            return new Result();
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        if (!Guid.TryParse(userId, out var guidId)) return new Result();

        await connection.ExecuteAsync(
            "dbo.PR_IDENTITY_REVOKE_ALL_SESSIONS",
            new { UserId = guidId },
            commandType: System.Data.CommandType.StoredProcedure);

        return new Result();
    }

    public async Task<Result<List<UserSession>>> GetSessionsAsync(string userId, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemorySessions)
            {
                if (InMemorySessions.TryGetValue(userId, out var list)) return list;
                return new List<UserSession> { new UserSession { Id = "sess_1", UserId = userId, Status = Emcore.IdentityAccess.Domain.Enums.SessionStatus.Active, CreatedAtUtc = DateTime.UtcNow } };
            }
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        if (!Guid.TryParse(userId, out var guidId)) return new List<UserSession>();

        var sessions = await connection.QueryAsync<UserSession>(
            "dbo.PR_IDENTITY_LIST_SESSIONS",
            new { UserId = guidId },
            commandType: System.Data.CommandType.StoredProcedure);

        return sessions.ToList();
    }

    public async Task<Result> CreateRecoveryRequestAsync(PasswordRecovery recovery, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryRecoveries)
            {
                foreach (var r in InMemoryRecoveries.Values.Where(x => x.UserId == recovery.UserId && x.Status == RecoveryStatus.Created))
                {
                    r.Status = RecoveryStatus.Cancelled;
                }
                InMemoryRecoveries[recovery.Id] = recovery;
            }
            return new Result();
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            "dbo.PR_IDENTITY_CREATE_RECOVERY_REQUEST",
            new
            {
                Id = Guid.Parse(recovery.Id),
                UserId = Guid.Parse(recovery.UserId),
                TokenHash = recovery.TokenHash,
                ExpiresAtUtc = recovery.ExpiresAtUtc
            },
            commandType: System.Data.CommandType.StoredProcedure);

        return new Result();
    }

    public async Task<Result> ResetPasswordAsync(string userId, string tokenHash, string newPasswordHash, string hashAlgorithm, string? outboxPayload, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryRecoveries)
            {
                var r = InMemoryRecoveries.Values
                    .Where(x => x.TokenHash == tokenHash && x.Status == RecoveryStatus.Created && x.ExpiresAtUtc > DateTime.UtcNow)
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .FirstOrDefault();

                if (r == null || (userId != Guid.Empty.ToString() && r.UserId != userId))
                {
                    return null!; // fail
                }
                userId = r.UserId; // Resolve token-only user
                r.Status = RecoveryStatus.Completed;
            }

            lock (InMemoryLookups)
            {
                InMemoryCredentials[userId] = newPasswordHash;
                var keys = InMemoryLookups.Where(x => x.Value.Id == userId).Select(x => x.Key).ToList();
                foreach (var k in keys)
                {
                    var old = InMemoryLookups[k];
                    InMemoryLookups[k] = new UserLookupResult(old.Id, old.UlidId, old.Status, old.Email, old.NormalizedEmail, old.EmailVerified, old.Mobile, old.NormalizedMobile, old.MobileVerified, newPasswordHash, hashAlgorithm, old.FailedCount, old.LockoutEndUtc);
                }
            }
            return new Result();
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId == Guid.Empty.ToString() ? (Guid?)null : Guid.Parse(userId));
        parameters.Add("TokenHash", tokenHash);
        parameters.Add("NewPasswordHash", newPasswordHash);
        parameters.Add("HashAlgorithm", hashAlgorithm);
        parameters.Add("OutboxId", outboxPayload != null ? (Guid?)Guid.NewGuid() : null);
        parameters.Add("OutboxMessageType", "identity.user.password-changed.v1");
        parameters.Add("OutboxPayload", outboxPayload);
        parameters.Add("ReturnValue", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.ReturnValue);

        await connection.ExecuteAsync(
            "dbo.PR_IDENTITY_RESET_PASSWORD",
            parameters,
            commandType: System.Data.CommandType.StoredProcedure);

        int returnCode = parameters.Get<int>("ReturnValue");
        if (returnCode == -1) return null!;

        return new Result();
    }

    public async Task<Result> ChangePasswordAsync(string userId, string oldPasswordHash, string newPasswordHash, string hashAlgorithm, string? outboxPayload, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryLookups)
            {
                InMemoryCredentials[userId] = newPasswordHash;
                var keys = InMemoryLookups.Where(x => x.Value.Id == userId).Select(x => x.Key).ToList();
                foreach (var k in keys)
                {
                    var old = InMemoryLookups[k];
                    InMemoryLookups[k] = new UserLookupResult(old.Id, old.UlidId, old.Status, old.Email, old.NormalizedEmail, old.EmailVerified, old.Mobile, old.NormalizedMobile, old.MobileVerified, newPasswordHash, hashAlgorithm, old.FailedCount, old.LockoutEndUtc);
                }
            }
            return new Result();
        }

        using var connection = await OpenConnectionAsync(cancellationToken);
        await connection.ExecuteScalarAsync<int>(
            "dbo.PR_IDENTITY_CHANGE_PASSWORD",
            new
            {
                UserId = Guid.Parse(userId),
                OldPasswordHash = oldPasswordHash,
                NewPasswordHash = newPasswordHash,
                HashAlgorithm = hashAlgorithm,
                OutboxId = outboxPayload != null ? (Guid?)Guid.NewGuid() : null,
                OutboxMessageType = "identity.user.password-changed.v1",
                OutboxPayload = outboxPayload
            },
            commandType: System.Data.CommandType.StoredProcedure);

        return new Result();
    }

    public async Task<Result<(bool IsCompleted, int StatusCode, string ResponseBody)>> BeginIdempotentRequestAsync(string idempotencyKey, string requestHash, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback) return (false, 0, string.Empty);

        using var connection = await OpenConnectionAsync(cancellationToken);
        var existing = await connection.QuerySingleOrDefaultAsync(
            "dbo.PR_IDENTITY_BEGIN_IDEMPOTENT_REQUEST",
            new { IdempotencyKey = idempotencyKey, Name = "ApiRequest", RequestHash = requestHash },
            commandType: System.Data.CommandType.StoredProcedure);

        if (existing is System.Collections.Generic.IDictionary<string, object> dict && dict.TryGetValue("StatusCode", out var sc) && sc != null && dict.TryGetValue("ResponseBody", out var rb))
        {
            return (true, Convert.ToInt32(sc), rb?.ToString() ?? string.Empty);
        }

        return (false, 0, string.Empty);
    }

    public async Task<Result> CompleteIdempotentRequestAsync(string idempotencyKey, int statusCode, string responseBody, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback) return new Result();

        using var connection = await OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            "dbo.PR_IDENTITY_COMPLETE_IDEMPOTENT_REQUEST",
            new { IdempotencyKey = idempotencyKey, StatusCode = statusCode, ResponseBody = responseBody },
            commandType: System.Data.CommandType.StoredProcedure);

        return new Result();
    }

    public async Task<Result> SaveMfaMethodAsync(MfaMethod mfaMethod, string? outboxPayload, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryMfaMethods) { InMemoryMfaMethods[$"{mfaMethod.UserId}_{mfaMethod.Type}"] = mfaMethod; }
            return new Result();
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync("dbo.PR_IDENTITY_SAVE_MFA_METHOD", new
        {
            Id = Guid.Parse(mfaMethod.Id),
            UserId = Guid.Parse(mfaMethod.UserId),
            Type = mfaMethod.Type,
            EncryptedSecret = mfaMethod.EncryptedSecret,
            IsEnabled = mfaMethod.IsEnabled,
            OutboxId = outboxPayload != null ? (Guid?)Guid.NewGuid() : null,
            OutboxMessageType = "identity.mfa.updated.v1",
            OutboxPayload = outboxPayload
        }, commandType: System.Data.CommandType.StoredProcedure);
        return new Result();
    }

    public async Task<Result<MfaMethod?>> GetMfaMethodAsync(string userId, string type, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryMfaMethods)
            {
                if (InMemoryMfaMethods.TryGetValue($"{userId}_{type}", out var m)) return m;
                return (MfaMethod?)null;
            }
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        var res = await conn.QuerySingleOrDefaultAsync<MfaMethod>("dbo.PR_IDENTITY_GET_MFA_METHOD", new
        {
            UserId = Guid.Parse(userId),
            Type = type
        }, commandType: System.Data.CommandType.StoredProcedure);
        return res;
    }

    public async Task<Result> DeleteMfaMethodAsync(string userId, string type, string? outboxPayload, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryMfaMethods) { InMemoryMfaMethods.Remove($"{userId}_{type}"); }
            return new Result();
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync("dbo.PR_IDENTITY_DELETE_MFA_METHOD", new
        {
            UserId = Guid.Parse(userId),
            Type = type,
            OutboxId = outboxPayload != null ? (Guid?)Guid.NewGuid() : null,
            OutboxMessageType = "identity.mfa.deleted.v1",
            OutboxPayload = outboxPayload
        }, commandType: System.Data.CommandType.StoredProcedure);
        return new Result();
    }

    public async Task<Result> SaveRecoveryCodesAsync(List<MfaRecoveryCode> codes, string? outboxPayload, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryRecoveryCodes) { InMemoryRecoveryCodes.AddRange(codes); }
            return new Result();
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        foreach (var c in codes)
        {
            await conn.ExecuteAsync("dbo.PR_IDENTITY_SAVE_RECOVERY_CODE", new
            {
                Id = Guid.Parse(c.Id),
                UserId = Guid.Parse(c.UserId),
                CodeHash = c.CodeHash
            }, commandType: System.Data.CommandType.StoredProcedure);
        }
        return new Result();
    }

    public async Task<Result<List<MfaRecoveryCode>>> GetRecoveryCodesAsync(string userId, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryRecoveryCodes)
            {
                return InMemoryRecoveryCodes.Where(x => x.UserId == userId && !x.IsConsumed).ToList();
            }
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        var list = await conn.QueryAsync<MfaRecoveryCode>("dbo.PR_IDENTITY_GET_RECOVERY_CODES", new
        {
            UserId = Guid.Parse(userId)
        }, commandType: System.Data.CommandType.StoredProcedure);
        return list.AsList();
    }

    public async Task<Result> ConsumeRecoveryCodeAsync(string id, string? outboxPayload, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryRecoveryCodes)
            {
                var code = InMemoryRecoveryCodes.FirstOrDefault(x => x.Id == id);
                if (code != null) code.Consume();
            }
            return new Result();
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync("dbo.PR_IDENTITY_CONSUME_RECOVERY_CODE", new
        {
            Id = Guid.Parse(id),
            OutboxId = outboxPayload != null ? (Guid?)Guid.NewGuid() : null,
            OutboxMessageType = "identity.mfa.recovery-code-consumed.v1",
            OutboxPayload = outboxPayload
        }, commandType: System.Data.CommandType.StoredProcedure);
        return new Result();
    }

    public async Task<Result> CreateStepUpChallengeAsync(StepUpChallenge challenge, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryStepUpChallenges) { InMemoryStepUpChallenges[challenge.Id] = challenge; }
            return new Result();
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync("dbo.PR_IDENTITY_CREATE_STEPUP_CHALLENGE", new
        {
            Id = Guid.Parse(challenge.Id),
            UserId = Guid.Parse(challenge.UserId),
            SessionId = string.IsNullOrWhiteSpace(challenge.SessionId) ? (Guid?)null : Guid.Parse(challenge.SessionId),
            TokenHash = challenge.TokenHash,
            TargetAction = challenge.TargetAction,
            ExpiresAtUtc = challenge.ExpiresAtUtc
        }, commandType: System.Data.CommandType.StoredProcedure);
        return new Result();
    }

    public async Task<Result<StepUpChallenge?>> GetStepUpChallengeAsync(string id, string userId, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryStepUpChallenges)
            {
                if (InMemoryStepUpChallenges.TryGetValue(id, out var c) && c.UserId == userId) return c;
                return (StepUpChallenge?)null;
            }
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<StepUpChallenge>("dbo.PR_IDENTITY_GET_STEPUP_CHALLENGE", new
        {
            Id = Guid.Parse(id),
            UserId = Guid.Parse(userId)
        }, commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task<Result> UpdateStepUpChallengeAsync(StepUpChallenge challenge, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryStepUpChallenges) { InMemoryStepUpChallenges[challenge.Id] = challenge; }
            return new Result();
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync("dbo.PR_IDENTITY_UPDATE_STEPUP_CHALLENGE", new
        {
            Id = Guid.Parse(challenge.Id),
            Status = challenge.Status
        }, commandType: System.Data.CommandType.StoredProcedure);
        return new Result();
    }

    public async Task<Result?> ConsumeStepUpChallengeAsync(string id, string userId, string? sessionId, string expectedPurpose, string tokenHash, int maxAttempts, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryStepUpChallenges)
            {
                if (InMemoryStepUpChallenges.TryGetValue(id, out var c) && c.UserId == userId)
                {
                    bool isSessionMatch = true;
                    if (c.TargetAction != "MfaLogin" && c.TargetAction != "MfaEnrollment")
                    {
                        var g1 = Guid.TryParse(c.SessionId, out var p1) ? p1.ToString("N") : c.SessionId ?? string.Empty;
                        var g2 = Guid.TryParse(sessionId, out var p2) ? p2.ToString("N") : sessionId ?? string.Empty;
                        isSessionMatch = string.Equals(g1, g2, StringComparison.OrdinalIgnoreCase);
                    }
                    if (!isSessionMatch) return null;
                    if (c.TargetAction != expectedPurpose) return null;
                    if (c.Status != "Issued" || c.ExpiresAtUtc < DateTime.UtcNow) return null;
                    if (c.AttemptCount >= maxAttempts) { c.Status = "Failed"; return null; }
                    c.AttemptCount++;
                    if (c.TokenHash != tokenHash)
                    {
                        if (c.AttemptCount >= maxAttempts) c.Status = "Failed";
                        return null;
                    }
                    c.Verify();
                    return new Result();
                }
                return null;
            }
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        var returnCode = await conn.ExecuteScalarAsync<int>("dbo.PR_IDENTITY_CONSUME_STEPUP_CHALLENGE", new
        {
            Id = Guid.Parse(id),
            UserId = Guid.Parse(userId),
            SessionId = sessionId != null && Guid.TryParse(sessionId, out var parsedSession) ? (Guid?)parsedSession : null,
            ExpectedPurpose = expectedPurpose,
            TokenHash = tokenHash,
            MaxAttempts = maxAttempts
        }, commandType: System.Data.CommandType.StoredProcedure);

        if (returnCode < 0)
        {
            return null;
        }
        return new Result();
    }

    public async Task<int> GetRecentStepUpChallengesCountAsync(string userId, string purpose, TimeSpan window, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            var cutoff = DateTime.UtcNow.Subtract(window);
            return InMemoryStepUpChallenges.Values.Count(c => c.UserId == userId && c.TargetAction == purpose && c.CreatedAtUtc >= cutoff);
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        var query = @"
            SELECT COUNT(1)
            FROM dbo.STEP_UP_CHALLENGE
            WHERE UserId = @UserId AND TargetAction = @Purpose AND CreatedAtUtc >= @Cutoff";
        var cutoffDate = DateTime.UtcNow.Subtract(window);
        return await conn.ExecuteScalarAsync<int>(query, new { UserId = Guid.Parse(userId), Purpose = purpose, Cutoff = cutoffDate });
    }

    public async Task<Result> CreateStepUpProofAsync(StepUpProof proof, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryStepUpProofs) { InMemoryStepUpProofs[proof.ProofHash] = proof; }
            return new Result();
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync("dbo.PR_IDENTITY_CREATE_STEPUP_PROOF", new
        {
            ProofId = Guid.Parse(proof.Id),
            UserId = Guid.Parse(proof.UserId),
            SessionId = string.IsNullOrWhiteSpace(proof.SessionId) ? (Guid?)null : Guid.Parse(proof.SessionId),
            TargetAction = proof.TargetAction,
            ProofHash = proof.ProofHash,
            IssuedAtUtc = proof.IssuedAtUtc,
            ExpiresAtUtc = proof.ExpiresAtUtc,
            Status = proof.Status
        }, commandType: System.Data.CommandType.StoredProcedure);
        return new Result();
    }

    public async Task<Result<string?>> ConsumeStepUpProofAsync(string proofHash, string userId, string? sessionId, string targetAction, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryStepUpProofs)
            {
                if (InMemoryStepUpProofs.TryGetValue(proofHash, out var p))
                {
                    bool isSessionMatch = true;
                    if (!string.IsNullOrWhiteSpace(sessionId) || !string.IsNullOrWhiteSpace(p.SessionId))
                    {
                        var g1 = Guid.TryParse(p.SessionId, out var p1) ? p1.ToString("N") : p.SessionId ?? string.Empty;
                        var g2 = Guid.TryParse(sessionId, out var p2) ? p2.ToString("N") : sessionId ?? string.Empty;
                        isSessionMatch = string.Equals(g1, g2, StringComparison.OrdinalIgnoreCase);
                    }

                    if (p.UserId == userId &&
                    isSessionMatch &&
                    p.TargetAction == targetAction &&
                    p.Status == "Issued" &&
                    p.ExpiresAtUtc >= DateTime.UtcNow &&
                    p.ConsumedAtUtc == null)
                    {
                        p.Status = "Consumed";
                        p.ConsumedAtUtc = DateTime.UtcNow;
                        return p.Id;
                    }
                }
                return (string?)null;
            }
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        var id = await conn.QuerySingleOrDefaultAsync<Guid?>("dbo.PR_IDENTITY_CONSUME_STEPUP_PROOF", new
        {
            ProofHash = proofHash,
            UserId = Guid.Parse(userId),
            SessionId = string.IsNullOrWhiteSpace(sessionId) ? (Guid?)null : Guid.Parse(sessionId),
            TargetAction = targetAction
        }, commandType: System.Data.CommandType.StoredProcedure);

        return id.HasValue ? id.Value.ToString("N") : (string?)null;
    }

    public async Task<Result<ServiceClient>> CreateServiceClientAsync(ServiceClient client, ServiceClientCredential credential, List<ServiceClientScope> scopes, string? outboxPayload, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryServiceClients) { InMemoryServiceClients[client.Id] = (client, new List<ServiceClientCredential> { credential }, scopes); }
            return client;
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        var scopeObj = scopes.FirstOrDefault();
        await conn.ExecuteAsync("dbo.PR_IDENTITY_CREATE_SERVICE_CLIENT_WITH_CREDENTIAL", new
        {
            ClientId = Guid.Parse(client.Id),
            ClientName = client.ClientId,
            CredentialId = Guid.Parse(credential.Id),
            KeyId = credential.KeyId,
            SecretHash = credential.SecretHash,
            ExpiresAtUtc = credential.ExpiresAtUtc,
            ScopeId = scopeObj != null ? (Guid?)Guid.Parse(scopeObj.Id) : (Guid?)Guid.NewGuid(),
            Scope = scopeObj?.Scope ?? string.Empty,
            OutboxId = outboxPayload != null ? (Guid?)Guid.NewGuid() : null,
            OutboxMessageType = "identity.workload.client-created.v1",
            OutboxPayload = outboxPayload
        }, commandType: System.Data.CommandType.StoredProcedure);
        return client;
    }

    public async Task<Result<ServiceClientCredential>> RotateServiceClientCredentialAsync(string serviceClientId, ServiceClientCredential newCredential, string? outboxPayload, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryServiceClients)
            {
                if (InMemoryServiceClients.TryGetValue(serviceClientId, out var t))
                {
                    t.Credentials.Add(newCredential);
                    t.Client.ClientSecretHash = newCredential.SecretHash;
                }
            }
            return newCredential;
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync("dbo.PR_IDENTITY_ROTATE_SERVICE_CLIENT_CREDENTIAL", new
        {
            CredentialId = Guid.Parse(newCredential.Id),
            ServiceClientId = Guid.Parse(serviceClientId),
            KeyId = newCredential.KeyId,
            NewSecretHash = newCredential.SecretHash,
            ExpiresAtUtc = newCredential.ExpiresAtUtc,
            OutboxId = outboxPayload != null ? (Guid?)Guid.NewGuid() : null,
            OutboxMessageType = "identity.workload.credential-rotated.v1",
            OutboxPayload = outboxPayload
        }, commandType: System.Data.CommandType.StoredProcedure);
        return newCredential;
    }

    public async Task<Result> RevokeServiceClientCredentialAsync(string credentialId, string? outboxPayload, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryServiceClients)
            {
                foreach (var t in InMemoryServiceClients.Values)
                {
                    var cred = t.Credentials.FirstOrDefault(x => x.Id == credentialId);
                    if (cred != null) cred.Revoke();
                }
            }
            return new Result();
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync("dbo.PR_IDENTITY_REVOKE_SERVICE_CLIENT_CREDENTIAL", new
        {
            CredentialId = Guid.Parse(credentialId),
            OutboxId = outboxPayload != null ? (Guid?)Guid.NewGuid() : null,
            OutboxMessageType = "identity.workload.credential-revoked.v1",
            OutboxPayload = outboxPayload
        }, commandType: System.Data.CommandType.StoredProcedure);
        return new Result();
    }

    public async Task<Result<ServiceClientCredential?>> GetServiceClientCredentialAsync(string clientSecretHash, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryServiceClients)
            {
                foreach (var t in InMemoryServiceClients.Values)
                {
                    var cred = t.Credentials.FirstOrDefault(x => x.SecretHash == clientSecretHash && !x.IsRevoked);
                    if (cred != null && cred.IsActive(DateTime.UtcNow)) return cred;
                }
                return (ServiceClientCredential?)null;
            }
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        return await conn.QuerySingleOrDefaultAsync<ServiceClientCredential>("dbo.PR_IDENTITY_GET_SERVICE_CLIENT_CREDENTIAL", new
        {
            SecretHash = clientSecretHash
        }, commandType: System.Data.CommandType.StoredProcedure);
    }

    public async Task<Result<List<ServiceClientCredential>>> ListServiceClientCredentialsAsync(string serviceClientId, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryServiceClients)
            {
                if (InMemoryServiceClients.TryGetValue(serviceClientId, out var t)) return t.Credentials.ToList();
                return new List<ServiceClientCredential>();
            }
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        var list = await conn.QueryAsync<ServiceClientCredential>("dbo.PR_IDENTITY_LIST_SERVICE_CLIENT_CREDENTIALS", new
        {
            ServiceClientId = Guid.Parse(serviceClientId)
        }, commandType: System.Data.CommandType.StoredProcedure);
        return list.AsList();
    }

    public async Task<Result<List<string>>> GetServiceClientScopesAsync(string serviceClientId, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryServiceClients)
            {
                if (InMemoryServiceClients.TryGetValue(serviceClientId, out var t)) return t.Scopes.Select(x => x.Scope).ToList();
                return new List<string>();
            }
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        var list = await conn.QueryAsync<string>("dbo.PR_IDENTITY_GET_SERVICE_CLIENT_SCOPES", new
        {
            ServiceClientId = Guid.Parse(serviceClientId)
        }, commandType: System.Data.CommandType.StoredProcedure);
        return list.AsList();
    }

    public async Task<Result> UpdateUserStatusAsync(string userId, AccountStatus status, string? reason, string actor, string? outboxPayload, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryLookups)
            {
                var keys = InMemoryLookups.Where(x => x.Value.Id == userId).Select(x => x.Key).ToList();
                foreach (var k in keys)
                {
                    var old = InMemoryLookups[k];
                    InMemoryLookups[k] = old with { Status = status.ToString() };
                }
            }
            return new Result();
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync("dbo.PR_IDENTITY_UPDATE_USER_STATUS", new
        {
            UserId = Guid.Parse(userId),
            Status = status.ToString(),
            Reason = reason ?? string.Empty,
            Actor = actor,
            OutboxId = outboxPayload != null ? (Guid?)Guid.NewGuid() : null,
            OutboxMessageType = "identity.user.status-updated.v1",
            OutboxPayload = outboxPayload
        }, commandType: System.Data.CommandType.StoredProcedure);
        return new Result();
    }

    public async Task<Result> SaveSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemorySecurityEvents) { InMemorySecurityEvents.Add(securityEvent); }
            return new Result();
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        await conn.ExecuteAsync("dbo.PR_IDENTITY_SAVE_SECURITY_EVENT", new
        {
            Id = Guid.Parse(securityEvent.Id),
            EventType = securityEvent.EventType,
            Actor = securityEvent.Actor,
            TargetUserId = string.IsNullOrWhiteSpace(securityEvent.TargetUserId) ? (Guid?)null : Guid.Parse(securityEvent.TargetUserId),
            Reason = securityEvent.Reason ?? string.Empty,
            RequestId = securityEvent.RequestId ?? string.Empty,
            CorrelationId = securityEvent.CorrelationId ?? string.Empty
        }, commandType: System.Data.CommandType.StoredProcedure);
        return new Result();
    }
    public async Task<int> GetRecentVerificationCountAsync(string userId, string channel, TimeSpan window, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.Subtract(window);
        if (_useInMemoryFallback)
        {
            lock (InMemoryVerifications)
            {
                return InMemoryVerifications.Values.Count(x => x.UserId == userId && x.Channel == channel && x.CreatedAtUtc >= cutoff);
            }
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.ACCOUNT_VERIFICATION WHERE UserId = @UserId AND Channel = @Channel AND CreatedAtUtc >= @Cutoff",
            new { UserId = Guid.Parse(userId), Channel = channel, Cutoff = cutoff });
    }

    public async Task<AccountVerification?> GetLatestVerificationAsync(string userId, string channel, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryVerifications)
            {
                return InMemoryVerifications.Values
                    .Where(x => x.UserId == userId && x.Channel == channel)
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .FirstOrDefault();
            }
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<AccountVerification>(
            "SELECT TOP 1 * FROM dbo.ACCOUNT_VERIFICATION WHERE UserId = @UserId AND Channel = @Channel ORDER BY CreatedAtUtc DESC",
            new { UserId = Guid.Parse(userId), Channel = channel });
    }

    public async Task<int> GetRecentRecoveryCountAsync(string userId, TimeSpan window, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.Subtract(window);
        if (_useInMemoryFallback)
        {
            lock (InMemoryRecoveries)
            {
                return InMemoryRecoveries.Values.Count(x => x.UserId == userId && x.CreatedAtUtc >= cutoff);
            }
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM dbo.ACCOUNT_RECOVERY WHERE UserId = @UserId AND CreatedAtUtc >= @Cutoff",
            new { UserId = Guid.Parse(userId), Cutoff = cutoff });
    }

    public async Task<PasswordRecovery?> GetLatestRecoveryAsync(string userId, CancellationToken cancellationToken)
    {
        if (_useInMemoryFallback)
        {
            lock (InMemoryRecoveries)
            {
                return InMemoryRecoveries.Values
                    .Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .FirstOrDefault();
            }
        }
        using var conn = await OpenConnectionAsync(cancellationToken);
        return await conn.QueryFirstOrDefaultAsync<PasswordRecovery>(
            "SELECT TOP 1 * FROM dbo.ACCOUNT_RECOVERY WHERE UserId = @UserId ORDER BY CreatedAtUtc DESC",
            new { UserId = Guid.Parse(userId) });
    }
}
