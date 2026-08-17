using System;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Domain.Entities;
using Emcore.IdentityAccess.Application.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Emcore.IdentityAccess.Api;

namespace Emcore.IdentityAccess.IntegrationTests;

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
                    ["ConnectionStrings:IdentityDatabase"] = "inmemory-stepup-tests"
                });
            });
        });
    }

    [Fact]
    public async Task Proof_Successful_Consumption_And_Reuse_Rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IIdentityRepository>();
        var validator = scope.ServiceProvider.GetRequiredService<IStepUpProofValidator>();
        var generator = scope.ServiceProvider.GetRequiredService<ITokenGenerator>();

        string userId = Guid.NewGuid().ToString("N");
        string sessionId = Guid.NewGuid().ToString("N");
        string targetAction = "DisableMfa";
        string proofToken = "secret_proof_123";
        string proofHash = generator.HashToken(proofToken);

        var proof = new StepUpProof
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            SessionId = sessionId,
            TargetAction = targetAction,
            ProofHash = proofHash,
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            Status = "Issued"
        };
        await db.CreateStepUpProofAsync(proof, CancellationToken.None);

        // Consume once successfully
        var res1 = await validator.ValidateAndConsumeStepUpProofAsync(userId, sessionId, targetAction, proofToken, CancellationToken.None);
        Assert.True(res1.IsSuccess);

        // Consume exact same proof again should fail
        var res2 = await validator.ValidateAndConsumeStepUpProofAsync(userId, sessionId, targetAction, proofToken, CancellationToken.None);
        Assert.False(res2.IsSuccess);
    }

    [Fact]
    public async Task Proof_With_Wrong_Session_Should_Fail()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IIdentityRepository>();
        var validator = scope.ServiceProvider.GetRequiredService<IStepUpProofValidator>();
        var generator = scope.ServiceProvider.GetRequiredService<ITokenGenerator>();

        string userId = Guid.NewGuid().ToString("N");
        string sessionId = Guid.NewGuid().ToString("N");
        string targetAction = "ChangeEmail";
        string proofToken = "secret_proof_456";
        string proofHash = generator.HashToken(proofToken);

        var proof = new StepUpProof
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            SessionId = sessionId,
            TargetAction = targetAction,
            ProofHash = proofHash,
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            Status = "Issued"
        };
        await db.CreateStepUpProofAsync(proof, CancellationToken.None);

        var res = await validator.ValidateAndConsumeStepUpProofAsync(userId, "wrong_session", targetAction, proofToken, CancellationToken.None);
        Assert.False(res.IsSuccess);
    }

    [Fact]
    public async Task Expired_Proof_Should_Fail()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IIdentityRepository>();
        var validator = scope.ServiceProvider.GetRequiredService<IStepUpProofValidator>();
        var generator = scope.ServiceProvider.GetRequiredService<ITokenGenerator>();

        string userId = Guid.NewGuid().ToString("N");
        string sessionId = Guid.NewGuid().ToString("N");
        string targetAction = "ChangeEmail";
        string proofToken = "secret_proof_789";
        string proofHash = generator.HashToken(proofToken);

        var proof = new StepUpProof
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            SessionId = sessionId,
            TargetAction = targetAction,
            ProofHash = proofHash,
            IssuedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5),
            Status = "Issued"
        };
        await db.CreateStepUpProofAsync(proof, CancellationToken.None);

        var res = await validator.ValidateAndConsumeStepUpProofAsync(userId, sessionId, targetAction, proofToken, CancellationToken.None);
        Assert.False(res.IsSuccess);
    }

    [Fact]
    public async Task Purpose_Isolation_Mfa_Cannot_Be_Consumed_As_StepUp()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IIdentityRepository>();

        string userId = Guid.NewGuid().ToString("N");
        string challengeId = Guid.NewGuid().ToString("N");
        
        var mfaChallenge = new StepUpChallenge
        {
            Id = challengeId,
            UserId = userId,
            SessionId = null,
            TargetAction = "MfaLogin",
            TokenHash = "hash123",
            Status = "Issued",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            CreatedAtUtc = DateTime.UtcNow
        };
        await db.CreateStepUpChallengeAsync(mfaChallenge, CancellationToken.None);

        var consumeRes = await db.ConsumeStepUpChallengeAsync(challengeId, userId, null, "DisableMfa", "hash123", 5, CancellationToken.None);
        Assert.Null(consumeRes);
    }

    [Fact]
    public async Task Concurrent_Proof_Consumption_Should_Yield_Single_Success()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IIdentityRepository>();
        var validator = scope.ServiceProvider.GetRequiredService<IStepUpProofValidator>();
        var generator = scope.ServiceProvider.GetRequiredService<ITokenGenerator>();

        string userId = Guid.NewGuid().ToString("N");
        string sessionId = Guid.NewGuid().ToString("N");
        string targetAction = "DisableMfa";
        string proofToken = Guid.NewGuid().ToString();
        string proofHash = generator.HashToken(proofToken);

        var proof = new StepUpProof
        {
            Id = Guid.NewGuid().ToString("N"),
            UserId = userId,
            SessionId = sessionId,
            TargetAction = targetAction,
            ProofHash = proofHash,
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            Status = "Issued"
        };
        await db.CreateStepUpProofAsync(proof, CancellationToken.None);

        var t1 = validator.ValidateAndConsumeStepUpProofAsync(userId, sessionId, targetAction, proofToken, CancellationToken.None);
        var t2 = validator.ValidateAndConsumeStepUpProofAsync(userId, sessionId, targetAction, proofToken, CancellationToken.None);

        var res1 = await t1;
        var res2 = await t2;

        var successCount = (res1.IsSuccess ? 1 : 0) + (res2.IsSuccess ? 1 : 0);
        Assert.Equal(1, successCount);
    }
}
