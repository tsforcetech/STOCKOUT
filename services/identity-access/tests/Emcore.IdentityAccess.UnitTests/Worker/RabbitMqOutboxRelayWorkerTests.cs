using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Emcore.IdentityAccess.UnitTests.Worker;

public class RabbitMqOutboxRelayWorkerTests
{
    private readonly Mock<ILogger<RabbitMqOutboxRelayWorker>> _loggerMock;
    private readonly Mock<IIntegrationEventPublisher> _publisherMock;
    private readonly Mock<IOutboxRepository> _repositoryMock;
    private readonly RabbitMqOutboxRelayWorker _worker;

    public RabbitMqOutboxRelayWorkerTests()
    {
        _loggerMock = new Mock<ILogger<RabbitMqOutboxRelayWorker>>();
        _publisherMock = new Mock<IIntegrationEventPublisher>();
        _repositoryMock = new Mock<IOutboxRepository>();

        var inMemorySettings = new Dictionary<string, string?> {
            {"Outbox:Enabled", "true"},
            {"RabbitMq:Enabled", "true"},
            {"Outbox:PollingIntervalSeconds", "1"},
            {"Outbox:BatchSize", "10"},
            {"Outbox:MaxPublishAttempts", "3"},
            {"ConnectionStrings:IdentityDatabase", "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;"}
        };

        IConfiguration configRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _worker = new RabbitMqOutboxRelayWorker(configRoot, _loggerMock.Object, _publisherMock.Object, _repositoryMock.Object);
    }

    [Fact]
    public async Task Worker_ShouldPublishAndMarkPublished_WhenSuccessful()
    {
        // Arrange
        var msgId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };
        var row = new OutboxRow { Id = msgId, MessageType = "test.event", Payload = "{}", RowVersion = rowVersion };

        _repositoryMock.Setup(x => x.GetPendingBatchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { row });

        _repositoryMock.Setup(x => x.MarkPublishedAsync(msgId, rowVersion, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _worker.StartAsync(CancellationToken.None);
        await Task.Delay(2000);
        await _worker.StopAsync(CancellationToken.None);

        // Assert
        _publisherMock.Verify(x => x.PublishAsync(row, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _repositoryMock.Verify(x => x.MarkPublishedAsync(msgId, rowVersion, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _repositoryMock.Verify(x => x.MarkFailedAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Worker_ShouldMarkFailed_WhenPublishThrows()
    {
        // Arrange
        var msgId = Guid.NewGuid();
        var rowVersion = new byte[] { 4, 5, 6 };
        var row = new OutboxRow { Id = msgId, MessageType = "test.event", Payload = "{}", RowVersion = rowVersion };

        _repositoryMock.Setup(x => x.GetPendingBatchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { row });

        _publisherMock.Setup(x => x.PublishAsync(row, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("RabbitMQ failure"));

        _repositoryMock.Setup(x => x.MarkFailedAsync(msgId, rowVersion, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _worker.StartAsync(CancellationToken.None);
        await Task.Delay(2000);
        await _worker.StopAsync(CancellationToken.None);

        // Assert
        _publisherMock.Verify(x => x.PublishAsync(row, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _repositoryMock.Verify(x => x.MarkPublishedAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(x => x.MarkFailedAsync(msgId, rowVersion, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Worker_RabbitMqDisabled_LeavesPending()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?> {
            {"Outbox:Enabled", "true"},
            {"RabbitMq:Enabled", "false"}, // DISABLED
            {"Outbox:PollingIntervalSeconds", "1"},
            {"ConnectionStrings:IdentityDatabase", "Server=myServerAddress;Database=myDataBase;"}
        };
        var finalConfig = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var worker = new RabbitMqOutboxRelayWorker(finalConfig, _loggerMock.Object, _publisherMock.Object, _repositoryMock.Object);

        // Act
        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(1000);
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _repositoryMock.Verify(x => x.GetPendingBatchAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(x => x.PublishAsync(It.IsAny<OutboxRow>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(x => x.MarkPublishedAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(x => x.MarkFailedAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
