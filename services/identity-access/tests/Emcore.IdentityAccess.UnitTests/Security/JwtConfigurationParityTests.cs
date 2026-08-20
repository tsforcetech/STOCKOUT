using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using Emcore.IdentityAccess.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Emcore.IdentityAccess.UnitTests.Security;

public class JwtConfigurationParityTests
{
    private static string GetTestPem()
    {
        using var rsa = RSA.Create(2048);
        return Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
    }

    [Fact]
    public void DevelopmentAndProduction_ShouldUseSameJwtGenerator()
    {
        var testPem = GetTestPem();
        var devConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ASPNETCORE_ENVIRONMENT", "Development" },
            { "Jwt:Enabled", "true" },
            { "Jwt:Issuer", "dev-issuer" },
            { "Jwt:Audience", "dev-aud" },
            { "Jwt:KeyId", "dev-kid" },
            { "Jwt:SigningKey", testPem },
            { "Jwt:AccessTokenLifetimeMinutes", "10" },
            { "Otp:HmacPepper", "dev-pepper" }
        }).Build();

        var prodConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ASPNETCORE_ENVIRONMENT", "Production" },
            { "Jwt:Enabled", "true" },
            { "Jwt:Issuer", "prod-issuer" },
            { "Jwt:Audience", "prod-aud" },
            { "Jwt:KeyId", "prod-kid" },
            { "Jwt:SigningKey", testPem },
            { "Jwt:AccessTokenLifetimeMinutes", "30" },
            { "Otp:HmacPepper", "prod-pepper" }
        }).Build();

        var devGen = new JwtTokenGenerator(devConfig);
        var prodGen = new JwtTokenGenerator(prodConfig);

        Assert.NotNull(devGen);
        Assert.NotNull(prodGen);
    }

    [Fact]
    public void JwtGeneration_ShouldReflectConfiguredValues()
    {
        var testPem = GetTestPem();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Jwt:Enabled", "true" },
            { "Jwt:Issuer", "my-issuer" },
            { "Jwt:Audience", "my-aud" },
            { "Jwt:KeyId", "my-kid" },
            { "Jwt:SigningKey", testPem },
            { "Jwt:AccessTokenLifetimeMinutes", "45" },
            { "Otp:HmacPepper", "pepper" }
        }).Build();

        var gen = new JwtTokenGenerator(config);
        var tokenStr = gen.GenerateAccessToken("user-123", "u@test.com", "sid-1", true);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenStr);

        Assert.Equal("my-issuer", token.Issuer);
        Assert.Equal("my-aud", token.Audiences.First());
        Assert.Equal("my-kid", token.Header.Kid);
        Assert.Equal("RS256", token.Header.Alg);

        var lifetime = token.ValidTo - token.ValidFrom;
        Assert.True(Math.Abs(lifetime.TotalMinutes - 45) < 2);
    }

    [Fact]
    public void MissingOtpPepper_ShouldFailStartup_RegardlessOfEnvironment()
    {
        var testPem = GetTestPem();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ASPNETCORE_ENVIRONMENT", "Development" },
            { "Jwt:Enabled", "true" },
            { "Jwt:Issuer", "my-issuer" },
            { "Jwt:Audience", "my-aud" },
            { "Jwt:KeyId", "my-kid" },
            { "Jwt:SigningKey", testPem },
            { "Jwt:AccessTokenLifetimeMinutes", "45" }
        }).Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new JwtTokenGenerator(config));
        Assert.Contains("Otp:HmacPepper is missing", ex.Message);
    }
}
