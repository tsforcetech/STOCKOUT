using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Emcore.IdentityAccess.Application.Commands;
using Emcore.IdentityAccess.Application.DTOs;
using Emcore.IdentityAccess.Application.Abstractions;
using Emcore.IdentityAccess.Application.Configuration;
using Emcore.IdentityAccess.Domain.Entities;
using Emcore.BuildingBlocks.Core;

namespace Emcore.IdentityAccess.UnitTests;

public class EmailVerificationSecurityTests
{
    private readonly Mock<IIdentityRepository> _repoMock;
    private readonly Mock<ITokenGenerator> _tokenGenMock;
    private readonly Mock<IPasswordHasher> _hasherMock;
    private readonly Mock<IVerificationDeliveryService> _deliveryMock;
    private readonly IdentityOptions _options;
    private readonly IdentityApplicationService _sut;

    public EmailVerificationSecurityTests()
    {
        _repoMock = new Mock<IIdentityRepository>();
        _tokenGenMock = new Mock<ITokenGenerator>();
        _hasherMock = new Mock<IPasswordHasher>();
        _deliveryMock = new Mock<IVerificationDeliveryService>();
        _options = new IdentityOptions();
        _sut = new IdentityApplicationService(
            _repoMock.Object, _tokenGenMock.Object, _hasherMock.Object, _options, _deliveryMock.Object);
    }

    [Fact]
    public async Task SendEmailVerification_LimitsTo_5_Sends_Per_15Minutes()
    {
        var req = new SendEmailVerificationRequest("test@example.com");
        var lookup = new UserLookupResult("user_id", "ulid", "PendingVerification", "test@example.com", "test@example.com", false, null, null, false, null, null, 0, null);
        _repoMock.Setup(r => r.GetUserByIdentifierAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lookup);
        
        // Setup recent count >= 5
        _repoMock.Setup(r => r.GetRecentVerificationCountAsync("user_id", "Email", TimeSpan.FromMinutes(15), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await _sut.SendEmailVerificationAsync(req, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Expect enumerate shield
        _repoMock.Verify(r => r.CreateVerificationAsync(It.IsAny<AccountVerification>(), It.IsAny<CancellationToken>()), Times.Never);
        _deliveryMock.Verify(d => d.SendVerificationOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendEmailVerification_Enforces_60_Second_Cooldown()
    {
        var req = new SendEmailVerificationRequest("test@example.com");
        var lookup = new UserLookupResult("user_id", "ulid", "PendingVerification", "test@example.com", "test@example.com", false, null, null, false, null, null, 0, null);
        _repoMock.Setup(r => r.GetUserByIdentifierAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lookup);
        
        _repoMock.Setup(r => r.GetRecentVerificationCountAsync("user_id", "Email", TimeSpan.FromMinutes(15), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Setup latest verification < 60 seconds ago
        _repoMock.Setup(r => r.GetLatestVerificationAsync("user_id", "Email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccountVerification { CreatedAtUtc = DateTime.UtcNow.AddSeconds(-30) });

        var result = await _sut.SendEmailVerificationAsync(req, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Expect enumerate shield
        _repoMock.Verify(r => r.CreateVerificationAsync(It.IsAny<AccountVerification>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
