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

        public string GenerateAccessToken(string userId, string email, string sessionId, bool emailVerified, string amr = "pwd") => _inner.GenerateAccessToken(userId, email, sessionId, emailVerified, amr);
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
        _service = new IdentityApplicationService(_repo, tokenGen, hasher);
    }


    [Fact]
    public async Task Mfa_Registration_Confirmation_And_Mfa_Login_Verification_Flow()
    {
        var ct = CancellationToken.None;

        // 1. Register User
        var regRes = await _service.RegisterAsync(new RegisterRequest("mfa_user@emcore.com", "9990001111", "SecurePass!23"), ct);
        Assert.True(regRes.IsSuccess);
        string userId = regRes.Data!.UserId;

        // Verify Email
        var verifyEmailRes = await _service.ConfirmEmailVerificationAsync(new ConfirmEmailVerificationRequest("mfa_user@emcore.com", "654321"), ct);
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
        var loginRes = await _service.LoginAsync(new LoginRequest("mfa_user@emcore.com", "SecurePass!23"), ct);
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
    public async Task StepUp_Authentication_Challenge_And_Verification_Flow()
    {
        var ct = CancellationToken.None;

        var regRes = await _service.RegisterAsync(new RegisterRequest("stepup@emcore.com", "9990002222", "SecurePass!23"), ct);
        string userId = regRes.Data!.UserId;

        // Initiate StepUp for high risk action
        var initRes = await _service.InitiateStepUpAsync(userId, new InitiateStepUpRequest("TransferFunds"), ct);
        Assert.True(initRes.IsSuccess);
        Assert.NotNull(initRes.Data?.StepUpId);

        // Verify StepUp
        var verRes = await _service.VerifyStepUpAsync(userId, new VerifyStepUpRequest(initRes.Data!.StepUpId, "654321"), ct);
        Assert.True(verRes.IsSuccess);
        Assert.StartsWith("STEPUP_OK_TransferFunds_", verRes.Data!.VerificationToken);
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
        if (verifyRes.StatusCode == 400) throw new Exception($"verifyRes returned 400: {verifyRes.ErrorDetail}");
        Assert.False(verifyRes.IsSuccess);
        Assert.Equal(401, verifyRes.StatusCode);
    }
}
