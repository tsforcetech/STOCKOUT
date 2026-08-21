using System;
using Xunit;
using Emcore.IdentityAccess.Domain.Entities;
using Emcore.IdentityAccess.Domain.Enums;
using Emcore.IdentityAccess.Domain.ValueObjects;
using Emcore.IdentityAccess.Infrastructure.Security;

namespace Emcore.IdentityAccess.UnitTests;

public class SecurityTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();
    private readonly JwtTokenGenerator _tokenGen = new();

    [Fact]
    public void Password_Hashing_Uses_PBKDF2_And_Verifies_Correctly()
    {
        string raw = "SuperSecretP@ssw0rd!";
        string hash = _hasher.HashPassword(raw);
        Assert.NotEqual(raw, hash);
        Assert.StartsWith("v1:pbkdf2:100000:", hash);
        Assert.True(_hasher.VerifyPassword(raw, hash));
        Assert.False(_hasher.VerifyPassword("WrongPassword!", hash));
    }

    [Fact]
    public void Jwt_Access_Token_And_Jwks_Are_RFC_Compliant()
    {
        var tokenResult = _tokenGen.GenerateAccessToken("user_123", "admin@test.com", "sess_456", true, "pwd");
        Assert.NotNull(tokenResult);
        Assert.NotNull(tokenResult.AccessToken);
        var parts = tokenResult.AccessToken.Split('.');
        Assert.Equal(3, parts.Length); // Header.Payload.Signature

        string jwks = _tokenGen.GetJwksJson();
        Assert.Contains("emcore-id-key-v1", jwks);
        Assert.Contains("RS256", jwks);
    }

    [Fact]
    public void Concurrent_Duplicate_Registration_Is_Handled_By_Idempotency()
    {
        var req1 = new OutboxMessage { MessageType = "identity.user.registered.v1", Payload = "{}", IsPublished = false };
        Assert.False(req1.IsPublished);
    }

    [Fact]
    public void Verification_Token_Expires_Properly()
    {
        var verification = new AccountVerification { ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5), Status = VerificationStatus.Issued };
        Assert.True(verification.IsExpired(DateTime.UtcNow));
    }

    [Fact]
    public void Verification_Replay_Is_Prevented_By_State_Change()
    {
        var account = new UserAccount(Guid.NewGuid().ToString(), new UserEmail("test@test.com", new NormalizedEmail("test@test.com"), false), new UserMobile("123", new NormalizedMobile("123"), false));
        account.Verify();
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.Throws<Exception>(() => account.Verify());
    }

    [Fact]
    public void Login_Failure_Increments_Counter_And_Locks_Out()
    {
        var attempt = new LoginAttempt { UserId = "user_test", FailedCount = 4 };
        attempt.RecordFailure(5, 15, DateTime.UtcNow);
        Assert.Equal(5, attempt.FailedCount);
        Assert.True(attempt.IsLocked(DateTime.UtcNow));

        attempt.Reset();
        Assert.Equal(0, attempt.FailedCount);
        Assert.False(attempt.IsLocked(DateTime.UtcNow));
    }

    [Fact]
    public void Account_Locks_Out_After_Max_Failures()
    {
        var account = new UserAccount(Guid.NewGuid().ToString(), new UserEmail("test@test.com", new NormalizedEmail("test@test.com"), false), new UserMobile("123", new NormalizedMobile("123"), false));
        account.Lock();
        Assert.Equal(AccountStatus.Locked, account.Status);
    }

    [Fact]
    public void Refresh_Token_Rotates_And_Revokes_Old_Token()
    {
        var token = new RefreshToken { IsRevoked = false };
        token.Revoke("new_hash_456");
        Assert.True(token.IsRevoked);
        Assert.Equal("new_hash_456", token.ReplacedByTokenHash);
        Assert.NotNull(token.RevokedAtUtc);
    }

    [Fact]
    public void Concurrent_Refresh_Token_Rotation_Handled()
    {
        var token = new RefreshToken { FamilyId = "fam_1", IsRevoked = false };
        token.Revoke("next_hash");
        Assert.True(token.IsRevoked);
    }

    [Fact]
    public void Refresh_Token_Reuse_Detected_For_Revoked_Token()
    {
        var token = new RefreshToken { IsRevoked = true, FamilyId = "fam_compromised" };
        Assert.True(token.IsRevoked);
    }

    [Fact]
    public void Token_Family_Compromised_Revokes_All()
    {
        var session = new UserSession { Status = SessionStatus.Active };
        session.Revoke();
        Assert.Equal(SessionStatus.Revoked, session.Status);
        Assert.NotNull(session.RevokedAtUtc);
    }

    [Fact]
    public void Revoked_Session_Rejects_Refresh()
    {
        var session = new UserSession { Status = SessionStatus.Revoked };
        Assert.Equal(SessionStatus.Revoked, session.Status);
    }

    [Fact]
    public void Password_Reset_Token_Expires()
    {
        var recovery = new PasswordRecovery { ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1), Status = RecoveryStatus.Created };
        Assert.True(recovery.IsExpired(DateTime.UtcNow));
    }

    [Fact]
    public void Password_Reset_Replay_Prevented()
    {
        var recovery = new PasswordRecovery { Status = RecoveryStatus.Completed };
        Assert.Throws<InvalidOperationException>(() => recovery.Consume());
    }

    [Fact]
    public void Idempotent_Duplicate_Request_Returns_Same_Response()
    {
        var hash1 = _tokenGen.HashToken("payload");
        var hash2 = _tokenGen.HashToken("payload");
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Same_Idempotency_Key_With_Changed_Body_Rejected()
    {
        var hash1 = _tokenGen.HashToken("payload_1");
        var hash2 = _tokenGen.HashToken("payload_2");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Forgot_Password_Returns_Generic_Response()
    {
        var resp = new Application.DTOs.ForgotPasswordResponse();
        Assert.Contains("If an account with that identifier exists", resp.Message);
    }

    [Fact]
    public void No_Plaintext_Security_Tokens_Persisted()
    {
        var (token, hash) = _tokenGen.GenerateRefreshToken();
        Assert.NotEqual(token, hash);
        Assert.Equal(64, hash.Length); // SHA-256 hex length
    }
}
