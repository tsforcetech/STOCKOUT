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

public class PasswordRecoverySecurityTests
{
    private readonly Mock<IIdentityRepository> _repoMock;
    private readonly Mock<ITokenGenerator> _tokenGenMock;
    private readonly Mock<IPasswordHasher> _hasherMock;
    private readonly Mock<IVerificationDeliveryService> _deliveryMock;
    private readonly IdentityOptions _options;
    private readonly IdentityApplicationService _sut;

    public PasswordRecoverySecurityTests()
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
    public async Task ForgotPassword_Fails_If_Email_Not_Verified()
    {
        var req = new ForgotPasswordRequest("test@example.com");
        var lookup = new UserLookupResult("user_id", "ulid", "Active", "test@example.com", "test@example.com", false, null, null, false, null, null, 0, null);
        _repoMock.Setup(r => r.GetUserByIdentifierAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lookup);
        
        var result = await _sut.ForgotPasswordAsync(req, CancellationToken.None);

        Assert.True(result.IsSuccess); // Anti-enum returns success
        _repoMock.Verify(r => r.CreateRecoveryRequestAsync(It.IsAny<PasswordRecovery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_LimitsTo_5_Sends_Per_15Minutes()
    {
        var req = new ForgotPasswordRequest("test@example.com");
        var lookup = new UserLookupResult("user_id", "ulid", "Active", "test@example.com", "test@example.com", true, null, null, false, null, null, 0, null);
        _repoMock.Setup(r => r.GetUserByIdentifierAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lookup);
        
        // Setup recent count >= 5
        _repoMock.Setup(r => r.GetRecentRecoveryCountAsync("user_id", TimeSpan.FromMinutes(15), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await _sut.ForgotPasswordAsync(req, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repoMock.Verify(r => r.CreateRecoveryRequestAsync(It.IsAny<PasswordRecovery>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
