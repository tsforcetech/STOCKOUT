using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Emcore.IdentityAccess.Infrastructure.Persistence;
using Emcore.IdentityAccess.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Emcore.IdentityAccess.IntegrationTests;

public class IdentityEndToEndTests
{
    private readonly IdentityApplicationService _service;

    public IdentityEndToEndTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:IdentityDatabase"] = "inmemory-test-db"
            })
            .Build();

        var repo = new IdentityRepository(config);
        var hasher = new Pbkdf2PasswordHasher();
        var tokenGen = new JwtTokenGenerator();
        _service = new IdentityApplicationService(repo, tokenGen, hasher, new Emcore.IdentityAccess.Application.Configuration.IdentityOptions(), null);
    }

    [Fact]
    public async Task EndToEnd_User_Registration_Login_And_Session_Management()
    {
        var ct = CancellationToken.None;

        // 1. Register User
        var regRes = await _service.RegisterAsync(new RegisterRequest("integration@emcore.com", "9998887777", "Password123!"), ct);
        Assert.True(regRes.IsSuccess);
        Assert.Equal(201, regRes.StatusCode);
        Assert.NotNull(regRes.Data);
        string userId = regRes.Data.UserId;

        // 2. Login successfully
        var loginRes = await _service.LoginAsync(new LoginRequest("integration@emcore.com", "Password123!"), ct);
        Assert.True(loginRes.IsSuccess);
        Assert.Equal(200, loginRes.StatusCode);
        Assert.NotNull(loginRes.Data);
        Assert.StartsWith("eyJ", loginRes.Data.AccessToken); // JWT base64url start

        // 3. Check Account Status
        var statusRes = await _service.GetAccountStatusAsync(userId, ct);
        Assert.True(statusRes.IsSuccess);
        Assert.Equal("integration@emcore.com", statusRes.Data?.Email);

        // 4. Refresh Token Rotation
        var refRes = await _service.RefreshAsync(new RefreshRequest(loginRes.Data.RefreshToken), ct);
        Assert.True(refRes.IsSuccess);
        Assert.NotEqual(loginRes.Data.RefreshToken, refRes.Data?.RefreshToken);

        // 5. Change Password
        var chgRes = await _service.ChangePasswordAsync(userId, new ChangePasswordRequest("Password123!", "NewSecret456!"), ct);
        Assert.True(chgRes.IsSuccess);

        // 6. Old password login fails
        var oldLogin = await _service.LoginAsync(new LoginRequest("integration@emcore.com", "Password123!"), ct);
        Assert.False(oldLogin.IsSuccess);
        Assert.Equal(401, oldLogin.StatusCode);

        // 7. New password login succeeds
        var newLogin = await _service.LoginAsync(new LoginRequest("integration@emcore.com", "NewSecret456!"), ct);
        Assert.True(newLogin.IsSuccess);

        // 8. Logout All Sessions
        var logoutRes = await _service.LogoutAllAsync(userId, ct);
        Assert.True(logoutRes.IsSuccess);
    }

    [Fact]
    public async Task Forgot_Password_Returns_Safe_Generic_Response_For_Unknown_User()
    {
        var res = await _service.ForgotPasswordAsync(new ForgotPasswordRequest("nonexistent@emcore.com"), CancellationToken.None);
        Assert.True(res.IsSuccess);
        Assert.Contains("If an account with that identifier exists", res.Data?.Message);
    }
}
