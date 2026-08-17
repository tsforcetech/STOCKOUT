using System;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Emcore.IdentityAccess.IntegrationTests.Security;

public class StepUpIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public StepUpIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["ConnectionStrings:IdentityDatabase"] = "inmemory-stepup-api-test-db",
                    ["Database:Enabled"] = "true",
                    ["Email:Provider"] = "Smtp",
                    ["Email:Host"] = "test",
                    ["Email:FromAddress"] = "test@example.com"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Emcore.IdentityAccess.Infrastructure.Integrations.IEmailSender));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton<Emcore.IdentityAccess.Infrastructure.Integrations.IEmailSender, FakeEmailSender>();
            });
        });
    }

    [Fact]
    public async Task StepUp_CorrectOtp_ShouldGenerateProof()
    {
        var client = _factory.CreateClient();
        
        // 1. Register a user
        var regReq = new RegisterRequest($"stepup_{Guid.NewGuid():N}@emcore.com", "9990001111", "P@ssw0rd123!");
        var regRes = await client.PostAsJsonAsync("/api/v1/auth/register", regReq);
        regRes.EnsureSuccessStatusCode();
        var regData = await regRes.Content.ReadFromJsonAsync<RegisterResponse>();
        
        // 2. Login
        var loginReq = new LoginRequest(regReq.Email, regReq.Password);
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        loginRes.EnsureSuccessStatusCode();
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        var token = loginData!.AccessToken;
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var userId = jwt.Claims.First(c => c.Type == "sub").Value;
        var sessionId = jwt.Claims.First(c => c.Type == "sid").Value;
        client.DefaultRequestHeaders.Add("X-User-Id", userId);
        client.DefaultRequestHeaders.Add("X-Session-Id", sessionId);

        // 3. Initiate StepUp
        var emailSender = (FakeEmailSender)_factory.Services.GetRequiredService<Emcore.IdentityAccess.Infrastructure.Integrations.IEmailSender>();
        emailSender.SentEmails.Clear();

        var initReq = new InitiateStepUpRequest("SensitiveAction");
        var initRes = await client.PostAsJsonAsync("/api/v1/auth/stepup/initiate", initReq);
        initRes.EnsureSuccessStatusCode();
        var initData = await initRes.Content.ReadFromJsonAsync<InitiateStepUpResponse>();
        var stepUpId = initData!.StepUpId;
        
        // 4. Retrieve OTP from fake email sender
        var emails = emailSender.SentEmails.Where(e => e.To == regReq.Email).ToList();
        Assert.NotEmpty(emails);
        string otp = System.Text.RegularExpressions.Regex.Match(emails.Last().TextBody, @"\b\d{6}\b").Value;
        Assert.False(string.IsNullOrEmpty(otp), "OTP could not be parsed from email body.");
        
        var verReq = new VerifyStepUpRequest(stepUpId, otp);
        var verRes = await client.PostAsJsonAsync("/api/v1/auth/stepup/verify", verReq);
        if (!verRes.IsSuccessStatusCode)
        {
            var body = await verRes.Content.ReadAsStringAsync();
            Assert.Fail($"DEBUG API ERROR: StatusCode={verRes.StatusCode}, Body={body}");
        }
        verRes.EnsureSuccessStatusCode();
        var verData = await verRes.Content.ReadFromJsonAsync<VerifyStepUpResponse>();
        
        Assert.NotNull(verData?.VerificationToken);
        Assert.DoesNotContain("STEPUP_OK", verData!.VerificationToken);
    }
    
    [Fact]
    public async Task StepUp_WrongOtp_ShouldFail_And_LockAfterMaxAttempts()
    {
        var client = _factory.CreateClient();
        
        var regReq = new RegisterRequest($"stepup_{Guid.NewGuid():N}@emcore.com", "9990001112", "P@ssw0rd123!");
        await client.PostAsJsonAsync("/api/v1/auth/register", regReq);
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(regReq.Email, regReq.Password));
        var token = (await loginRes.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var userId = jwt.Claims.First(c => c.Type == "sub").Value;
        var sessionId = jwt.Claims.First(c => c.Type == "sid").Value;
        client.DefaultRequestHeaders.Add("X-User-Id", userId);
        client.DefaultRequestHeaders.Add("X-Session-Id", sessionId);

        var initRes = await client.PostAsJsonAsync("/api/v1/auth/stepup/initiate", new InitiateStepUpRequest("SensitiveAction"));
        var stepUpId = (await initRes.Content.ReadFromJsonAsync<InitiateStepUpResponse>())!.StepUpId;
        
        // Try wrong OTP 5 times
        for (int i = 0; i < 5; i++)
        {
            var verRes = await client.PostAsJsonAsync("/api/v1/auth/stepup/verify", new VerifyStepUpRequest(stepUpId, "000000"));
            Assert.False(verRes.IsSuccessStatusCode);
        }
        
        // Try correct OTP, should still fail because it's locked
        var emailSender = (FakeEmailSender)_factory.Services.GetRequiredService<Emcore.IdentityAccess.Infrastructure.Integrations.IEmailSender>();
        var emails = emailSender.SentEmails.Where(e => e.To == regReq.Email).ToList();
        Assert.NotEmpty(emails);
        string otp = System.Text.RegularExpressions.Regex.Match(emails.Last().TextBody, @"\b\d{6}\b").Value;
        
        var verRes2 = await client.PostAsJsonAsync("/api/v1/auth/stepup/verify", new VerifyStepUpRequest(stepUpId, otp));
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, verRes2.StatusCode);
    }
}
