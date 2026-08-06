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

    public SecurityHardeningTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:IdentityDatabase"] = "inmemory-security-test-db"
            })
            .Build();

        var repo = new IdentityRepository(config);
        var hasher = new Pbkdf2PasswordHasher();
        var tokenGen = new JwtTokenGenerator();
        _service = new IdentityApplicationService(repo, tokenGen, hasher);
    }

    [Fact]
    public async Task Mfa_Registration_Confirmation_And_Mfa_Login_Verification_Flow()
    {
        var ct = CancellationToken.None;

        // 1. Register User
        var regRes = await _service.RegisterAsync(new RegisterRequest("mfa_user@emcore.com", "9990001111", "SecurePass!23"), ct);
        Assert.True(regRes.IsSuccess);
        string userId = regRes.Data!.UserId;

        // 2. Register MFA (TOTP)
        var mfaRegRes = await _service.RegisterMfaAsync(userId, new RegisterMfaRequest("TOTP"), ct);
        Assert.True(mfaRegRes.IsSuccess);
        Assert.NotNull(mfaRegRes.Data?.Secret);

        // 3. Confirm MFA
        var confirmRes = await _service.ConfirmMfaAsync(userId, new ConfirmMfaRequest("TOTP", "123456"), ct);
        Assert.True(confirmRes.IsSuccess);

        // 4. Login now requires MFA
        var loginRes = await _service.LoginAsync(new LoginRequest("mfa_user@emcore.com", "SecurePass!23"), ct);
        Assert.True(loginRes.IsSuccess);
        Assert.True(loginRes.Data!.MfaRequired);
        Assert.NotEmpty(loginRes.Data.MfaChallengeToken!); // Challenge ID

        // 5. Verify MFA to finish login
        var verifyRes = await _service.VerifyMfaLoginAsync(new MfaLoginVerifyRequest(userId, loginRes.Data.MfaChallengeToken!, "123456"), ct);
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
        var verRes = await _service.VerifyStepUpAsync(userId, new VerifyStepUpRequest(initRes.Data!.StepUpId, "123456"), ct);
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
}
