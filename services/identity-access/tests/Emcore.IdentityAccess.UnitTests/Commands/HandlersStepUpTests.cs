using System;
using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application.Abstractions;
using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Emcore.IdentityAccess.Domain.Entities;
using Moq;
using Xunit;
using Emcore.BuildingBlocks.Core;

namespace Emcore.IdentityAccess.UnitTests.Commands;

public class HandlersStepUpTests
{
    private readonly Mock<IIdentityRepository> _mockRepo;
    private readonly Mock<ITokenGenerator> _mockGenerator;
    private readonly Mock<IPasswordHasher> _mockHasher;
    private readonly Mock<IVerificationDeliveryService> _mockDelivery;
    private readonly IdentityApplicationService _service;

    public HandlersStepUpTests()
    {
        _mockRepo = new Mock<IIdentityRepository>();
        _mockGenerator = new Mock<ITokenGenerator>();
        _mockHasher = new Mock<IPasswordHasher>();
        _mockDelivery = new Mock<IVerificationDeliveryService>();
        
        _service = new IdentityApplicationService(
            _mockRepo.Object,
            _mockGenerator.Object,
            _mockHasher.Object,
            _mockDelivery.Object);
    }

    [Fact]
    public async Task InitiateStepUpAsync_ShouldNotReturnPlaintextOtp()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString("N");
        var sessionId = Guid.NewGuid().ToString("N");
        var req = new InitiateStepUpRequest("DisableMfa");

        _mockGenerator.Setup(x => x.GenerateVerificationToken())
            .Returns(("123456", "hashed_123456"));
            
        var userLookup = new Emcore.IdentityAccess.Application.Abstractions.UserLookupResult(
            Id: userId,
            UlidId: "test_ulid",
            Status: "Active",
            Email: "test@example.com",
            NormalizedEmail: "TEST@EXAMPLE.COM",
            EmailVerified: true,
            Mobile: null,
            NormalizedMobile: null,
            MobileVerified: false,
            PasswordHash: null,
            HashAlgorithm: null,
            FailedCount: 0,
            LockoutEndUtc: null,
            SecurityVersion: 1,
            MfaEnabled: false
        );
        
        _mockRepo.Setup(x => x.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Emcore.BuildingBlocks.Core.Result<Emcore.IdentityAccess.Application.Abstractions.UserLookupResult>.Success(userLookup));

        // Act
        var result = await _service.InitiateStepUpAsync(userId, sessionId, req, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(300, result.Data.ExpiresInSeconds);
        
        _mockRepo.Verify(x => x.CreateStepUpChallengeAsync(It.Is<StepUpChallenge>(c => c.SessionId == sessionId && c.TargetAction == "DisableMfa" && c.TokenHash == "hashed_123456"), It.IsAny<CancellationToken>()), Times.Once);
        _mockDelivery.Verify(x => x.SendVerificationOtpAsync("test@example.com", "StepUp", "123456", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyStepUpAsync_ShouldGenerateCryptographicProof()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString("N");
        var sessionId = Guid.NewGuid().ToString("N");
        var stepUpId = Guid.NewGuid().ToString("N");
        var req = new VerifyStepUpRequest(stepUpId, "123456");

        var challenge = new StepUpChallenge
        {
            Id = stepUpId,
            UserId = userId,
            SessionId = sessionId,
            TargetAction = "ChangeEmail",
            TokenHash = "hashed_123456",
            Status = "Issued",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
        };

        _mockRepo.Setup(x => x.GetStepUpChallengeAsync(stepUpId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        _mockGenerator.Setup(x => x.HashToken(It.IsAny<string>()))
            .Returns((string s) => s == "123456" ? "hashed_123456" : "proof_hash");
        
        // Mock atomic consumption success
        _mockRepo.Setup(x => x.ConsumeStepUpChallengeAsync(stepUpId, userId, "ChangeEmail", "hashed_123456", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result());

        // Act
        var result = await _service.VerifyStepUpAsync(userId, sessionId, req, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.VerificationToken));
        Assert.DoesNotContain("STEPUP_OK", result.Data.VerificationToken);
        
        _mockRepo.Verify(x => x.CreateStepUpProofAsync(It.Is<StepUpProof>(p => p.UserId == userId && p.SessionId == sessionId && p.TargetAction == "ChangeEmail" && p.ProofHash == "proof_hash"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
