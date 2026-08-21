using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Emcore.IdentityAccess.Infrastructure.Persistence;
using Emcore.IdentityAccess.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Emcore.IdentityAccess.Application.Abstractions;

namespace Emcore.IdentityAccess.IntegrationTests;

public class SecurityHardeningTests
{
    private readonly IdentityApplicationService _service;

    private class TestTokenGenerator : Emcore.IdentityAccess.Application.Abstractions.ITokenGenerator, Emcore.IdentityAccess.Application.Abstractions.IJwksService
    {
        private readonly JwtTokenGenerator _inner = new JwtTokenGenerator();
        private readonly string _overrideToken;

        public TestTokenGenerator(string overrideToken)
        {
            _overrideToken = overrideToken;
        }

        public TokenResult GenerateAccessToken(string userId, string email, string sessionId, bool emailVerified, string amr = "pwd") => _inner.GenerateAccessToken(userId, email, sessionId, emailVerified, amr);
        public (string Token, string Hash) GenerateRefreshToken() => _inner.GenerateRefreshToken();
        public (string Token, string Hash) GenerateVerificationToken() => (_overrideToken, HashToken(_overrideToken));
        public (string Token, string Hash) GenerateKeyedVerificationToken(string verificationId, string normalizedDestination) => (_overrideToken, HashKeyedToken(verificationId, normalizedDestination, _overrideToken));
        public (string Token, string Hash) GeneratePasswordResetToken() => _inner.GeneratePasswordResetToken();
        public string HashToken(string rawToken) => _inner.HashToken(rawToken);
        public string HashKeyedToken(string verificationId, string normalizedDestination, string rawOtp) => _inner.HashKeyedToken(verificationId, normalizedDestination, rawOtp);
        public string GetJwksJson() => _inner.GetJwksJson();
    }

    private readonly IdentityRepository _repo;

