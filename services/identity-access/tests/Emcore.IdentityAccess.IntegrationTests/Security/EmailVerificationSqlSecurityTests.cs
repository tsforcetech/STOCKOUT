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

namespace Emcore.IdentityAccess.IntegrationTests.Security;

public class EmailVerificationSqlSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _connectionString = "Server=148.66.157.41,5321;Database=EMCORE_IDENTITY_DB;User Id=sa;Password=Newpassword@1;Encrypt=False;TrustServerCertificate=True;";

    public EmailVerificationSqlSecurityTests(WebApplicationFactory<Program> factory)
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
                    ["Email:FromAddress"] = "test@example.com"
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

    private string GetOtpFromFakeSender(string email)
    {
        var emailSender = (FakeEmailSender)_factory.Services.GetRequiredService<IEmailSender>();
        var emails = emailSender.SentEmails.Where(e => e.To == email).ToList();
        var match = System.Text.RegularExpressions.Regex.Match(emails.Last().TextBody, @"\b\d{6}\b");
        return match.Success ? match.Value : "";
    }

    private async Task<(string userId, string email, string otp)> RegisterAndGetOtpAsync()
    {
        var client = _factory.CreateClient();
        var email = $"verify_{Guid.NewGuid():N}@emcore.com";
        var req = new RegisterRequest(email, $"999{new Random().Next(1000000, 9999999)}", "SecureP@ss1!");
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", req);
        res.EnsureSuccessStatusCode();
        var data = await res.Content.ReadFromJsonAsync<RegisterResponse>();

        // Wait a small moment to ensure email is captured (if async)
        await Task.Delay(50);
        return (data!.UserId, email, GetOtpFromFakeSender(email));
    }

    [Fact]
    public async Task EmailVerification_WrongOtp_ShouldFail()
    {
        var (userId, email, otp) = await RegisterAndGetOtpAsync();
        var client = _factory.CreateClient();

        var verifyReq = new ConfirmEmailVerificationRequest(email, "000000"); // Wrong
        var verifyRes = await client.PostAsJsonAsync("/api/v1/auth/verification/email/confirm", verifyReq);
        Assert.False(verifyRes.IsSuccessStatusCode);

        using var conn = new SqlConnection(_connectionString);

        var verification = await conn.QuerySingleOrDefaultAsync(
            "SELECT TOP 1 * FROM ACCOUNT_VERIFICATION WHERE UserId = @UserId ORDER BY CreatedAtUtc DESC",
            new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });

        Assert.NotNull(verification);
        Assert.Equal(1, verification!.AttemptCount);

        var user = await conn.QuerySingleOrDefaultAsync(
            "SELECT e.IsVerified FROM USER_ACCOUNT u JOIN USER_EMAIL e ON u.Id = e.UserId WHERE u.Id = @UserId",
            new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });
        Assert.False((bool)user!.IsVerified);
    }

    [Fact]
    public async Task EmailVerification_FiveAttemptsExhaustion_ShouldFail()
    {
        var (userId, email, otp) = await RegisterAndGetOtpAsync();
        var client = _factory.CreateClient();

        // 5 wrong attempts
        for (int i = 0; i < 5; i++)
        {
            var verifyReq = new ConfirmEmailVerificationRequest(email, "000000");
            await client.PostAsJsonAsync("/api/v1/auth/verification/email/confirm", verifyReq);
        }

        // 6th attempt with correct OTP
        var correctReq = new ConfirmEmailVerificationRequest(email, otp);
        var correctRes = await client.PostAsJsonAsync("/api/v1/auth/verification/email/confirm", correctReq);
        Assert.False(correctRes.IsSuccessStatusCode);

        using var conn = new SqlConnection(_connectionString);

        var verification = await conn.QuerySingleOrDefaultAsync(
            "SELECT TOP 1 * FROM ACCOUNT_VERIFICATION WHERE UserId = @UserId ORDER BY CreatedAtUtc DESC",
            new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });

        Assert.NotNull(verification);
        Assert.Equal(5, verification!.AttemptCount); // It stops at 5 according to logic usually or it's 6, just assert it's >= 5
    }

    [Fact]
    public async Task EmailVerification_ValidSuccess_ShouldPass()
    {
        var (userId, email, otp) = await RegisterAndGetOtpAsync();
        var client = _factory.CreateClient();

        var correctReq = new ConfirmEmailVerificationRequest(email, otp);
        var correctRes = await client.PostAsJsonAsync("/api/v1/auth/verification/email/confirm", correctReq);
        correctRes.EnsureSuccessStatusCode();

        using var conn = new SqlConnection(_connectionString);

        var user = await conn.QuerySingleOrDefaultAsync(
            "SELECT u.Status AS UserStatus, e.IsVerified FROM USER_ACCOUNT u JOIN USER_EMAIL e ON u.Id = e.UserId WHERE u.Id = @UserId",
            new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });
        Assert.True((bool)user!.IsVerified);
        Assert.Equal("Active", user!.UserStatus);

        var verification = await conn.QuerySingleOrDefaultAsync(
            "SELECT Status AS UserStatus FROM ACCOUNT_VERIFICATION WHERE UserId = @UserId",
            new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });
        Assert.Equal("Verified", verification!.UserStatus);

        // Check Outbox
        var events = await conn.QueryAsync(
            "SELECT * FROM IDENTITY_OUTBOX WHERE MessageType = 'identity.user.email-verified.v1' AND Payload LIKE '%' + @UserIdStr + '%'",
            new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });
        Assert.Single(events);
    }

    [Fact]
    public async Task EmailVerification_Replay_ShouldFail()
    {
        var (userId, email, otp) = await RegisterAndGetOtpAsync();
        var client = _factory.CreateClient();

        var correctReq = new ConfirmEmailVerificationRequest(email, otp);
        var correctRes1 = await client.PostAsJsonAsync("/api/v1/auth/verification/email/confirm", correctReq);
        correctRes1.EnsureSuccessStatusCode();

        var correctRes2 = await client.PostAsJsonAsync("/api/v1/auth/verification/email/confirm", correctReq);
        Assert.False(correctRes2.IsSuccessStatusCode);

        using var conn = new SqlConnection(_connectionString);

        var events = await conn.QueryAsync(
            "SELECT * FROM IDENTITY_OUTBOX WHERE MessageType = 'identity.user.email-verified.v1' AND Payload LIKE '%' + @UserIdStr + '%'",
            new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });
        Assert.Single(events);
    }

    [Fact]
    public async Task EmailVerification_ExpiredOtp_ShouldFail()
    {
        var (userId, email, otp) = await RegisterAndGetOtpAsync();
        var client = _factory.CreateClient();

        using var conn = new SqlConnection(_connectionString);

        await conn.ExecuteAsync("UPDATE ACCOUNT_VERIFICATION SET ExpiresAtUtc = DATEADD(minute, -10, GETUTCDATE()) WHERE UserId = @UserId", new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });

        var correctReq = new ConfirmEmailVerificationRequest(email, otp);
        var correctRes = await client.PostAsJsonAsync("/api/v1/auth/verification/email/confirm", correctReq);
        Assert.False(correctRes.IsSuccessStatusCode);
    }

    [Fact]
    public async Task EmailVerification_CancelledOldOtp_ShouldFail()
    {
        var (userId, email, otp1) = await RegisterAndGetOtpAsync();
        var client = _factory.CreateClient();

        // Expire the created at so we can resend without rate limit
        using var conn = new SqlConnection(_connectionString);

        await conn.ExecuteAsync("UPDATE ACCOUNT_VERIFICATION SET CreatedAtUtc = DATEADD(minute, -5, GETUTCDATE()) WHERE UserId = @UserId", new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });

        var resendReq = new SendEmailVerificationRequest(email);
        var resendRes = await client.PostAsJsonAsync("/api/v1/auth/verification/email/send", resendReq);
        resendRes.EnsureSuccessStatusCode();

        await Task.Delay(50);
        var otp2 = GetOtpFromFakeSender(email);

        // Try OTP1
        var req1 = new ConfirmEmailVerificationRequest(email, otp1);
        var res1 = await client.PostAsJsonAsync("/api/v1/auth/verification/email/confirm", req1);
        Assert.False(res1.IsSuccessStatusCode);

        // Try OTP2
        var req2 = new ConfirmEmailVerificationRequest(email, otp2);
        var res2 = await client.PostAsJsonAsync("/api/v1/auth/verification/email/confirm", req2);
        res2.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task EmailVerification_ConcurrentConsumption_ShouldYieldSingleSuccess()
    {
        var (userId, email, otp) = await RegisterAndGetOtpAsync();
        var client1 = _factory.CreateClient();
        var client2 = _factory.CreateClient();

        var req = new ConfirmEmailVerificationRequest(email, otp);

        var t1 = client1.PostAsJsonAsync("/api/v1/auth/verification/email/confirm", req);
        var t2 = client2.PostAsJsonAsync("/api/v1/auth/verification/email/confirm", req);

        var results = await Task.WhenAll(t1, t2);
        int successCount = results.Count(r => r.IsSuccessStatusCode);
        int failCount = results.Count(r => !r.IsSuccessStatusCode);

        Assert.Equal(1, successCount);
        Assert.Equal(1, failCount);
    }

    [Fact]
    public async Task ignore_test()
    {
        // Handled inherently by the above tests (wrong otp, 5 attempts, success, expired, etc.)
        // But let's explicitly run the repository method to see result mapping
        var (userId, email, otp) = await RegisterAndGetOtpAsync();
        var repo = _factory.Services.GetRequiredService<Emcore.IdentityAccess.Application.Abstractions.IIdentityRepository>();

        var wrongHash = "some_wrong_hash";
        var resultFail = await repo.VerifyAccountAsync(userId, "Email", wrongHash, null, CancellationToken.None);
        Assert.Null(resultFail); // Fails and returns null

        var correctTokenGenerator = _factory.Services.GetRequiredService<Emcore.IdentityAccess.Application.Abstractions.ITokenGenerator>();
        var correctHash = correctTokenGenerator.HashToken(otp);
        var resultSuccess = await repo.VerifyAccountAsync(userId, "Email", correctHash, "outbox_payload", CancellationToken.None);
        Assert.NotNull(resultSuccess); // Success returns new Result()
    }

    [Fact]
    public async Task CanonicalRegistrationUserId_SqlTest()
    {
        var (userId, email, otp) = await RegisterAndGetOtpAsync();

        using var conn = new SqlConnection(_connectionString);

        var userAcc = await conn.QuerySingleOrDefaultAsync("SELECT Id FROM USER_ACCOUNT WHERE Id = @UserId", new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });
        var userEmail = await conn.QuerySingleOrDefaultAsync("SELECT UserId FROM USER_EMAIL WHERE UserId = @UserId", new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });
        var accVer = await conn.QuerySingleOrDefaultAsync("SELECT UserId FROM ACCOUNT_VERIFICATION WHERE UserId = @UserId", new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });
        var userCred = await conn.QuerySingleOrDefaultAsync("SELECT UserId FROM USER_CREDENTIAL WHERE UserId = @UserId", new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });

        Assert.NotNull(userAcc);
        Assert.NotNull(userEmail);
        Assert.NotNull(accVer);
        Assert.NotNull(userCred);

        var outbox = await conn.QuerySingleOrDefaultAsync("SELECT Payload FROM IDENTITY_OUTBOX WHERE MessageType = 'identity.user.registered.v1' AND Payload LIKE '%' + @UserIdStr + '%'", new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });
        Assert.NotNull(outbox);
        Assert.Contains(userId, (string)outbox!.Payload);
    }

    [Fact]
    public async Task VerificationOtpStorageTest()
    {
        var (userId, email, otp) = await RegisterAndGetOtpAsync();

        using var conn = new SqlConnection(_connectionString);

        var ver = await conn.QuerySingleOrDefaultAsync("SELECT TokenHash FROM ACCOUNT_VERIFICATION WHERE UserId = @UserId AND Status = 'Issued'", new { UserId = Guid.Parse(userId), UserIdStr = Guid.Parse(userId).ToString("N") });

        Assert.NotNull(ver);
        string storedHash = ver!.TokenHash;
        Assert.NotEqual(otp, storedHash);

        var tokenGenerator = _factory.Services.GetRequiredService<Emcore.IdentityAccess.Application.Abstractions.ITokenGenerator>();
        var generatedHash = tokenGenerator.HashToken(otp);
        Assert.Equal(generatedHash, storedHash);
    }
}


