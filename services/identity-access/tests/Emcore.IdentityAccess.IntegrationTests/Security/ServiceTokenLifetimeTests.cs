using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Emcore.IdentityAccess.Infrastructure.Persistence;
using Emcore.IdentityAccess.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Emcore.IdentityAccess.IntegrationTests.Security;

public class ServiceTokenLifetimeTests
{
    private static string GetTestPem()
    {
        using var rsa = RSA.Create(2048);
        return Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
    }

    [Fact]
    public async Task IssueServiceToken_UsesConfiguredLifetime()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:IdentityDatabase"] = "inmemory-servicetoken-test-db",
                ["Jwt:AccessTokenLifetimeMinutes"] = "20",
                ["Jwt:Enabled"] = "true",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-aud",
                ["Jwt:KeyId"] = "test-kid",
                ["Jwt:SigningKey"] = GetTestPem(),
                ["Otp:HmacPepper"] = "test-pepper"
            })
            .Build();

        var repo = new IdentityRepository(config);
        var hasher = new Pbkdf2PasswordHasher();
        var tokenGen = new JwtTokenGenerator(config);
        var service = new IdentityApplicationService(repo, tokenGen, hasher, new Emcore.IdentityAccess.Application.Configuration.IdentityOptions(), null);

        var ct = CancellationToken.None;

        // Setup service client
        var regRes = await service.RegisterServiceClientAsync(new RegisterServiceClientRequest("My Service", ["orders:read"]), ct);
        Assert.True(regRes.IsSuccess);

        var clientId = regRes.Data!.ClientId;
        var clientSecret = regRes.Data!.ClientSecret;

        var before = DateTime.UtcNow;

        // Request service token
        var tokenRes = await service.IssueServiceTokenAsync(new ServiceTokenRequest(clientId, clientSecret, "orders:read"), ct);

        var after = DateTime.UtcNow;

        Assert.True(tokenRes.IsSuccess);
        Assert.NotNull(tokenRes.Data);

        // Assert ServiceTokenResponse.ExpiresIn is exactly 1200 seconds (20 minutes)
        Assert.Equal(1200, tokenRes.Data.ExpiresIn);

        // Assert actual JWT expiry is ~20 minutes
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenRes.Data.AccessToken);

        Assert.True(jwt.ValidTo >= before.AddMinutes(19), "JWT ValidTo should be at least 19 minutes in the future.");
        Assert.True(jwt.ValidTo <= after.AddMinutes(21), "JWT ValidTo should be at most 21 minutes in the future.");
    }
}
