using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using Emcore.IdentityAccess.Api;
using Emcore.IdentityAccess.Application.DTOs;
using Emcore.IdentityAccess.Infrastructure.Persistence;
using Emcore.IdentityAccess.Infrastructure.Integrations;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Net.Http.Headers;

namespace Emcore.IdentityAccess.IntegrationTests.Security;

[Trait("Category", "ExternalSql")]
public class PasswordRecoverySqlSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _connectionString = "Server=148.66.157.41,5321;Database=EMCORE_IDENTITY_DB;User Id=sa;Password=Newpassword@1;Encrypt=False;TrustServerCertificate=True;";

    public PasswordRecoverySqlSecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:IdentityDatabase"] = _connectionString,
                    ["Database:Enabled"] = "true",
                    ["Email:Provider"] = "Smtp",
                    ["Email:Host"] = "test",
                    ["Email:FromAddress"] = "test@example.com",
                    ["Identity:MinimumPasswordLength"] = "12"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEmailSender));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton<IEmailSender, FakeEmailSender>();
            });
        });
    }

    private string GetTokenFromFakeSender(string email)
    {
        var emailSender = (FakeEmailSender)_factory.Services.GetRequiredService<IEmailSender>();
        var emails = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(emailSender.SentEmails, e => e.To == email));
        if (!System.Linq.Enumerable.Any(emails)) return "";
        var recoveryEmail = System.Linq.Enumerable.FirstOrDefault(emails, e => e.TextBody.Contains("Your password recovery token is:"));
        if (recoveryEmail.TextBody == null) return "";
        var lines = recoveryEmail.TextBody.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        var index = System.Array.FindIndex(lines, l => l.Contains("Your password recovery token is:"));
        if (index >= 0 && index + 1 < lines.Length) return lines[index + 1].Trim();
        return "";
    }

    private async Task<(string userId, string email, string otp)> RegisterUserAsync(bool verifyEmail = true, string password = "SecureP@ss123!")
    {
        var client = _factory.CreateClient();
        var email = $"rec_{Guid.NewGuid():N}@emcore.com";
        var req = new RegisterRequest(email, $"999{new Random().Next(1000000, 9999999)}", password);
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", req);
        res.EnsureSuccessStatusCode();
        var data = await res.Content.ReadFromJsonAsync<RegisterResponse>();

        await Task.Delay(50);
        var emailSender = (FakeEmailSender)_factory.Services.GetRequiredService<IEmailSender>();
        var emails = emailSender.SentEmails.Where(e => e.To == email).ToList();
        var match = System.Text.RegularExpressions.Regex.Match(emails.Last().TextBody, @"\b\d{6}\b");
        var otp = match.Success ? match.Value : "";

        if (verifyEmail)
        {
            var verifyReq = new ConfirmEmailVerificationRequest(email, otp);
            var verifyRes = await client.PostAsJsonAsync("/api/v1/auth/verification/email/confirm", verifyReq);
            verifyRes.EnsureSuccessStatusCode();
        }

        return (data!.UserId, email, otp);
    }

    [Fact]
    [Trait("Category", "ExternalSql")]
    public async Task PasswordRecovery_VerifiedEmail_ShouldSucceed()
    {
        var (userId, email, _) = await RegisterUserAsync(verifyEmail: true);
        var client = _factory.CreateClient();

        var req = new ForgotPasswordRequest(email);
        var res = await client.PostAsJsonAsync("/api/v1/auth/password/forgot", req);
        res.EnsureSuccessStatusCode();

        await Task.Delay(50);
        var token = GetTokenFromFakeSender(email);
        Assert.NotEmpty(token);

        using var conn = new SqlConnection(_connectionString);

        var recovery = await conn.QuerySingleOrDefaultAsync("SELECT * FROM ACCOUNT_RECOVERY WHERE UserId = @UserId", new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });
        Assert.NotNull(recovery);
        Assert.NotEqual(token, (string)recovery!.TokenHash);
    }

    [Fact]
    [Trait("Category", "ExternalSql")]
    public async Task PasswordRecovery_UnverifiedEmail_ShouldNotSendToken()
    {
        var (userId, email, _) = await RegisterUserAsync(verifyEmail: false);
        var client = _factory.CreateClient();

        var emailSender = (FakeEmailSender)_factory.Services.GetRequiredService<IEmailSender>();
        emailSender.SentEmails.Clear();

        var req = new ForgotPasswordRequest(email);
        var res = await client.PostAsJsonAsync("/api/v1/auth/password/forgot", req);
        res.EnsureSuccessStatusCode();

        await Task.Delay(50);
        var token = GetTokenFromFakeSender(email);
        Assert.Empty(token);

        using var conn = new SqlConnection(_connectionString);

        var recovery = await conn.QuerySingleOrDefaultAsync("SELECT * FROM ACCOUNT_RECOVERY WHERE UserId = @UserId", new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });
        Assert.Null(recovery);
    }

    [Fact]
    [Trait("Category", "ExternalSql")]
    public async Task PasswordRecovery_UnknownAccount_ShouldNotSendToken_GenericResponse()
    {
        var client = _factory.CreateClient();
        var email = $"unknown_{Guid.NewGuid():N}@emcore.com";

        var emailSender = (FakeEmailSender)_factory.Services.GetRequiredService<IEmailSender>();
        emailSender.SentEmails.Clear();

        var req = new ForgotPasswordRequest(email);
        var res = await client.PostAsJsonAsync("/api/v1/auth/password/forgot", req);
        res.EnsureSuccessStatusCode();

        await Task.Delay(50);
        var token = GetTokenFromFakeSender(email);
        Assert.Empty(token);
    }

    [Fact]
    [Trait("Category", "ExternalSql")]
    public async Task PasswordReset_TokenOnlyFlow_ShouldSucceed()
    {
        var (userId, email, _) = await RegisterUserAsync(verifyEmail: true);
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/password/forgot", new ForgotPasswordRequest(email));
        await Task.Delay(50);
        var token = GetTokenFromFakeSender(email);

        var resetReq = new ResetPasswordRequest(token, "NewP@ssw0rd123!", null); // Token only flow
        var resetRes = await client.PostAsJsonAsync("/api/v1/auth/password/reset", resetReq);
        resetRes.EnsureSuccessStatusCode();

        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "NewP@ssw0rd123!"));
        loginRes.EnsureSuccessStatusCode();

        using var conn = new SqlConnection(_connectionString);

        var events = await conn.QueryAsync(
            "SELECT * FROM IDENTITY_OUTBOX WHERE MessageType = 'identity.user.password-changed.v1' AND Payload LIKE '%' + @UserIdStr + '%' AND Payload LIKE '%' + @UserIdStr + '%'",
            new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });
        Assert.Single(events);
    }

    [Fact]
    [Trait("Category", "ExternalSql")]
    public async Task PasswordReset_WrongIdentifierAndValidToken_ShouldFail()
    {
        var (userA, emailA, _) = await RegisterUserAsync(verifyEmail: true);
        var (userB, emailB, _) = await RegisterUserAsync(verifyEmail: true);
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/password/forgot", new ForgotPasswordRequest(emailA));
        await Task.Delay(50);
        var tokenA = GetTokenFromFakeSender(emailA);

        var resetReq = new ResetPasswordRequest(tokenA, "NewP@ssw0rd123!", emailB); // User B's identifier with User A's token
        var resetRes = await client.PostAsJsonAsync("/api/v1/auth/password/reset", resetReq);
        Assert.False(resetRes.IsSuccessStatusCode);
    }

    [Fact]
    [Trait("Category", "ExternalSql")]
    public async Task PasswordReset_InvalidToken_ShouldFail()
    {
        var (userId, email, _) = await RegisterUserAsync(verifyEmail: true);
        var client = _factory.CreateClient();

        var resetReq = new ResetPasswordRequest("invalid_token_123", "NewP@ssw0rd123!", null);
        var resetRes = await client.PostAsJsonAsync("/api/v1/auth/password/reset", resetReq);
        Assert.False(resetRes.IsSuccessStatusCode);
    }

    [Fact]
    [Trait("Category", "ExternalSql")]
    public async Task PasswordReset_ExpiredToken_ShouldFail()
    {
        var (userId, email, _) = await RegisterUserAsync(verifyEmail: true);
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/password/forgot", new ForgotPasswordRequest(email));
        await Task.Delay(50);
        var token = GetTokenFromFakeSender(email);

        using var conn = new SqlConnection(_connectionString);

        await conn.ExecuteAsync("UPDATE ACCOUNT_RECOVERY SET ExpiresAtUtc = DATEADD(minute, -10, GETUTCDATE()) WHERE UserId = @UserId", new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });

        var resetReq = new ResetPasswordRequest(token, "NewP@ssw0rd123!", null);
        var resetRes = await client.PostAsJsonAsync("/api/v1/auth/password/reset", resetReq);
        Assert.False(resetRes.IsSuccessStatusCode);
    }

    [Fact]
    [Trait("Category", "ExternalSql")]
    public async Task PasswordReset_Replay_ShouldFail()
    {
        var (userId, email, _) = await RegisterUserAsync(verifyEmail: true);
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/password/forgot", new ForgotPasswordRequest(email));
        await Task.Delay(50);
        var token = GetTokenFromFakeSender(email);

        var resetReq = new ResetPasswordRequest(token, "NewP@ssw0rd123!", null);
        var resetRes1 = await client.PostAsJsonAsync("/api/v1/auth/password/reset", resetReq);
        resetRes1.EnsureSuccessStatusCode();

        var resetRes2 = await client.PostAsJsonAsync("/api/v1/auth/password/reset", resetReq);
        Assert.False(resetRes2.IsSuccessStatusCode);

        using var conn = new SqlConnection(_connectionString);

        var events = await conn.QueryAsync(
            "SELECT * FROM IDENTITY_OUTBOX WHERE MessageType = 'identity.user.password-changed.v1' AND Payload LIKE '%' + @UserIdStr + '%' AND Payload LIKE '%' + @UserIdStr + '%'",
            new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });
        Assert.Single(events);
    }

    [Fact]
    [Trait("Category", "ExternalSql")]
    public async Task PasswordReset_ConcurrentConsumption_ShouldYieldSingleSuccess()
    {
        var (userId, email, _) = await RegisterUserAsync(verifyEmail: true);
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        await client1.PostAsJsonAsync("/api/v1/auth/password/forgot", new ForgotPasswordRequest(email));
        await Task.Delay(50);
        var token = GetTokenFromFakeSender(email);

        var resetReq = new ResetPasswordRequest(token, "NewP@ssw0rd123!", null);
        var t1 = client1.PostAsJsonAsync("/api/v1/auth/password/reset", resetReq);
        var t2 = client2.PostAsJsonAsync("/api/v1/auth/password/reset", resetReq);

        var results = await Task.WhenAll(t1, t2);
        Assert.Equal(1, results.Count(r => r.IsSuccessStatusCode));
        Assert.Equal(1, results.Count(r => !r.IsSuccessStatusCode));
    }

    [Fact]
    [Trait("Category", "ExternalSql")]
    public async Task PasswordReset_SessionRevocation_ShouldRevokeAllUserSessions()
    {
        var (userId, email, _) = await RegisterUserAsync(verifyEmail: true, password: "SecureP@ss123!");
        var client = _factory.CreateClient();

        // Login to get a session
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "SecureP@ss123!"));
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();

        // Reset password
        await client.PostAsJsonAsync("/api/v1/auth/password/forgot", new ForgotPasswordRequest(email));
        await Task.Delay(50);
        var token = GetTokenFromFakeSender(email);

        var resetRes = await client.PostAsJsonAsync("/api/v1/auth/password/reset", new ResetPasswordRequest(token, "NewP@ssw0rd123!", null));
        resetRes.EnsureSuccessStatusCode();

        // Refresh token should be rejected
        var refreshRes = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(loginData!.RefreshToken));
        Assert.False(refreshRes.IsSuccessStatusCode);

        using var conn = new SqlConnection(_connectionString);

        var sessions = await conn.QueryAsync("SELECT * FROM USER_SESSION WHERE UserId = @UserId", new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });
        Assert.All(sessions, s => Assert.Equal("Revoked", (string)s!.Status));
    }

    [Fact]
    [Trait("Category", "ExternalSql")]
    public async Task PasswordReset_SessionRevocation_IsUserScoped()
    {
        var (userA, emailA, _) = await RegisterUserAsync(verifyEmail: true, password: "SecureP@ss123!");
        var (userB, emailB, _) = await RegisterUserAsync(verifyEmail: true, password: "SecureP@ss123!");
        var client = _factory.CreateClient();

        var loginResB = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(emailB, "SecureP@ss123!"));
        var loginDataB = await loginResB.Content.ReadFromJsonAsync<LoginResponse>();

        await client.PostAsJsonAsync("/api/v1/auth/password/forgot", new ForgotPasswordRequest(emailA));
        await Task.Delay(50);
        var tokenA = GetTokenFromFakeSender(emailA);

        var resetRes = await client.PostAsJsonAsync("/api/v1/auth/password/reset", new ResetPasswordRequest(tokenA, "NewP@ssw0rd123!", null));
        resetRes.EnsureSuccessStatusCode();

        // User B's session should still be active
        var refreshResB = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(loginDataB!.RefreshToken));
        refreshResB.EnsureSuccessStatusCode();
    }

    [Fact]
    [Trait("Category", "ExternalSql")]
    public async Task PasswordPolicy_WeakPassword_DoesNotConsumeToken()
    {
        var (userId, email, _) = await RegisterUserAsync(verifyEmail: true);
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/password/forgot", new ForgotPasswordRequest(email));
        await Task.Delay(50);
        var token = GetTokenFromFakeSender(email);

        var resetReq = new ResetPasswordRequest(token, "Weak123!", null); // 8 chars, min is 12
        var resetRes = await client.PostAsJsonAsync("/api/v1/auth/password/reset", resetReq);
        Assert.False(resetRes.IsSuccessStatusCode);

        // Token should still be active and usable
        var resetReqValid = new ResetPasswordRequest(token, "StrongP@ssw0rd123!", null);
        var resetResValid = await client.PostAsJsonAsync("/api/v1/auth/password/reset", resetReqValid);
        resetResValid.EnsureSuccessStatusCode();
    }

    [Fact]
    [Trait("Category", "ExternalSql")]
    public async Task PasswordPolicy_Registration()
    {
        var client = _factory.CreateClient();
        var email = $"reg_{Guid.NewGuid():N}@emcore.com";

        var reqWeak = new RegisterRequest(email, $"999{new Random().Next(1000000, 9999999)}", "Weak123!");
        var resWeak = await client.PostAsJsonAsync("/api/v1/auth/register", reqWeak);
        Assert.False(resWeak.IsSuccessStatusCode);

        var reqStrong = new RegisterRequest(email, $"999{new Random().Next(1000000, 9999999)}", "StrongP@ssw0rd123!");
        var resStrong = await client.PostAsJsonAsync("/api/v1/auth/register", reqStrong);
        resStrong.EnsureSuccessStatusCode();
    }

    [Fact]
    [Trait("Category", "ExternalSql")]
    public async Task PasswordPolicy_ChangePassword()
    {
        var (userId, email, _) = await RegisterUserAsync(verifyEmail: true, password: "SecureP@ss123!");
        var client = _factory.CreateClient();

        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "SecureP@ss123!"));
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginData!.AccessToken);

        var changeReqWeak = new ChangePasswordRequest("SecureP@ss123!", "Weak123!");
        var changeResWeak = await client.PostAsJsonAsync($"/api/v1/auth/password/change", changeReqWeak);
        Assert.False(changeResWeak.IsSuccessStatusCode);

        var changeReqStrong = new ChangePasswordRequest("SecureP@ss123!", "StrongP@ssw0rd123!");
        var changeResStrong = await client.PostAsJsonAsync($"/api/v1/auth/password/change", changeReqStrong);
        changeResStrong.EnsureSuccessStatusCode();
    }
}