    public SecurityHardeningTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:IdentityDatabase"] = "inmemory-security-test-db"
            })
            .Build();

        _repo = new IdentityRepository(config);
        var hasher = new Pbkdf2PasswordHasher();
        var tokenGen = new TestTokenGenerator("654321");
        _service = new IdentityApplicationService(_repo, tokenGen, hasher, new Emcore.IdentityAccess.Application.Configuration.IdentityOptions(), null);
    }

    [Fact]
    public async Task Mfa_Registration_Confirmation_And_Mfa_Login_Verification_Flow()
    {
        var ct = CancellationToken.None;

        var uniqueEmail = $"mfa_user_{Guid.NewGuid():N}@emcore.com";
        var uniquePhone = $"999{new Random().Next(1000000, 9999999)}";
        // 1. Register User
        var regRes = await _service.RegisterAsync(new RegisterRequest(uniqueEmail, uniquePhone, "SecurePass!23"), ct);
        Assert.True(regRes.IsSuccess);
        string userId = regRes.Data!.UserId;

        // Verify Email
        var verifyEmailRes = await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest(uniqueEmail, "654321"), ct);
        if (!verifyEmailRes.IsSuccess) throw new Exception($"VerifyEmail failed: {verifyEmailRes.ErrorDetail}");
        Assert.True(verifyEmailRes.IsSuccess);

        // 2. Register MFA (EMAIL_OTP)
        var mfaRegRes = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);
        if (!mfaRegRes.IsSuccess) throw new Exception($"mfaRegRes failed: {mfaRegRes.ErrorDetail}");
        Assert.True(mfaRegRes.IsSuccess);
        Assert.NotNull(mfaRegRes.Data?.ChallengeId);

        // 3. Confirm MFA
        var confirmRes = await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "654321", mfaRegRes.Data?.ChallengeId), ct);
        if (!confirmRes.IsSuccess) throw new Exception($"confirmRes failed: {confirmRes.ErrorDetail}");
        Assert.True(confirmRes.IsSuccess);

        // 4. Login now requires MFA
        var loginRes = await _service.LoginAsync(new LoginRequest(uniqueEmail, "SecurePass!23"), ct);
        Assert.True(loginRes.IsSuccess);
        Assert.True(loginRes.Data!.MfaRequired);
        Assert.NotEmpty(loginRes.Data.MfaChallengeToken!); // Challenge ID

        // 5. Verify MFA to finish login
        var verifyRes = await _service.VerifyMfaLoginAsync(new MfaLoginVerifyRequest(userId, loginRes.Data.MfaChallengeToken!, "654321"), ct);
        Assert.True(verifyRes.IsSuccess);
        Assert.False(verifyRes.Data!.MfaRequired);
        Assert.StartsWith("eyJ", verifyRes.Data.AccessToken);
    }

    [Fact]
    public async Task Workload_ServiceClient_Registration_Rotation_And_Token_Issuance()
    {
        var ct = CancellationToken.None;

        // 1. Register Service Client
        var regRes = await _service.RegisterServiceClientAsync(new RegisterServiceClientRequest("billing-service-v1", new List<string> { "orders:read", "invoices:write" }, 365), ct);
        Assert.True(regRes.IsSuccess);
        Assert.Equal("billing-service-v1", regRes.Data!.ClientId);
        Assert.NotEmpty(regRes.Data.ClientSecret);
        string serviceClientId = regRes.Data.Id;
        string initialSecret = regRes.Data.ClientSecret;

        // 2. Request token with valid secret and scope
        var tokenRes = await _service.IssueServiceTokenAsync(new ServiceTokenRequest("billing-service-v1", initialSecret, "orders:read"), ct);
        Assert.True(tokenRes.IsSuccess);
        Assert.StartsWith("eyJ", tokenRes.Data!.AccessToken);

        // 3. Request token for unauthorized scope fails
        var failScopeRes = await _service.IssueServiceTokenAsync(new ServiceTokenRequest("billing-service-v1", initialSecret, "admin:root"), ct);
        Assert.False(failScopeRes.IsSuccess);
        Assert.Equal(403, failScopeRes.StatusCode);

        // 4. Rotate credential
        var rotateRes = await _service.RotateServiceClientCredentialAsync(new RotateServiceClientCredentialRequest(serviceClientId), ct);
        Assert.True(rotateRes.IsSuccess);
        string newSecret = rotateRes.Data!.NewClientSecret;

        // 5. New credential issues valid token
        var newTokenRes = await _service.IssueServiceTokenAsync(new ServiceTokenRequest("billing-service-v1", newSecret, "invoices:write"), ct);
        Assert.True(newTokenRes.IsSuccess);
    }

    [Fact]
    public async Task Administrative_Account_Status_Modifications_Enforce_Reason_And_Login_Restriction()
    {
        var ct = CancellationToken.None;

        // Register User
        var regRes = await _service.RegisterAsync(new RegisterRequest("suspect@emcore.com", "9990003333", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;

        // Attempt admin status update without reason -> fails with 400
        var noReasonRes = await _service.AdminUpdateUserStatusAsync(new AdminUpdateUserStatusRequest(userId, "Suspended", ""), "sec-admin", ct);
        Assert.False(noReasonRes.IsSuccess);
        Assert.Equal(400, noReasonRes.StatusCode);

        // Valid admin update to Suspended with mandatory reason -> succeeds
        var validRes = await _service.AdminUpdateUserStatusAsync(new AdminUpdateUserStatusRequest(userId, "Suspended", "Security incident #9897 detected"), "sec-admin", ct);
        Assert.True(validRes.IsSuccess);

        // Subsequent login attempt is blocked due to suspended status
        var loginRes = await _service.LoginAsync(new LoginRequest("suspect@emcore.com", "SecurePass!23"), ct);
        Assert.False(loginRes.IsSuccess);
        Assert.Equal(403, loginRes.StatusCode);
        Assert.Contains("Suspended", loginRes.ErrorDetail);
    }

    [Fact]
    public async Task Security_Bypass_123456_Should_Fail()
    {
        var ct = CancellationToken.None;

        var regRes = await _service.RegisterAsync(new RegisterRequest("bypass_mfa@emcore.com", "9990004444", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;

        var verifyEmailRes1 = await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("bypass_mfa@emcore.com", "654321"), ct);
        if (!verifyEmailRes1.IsSuccess) throw new Exception($"VerifyEmail failed: {verifyEmailRes1.ErrorDetail}");

        var mfaRegRes = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);
        await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "654321", mfaRegRes.Data?.ChallengeId), ct);

        var loginRes = await _service.LoginAsync(new LoginRequest("bypass_mfa@emcore.com", "SecurePass!23"), ct);
        if (!loginRes.IsSuccess) throw new Exception($"login failed: {loginRes.ErrorDetail}");

        // Try the bypass literal "123456"
        var verifyRes = await _service.VerifyMfaLoginAsync(new MfaLoginVerifyRequest(userId, loginRes.Data!.MfaChallengeToken!, "123456"), ct);
        if (verifyRes.StatusCode == 400) throw new Exception($"verifyRes returned 400: {verifyRes.ErrorDetail}");
        Assert.False(verifyRes.IsSuccess);
        Assert.Equal(401, verifyRes.StatusCode);
    }

    [Fact]
    public async Task Security_Bypass_Password_Hashed_Should_Fail()
    {
        var ct = CancellationToken.None;

        await _service.RegisterAsync(new RegisterRequest("bypass_pwd@emcore.com", "9990005555", "SecurePass!23"), ct);

        // Real PBKDF2 hash + correct password -> PASS
        var correctLogin = await _service.LoginAsync(new LoginRequest("bypass_pwd@emcore.com", "SecurePass!23"), ct);
        Assert.True(correctLogin.IsSuccess);

        // Real PBKDF2 hash + wrong password -> FAIL
        var wrongLogin = await _service.LoginAsync(new LoginRequest("bypass_pwd@emcore.com", "WrongPass!23"), ct);
        Assert.False(wrongLogin.IsSuccess);

        // CorrectPassword_hashed -> FAIL
        var bypassLogin = await _service.LoginAsync(new LoginRequest("bypass_pwd@emcore.com", "SecurePass!23_hashed"), ct);
        Assert.False(bypassLogin.IsSuccess);
        Assert.Equal(401, bypassLogin.StatusCode);
    }

    [Fact]
    public async Task Security_Bypass_RECOVERY_ALL_Should_Fail()
    {
        var ct = CancellationToken.None;

        var regRes = await _service.RegisterAsync(new RegisterRequest("bypass_rec@emcore.com", "9990006666", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;

        var verifyEmailRes2 = await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("bypass_rec@emcore.com", "654321"), ct);
        if (!verifyEmailRes2.IsSuccess) throw new Exception($"VerifyEmail failed: {verifyEmailRes2.ErrorDetail}");

        var mfaRegRes = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);
        await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "654321", mfaRegRes.Data?.ChallengeId), ct);

        var loginRes = await _service.LoginAsync(new LoginRequest("bypass_rec@emcore.com", "SecurePass!23"), ct);
        if (!loginRes.IsSuccess) throw new Exception($"login failed: {loginRes.ErrorDetail}");

        // Try RECOVERY-ALL bypass
        var verifyRes = await _service.VerifyMfaLoginAsync(new MfaLoginVerifyRequest(userId, loginRes.Data!.MfaChallengeToken!, string.Empty) { RecoveryCode = "RECOVERY-ALL" }, ct);
        Assert.False(verifyRes.IsSuccess);
        Assert.Equal(400, verifyRes.StatusCode);
    }

    [Fact]
    public async Task Mfa_Enrollment_Unverified_Email_Should_Fail()
    {
        var ct = CancellationToken.None;
        var regRes = await _service.RegisterAsync(new RegisterRequest("unverified@emcore.com", "9990001000", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;

        var mfaRegRes = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);
        Assert.False(mfaRegRes.IsSuccess);
        Assert.Equal(400, mfaRegRes.StatusCode);
    }

    [Fact]
    public async Task Mfa_Enrollment_Wrong_OTP_Should_Fail()
    {
        var ct = CancellationToken.None;
        var regRes = await _service.RegisterAsync(new RegisterRequest("wrong_otp_reg@emcore.com", "9990001001", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;
        await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("wrong_otp_reg@emcore.com", "654321"), ct);

        var mfaRegRes = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);

        var confirmRes = await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "000000", mfaRegRes.Data?.ChallengeId), ct);
        Assert.False(confirmRes.IsSuccess);
        Assert.Equal(400, confirmRes.StatusCode);
    }

    [Fact]
    public async Task Mfa_Enrollment_Unsupported_Mfa_Type_Should_Fail()
    {
        var ct = CancellationToken.None;
        var regRes = await _service.RegisterAsync(new RegisterRequest("unsupported_type@emcore.com", "9990001002", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;
        await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("unsupported_type@emcore.com", "654321"), ct);

        var mfaRegRes = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);

        var confirmRes = await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("TOTP", "654321", mfaRegRes.Data?.ChallengeId), ct);
        Assert.False(confirmRes.IsSuccess);
        Assert.Equal(400, confirmRes.StatusCode);
    }

    [Fact]
    public async Task Mfa_Login_Tokens_Withheld_Before_Mfa()
    {
        var ct = CancellationToken.None;
        var regRes = await _service.RegisterAsync(new RegisterRequest("withheld@emcore.com", "9990001003", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;
        await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("withheld@emcore.com", "654321"), ct);
        var mfaRegRes = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);
        await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "654321", mfaRegRes.Data?.ChallengeId), ct);

        var loginRes = await _service.LoginAsync(new LoginRequest("withheld@emcore.com", "SecurePass!23"), ct);
        Assert.True(loginRes.IsSuccess);
        Assert.True(loginRes.Data!.MfaRequired);
        Assert.True(string.IsNullOrEmpty(loginRes.Data.AccessToken));
        Assert.True(string.IsNullOrEmpty(loginRes.Data.RefreshToken));
        Assert.NotEmpty(loginRes.Data.MfaChallengeToken!);
    }

    [Fact]
    public async Task Mfa_Login_Wrong_OTP_Should_Fail()
    {
        var ct = CancellationToken.None;
        var regRes = await _service.RegisterAsync(new RegisterRequest("wrong_otp_login@emcore.com", "9990001004", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;
        await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("wrong_otp_login@emcore.com", "654321"), ct);
        var mfaRegRes = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);
        await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "654321", mfaRegRes.Data?.ChallengeId), ct);

        var loginRes = await _service.LoginAsync(new LoginRequest("wrong_otp_login@emcore.com", "SecurePass!23"), ct);

        var verifyRes = await _service.VerifyMfaLoginAsync(new MfaLoginVerifyRequest(userId, loginRes.Data!.MfaChallengeToken!, "000000"), ct);
        Assert.False(verifyRes.IsSuccess);
        Assert.Equal(401, verifyRes.StatusCode);
    }

    [Fact]
    public async Task Mfa_OTP_One_Time_Use_Should_Fail()
    {
        var ct = CancellationToken.None;
        var regRes = await _service.RegisterAsync(new RegisterRequest("otp_reuse@emcore.com", "9990001005", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;
        await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("otp_reuse@emcore.com", "654321"), ct);
        var mfaRegRes = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);
        await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "654321", mfaRegRes.Data?.ChallengeId), ct);

        var loginRes = await _service.LoginAsync(new LoginRequest("otp_reuse@emcore.com", "SecurePass!23"), ct);

        var verifyRes1 = await _service.VerifyMfaLoginAsync(new MfaLoginVerifyRequest(userId, loginRes.Data!.MfaChallengeToken!, "654321"), ct);
        Assert.True(verifyRes1.IsSuccess);

        var verifyRes2 = await _service.VerifyMfaLoginAsync(new MfaLoginVerifyRequest(userId, loginRes.Data!.MfaChallengeToken!, "654321"), ct);
        Assert.False(verifyRes2.IsSuccess);
    }

    [Fact]
    public async Task Mfa_Purpose_Binding_Should_Fail()
    {
        var ct = CancellationToken.None;
        var regRes = await _service.RegisterAsync(new RegisterRequest("purpose_binding@emcore.com", "9990001006", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;
        await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("purpose_binding@emcore.com", "654321"), ct);
        var mfaRegRes = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);

        // Attempt to use Enrollment OTP for Login
        var verifyRes = await _service.VerifyMfaLoginAsync(new MfaLoginVerifyRequest(userId, mfaRegRes.Data!.ChallengeId!, "654321"), ct);
        Assert.False(verifyRes.IsSuccess);
    }

    [Fact]
    public async Task Mfa_User_Binding_Should_Fail()
    {
        var ct = CancellationToken.None;
        var regRes1 = await _service.RegisterAsync(new RegisterRequest("user_bind1@emcore.com", "9990001007", "SecurePass!23"), ct);
        var regRes2 = await _service.RegisterAsync(new RegisterRequest("user_bind2@emcore.com", "9990001008", "SecurePass!23"), ct);
        string user1 = regRes1.Data!.UserId;
        string user2 = regRes2.Data!.UserId;
        await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("user_bind1@emcore.com", "654321"), ct);
        var mfaRegRes1 = await _service.RegisterMfaAsync(user1, new RegisterMfaRequest("EMAIL_OTP"), ct);

        // Attempt to use user1's challenge with user2
        var confirmRes = await _service.ConfirmMfaAsync(user2, new ConfirmMfaRequest("EMAIL_OTP", "654321", mfaRegRes1.Data?.ChallengeId), ct);
        Assert.False(confirmRes.IsSuccess);
    }

    [Fact]
    public async Task Mfa_Challenge_Binding_Should_Fail()
    {
        var ct = CancellationToken.None;
        var regRes = await _service.RegisterAsync(new RegisterRequest("challenge_bind@emcore.com", "9990001009", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;
        await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("challenge_bind@emcore.com", "654321"), ct);

        var mfaRegRes1 = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);

        // Bypass cooldown to get second challenge
        var dictField = typeof(IdentityRepository).GetField("InMemoryStepUpChallenges", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var dict = dictField!.GetValue(null) as System.Collections.IDictionary;
        var challenge1 = dict![mfaRegRes1.Data!.ChallengeId!];
        var prop = challenge1!.GetType().GetProperty("CreatedAtUtc");
        prop!.SetValue(challenge1, DateTime.UtcNow.AddSeconds(-61));

        var mfaRegRes2 = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);

        // Try wrong OTP for challenge B
        var confirmRes = await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "000000", mfaRegRes2.Data?.ChallengeId), ct);
        Assert.False(confirmRes.IsSuccess);
    }

    [Fact]
    public async Task Mfa_Expired_OTP_Should_Fail()
    {
        var ct = CancellationToken.None;
        var regRes = await _service.RegisterAsync(new RegisterRequest("expire@emcore.com", "9990001010", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;
        await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("expire@emcore.com", "654321"), ct);
        var mfaRegRes = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);

        // Manipulate expiry using reflection
        var dictField = typeof(IdentityRepository).GetField("InMemoryStepUpChallenges", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var dict = dictField!.GetValue(null) as System.Collections.IDictionary;
        var challenge = dict![mfaRegRes.Data!.ChallengeId!];
        var prop = challenge!.GetType().GetProperty("ExpiresAtUtc");
        prop!.SetValue(challenge, DateTime.UtcNow.AddMinutes(-10));

        var confirmRes = await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "654321", mfaRegRes.Data?.ChallengeId), ct);
        Assert.False(confirmRes.IsSuccess);
    }

    [Fact]
    public async Task Mfa_Maximum_Verification_Attempts_Should_Lockout()
    {
        var ct = CancellationToken.None;
        var regRes = await _service.RegisterAsync(new RegisterRequest("lockout@emcore.com", "9990001011", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;
        await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("lockout@emcore.com", "654321"), ct);
        var mfaRegRes = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);

        // 5 wrong attempts
        for (int i = 0; i < 5; i++)
        {
            await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "000000", mfaRegRes.Data?.ChallengeId), ct);
        }

        // 6th attempt with CORRECT OTP should fail because it's locked out
        var confirmRes = await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "654321", mfaRegRes.Data?.ChallengeId), ct);
        Assert.False(confirmRes.IsSuccess);
    }

    [Fact]
    public async Task Mfa_Resend_Cooldown_Should_Throttle()
    {
        var ct = CancellationToken.None;
        var regRes = await _service.RegisterAsync(new RegisterRequest("cooldown@emcore.com", "9990001012", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;
        await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("cooldown@emcore.com", "654321"), ct);

        var mfaRegRes1 = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);
        Assert.True(mfaRegRes1.IsSuccess);

        // Immediate second request should be throttled
        var resendRes = await _service.ResendMfaAsync(userId, new ResendMfaRequest(userId, mfaRegRes1.Data!.ChallengeId!), ct);
        Assert.False(resendRes.IsSuccess);
        Assert.Equal(429, resendRes.StatusCode);
    }

    [Fact]
    public async Task Mfa_Send_Rate_Limit_Should_Reject()
    {
        var ct = CancellationToken.None;
        var regRes = await _service.RegisterAsync(new RegisterRequest("ratelimit@emcore.com", "9990001013", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;
        await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("ratelimit@emcore.com", "654321"), ct);

        var dictField = typeof(IdentityRepository).GetField("InMemoryStepUpChallenges", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var dict = dictField!.GetValue(null) as System.Collections.IDictionary;

        for (int i = 0; i < 5; i++)
        {
            var res = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);
            if (res.IsSuccess)
            {
                var challenge = dict![res.Data!.ChallengeId!];
                var prop = challenge!.GetType().GetProperty("CreatedAtUtc");
                prop!.SetValue(challenge, DateTime.UtcNow.AddSeconds(-61));
            }
        }

        // 6th attempt within 15 mins should hit max limit
        var mfaRegRes6 = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);
        Assert.False(mfaRegRes6.IsSuccess);
        Assert.Equal(429, mfaRegRes6.StatusCode);
        Assert.Contains("Maximum OTP send limit", mfaRegRes6.ErrorDetail);
    }

    [Fact]
    public async Task Mfa_Resend_Invalidates_Old_OTP()
    {
        var ct = CancellationToken.None;
        var regRes = await _service.RegisterAsync(new RegisterRequest("resend_inv@emcore.com", "9990001014", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;
        await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("resend_inv@emcore.com", "654321"), ct);

        var mfaRegRes1 = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);

        var dictField = typeof(IdentityRepository).GetField("InMemoryStepUpChallenges", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var dict = dictField!.GetValue(null) as System.Collections.IDictionary;
        var challenge1 = dict![mfaRegRes1.Data!.ChallengeId!];
        var prop = challenge1!.GetType().GetProperty("CreatedAtUtc");
        prop!.SetValue(challenge1, DateTime.UtcNow.AddSeconds(-61));

        var resendRes = await _service.ResendMfaAsync(userId, new ResendMfaRequest(userId, mfaRegRes1.Data!.ChallengeId!), ct);
        Assert.True(resendRes.IsSuccess);

        // Assert it fails to use the first one since it was invalidated
        var confirmRes1 = await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "654321", mfaRegRes1.Data?.ChallengeId), ct);
        Assert.False(confirmRes1.IsSuccess);

        // Assert it succeeds with the new challenge
        var confirmRes2 = await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "654321", resendRes.Data?.ChallengeId), ct);
        Assert.True(confirmRes2.IsSuccess);
    }

    [Fact]
    public async Task Mfa_Concurrent_Consumption_Should_Yield_Single_Success()
    {
        var ct = CancellationToken.None;
        var regRes = await _service.RegisterAsync(new RegisterRequest("concurrent@emcore.com", "9990001015", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;
        await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("concurrent@emcore.com", "654321"), ct);
        var mfaRegRes = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("EMAIL_OTP"), ct);

        var t1 = _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "654321", mfaRegRes.Data?.ChallengeId), ct);
        var t2 = _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("EMAIL_OTP", "654321", mfaRegRes.Data?.ChallengeId), ct);
        var results = await Task.WhenAll(t1, t2);

        int successCount = 0;
        int failCount = 0;
        foreach (var r in results)
        {
            if (r.IsSuccess) successCount++;
            else failCount++;
        }
        Assert.Equal(1, successCount);
        Assert.Equal(1, failCount);
    }

    [Fact]
    public async Task Registration_Uses_Canonical_UserId_Across_All_Entities()
    {
        var ct = CancellationToken.None;

        var req = new RegisterRequest("canonical@emcore.com", "9990001234", "SecurePass!23");
        var regRes = await _service.RegisterAsync(req, ct);

        Assert.True(regRes.IsSuccess);
        string canonicalId = regRes.Data!.UserId;

        // Verify USER_ACCOUNT.Id
        var userRes = await _repo.GetUserByIdAsync(canonicalId, ct);
        Assert.NotNull(userRes?.Value);
        Assert.Equal(canonicalId, userRes!.Value.Id);

        // Verify ACCOUNT_VERIFICATION.UserId
        var verification = await _repo.GetLatestVerificationAsync(canonicalId, "Email", ct);
        Assert.NotNull(verification);
        Assert.Equal(canonicalId, verification!.UserId);
    }
}
