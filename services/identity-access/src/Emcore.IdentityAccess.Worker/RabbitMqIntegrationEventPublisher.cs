using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Emcore.IdentityAccess.Worker;

public class RabbitMqIntegrationEventPublisher : IIntegrationEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqIntegrationEventPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _initialized;

    public RabbitMqIntegrationEventPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqIntegrationEventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (_initialized && _connection is { IsOpen: true } && _channel is { IsOpen: true })
        {
            return;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_initialized && _connection is { IsOpen: true } && _channel is { IsOpen: true })
            {
                return;
            }

            _logger.LogInformation("Connecting to RabbitMQ at {HostName}:{Port}...", _options.HostName, _options.Port);

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                VirtualHost = _options.VirtualHost,
                UserName = _options.UserName,
                Password = _options.Password,
                ClientProvidedName = _options.ConnectionName,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            var channelOptions = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true
            );
            _channel = await _connection.CreateChannelAsync(channelOptions, cancellationToken);

            // Declare Exchange
            await _channel.ExchangeDeclareAsync(
                exchange: _options.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            _initialized = true;
            _logger.LogInformation("Successfully connected to RabbitMQ and declared exchange {Exchange}.", _options.Exchange);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task PublishAsync(OutboxRow message, CancellationToken cancellationToken)
    {
        await EnsureConnectionAsync(cancellationToken);

        if (_channel == null)
            throw new InvalidOperationException("RabbitMQ Channel is not initialized.");

        var routingKey = message.MessageType.ToLowerInvariant();
        var body = Encoding.UTF8.GetBytes(message.Payload);

        var properties = new BasicProperties
        {
            MessageId = message.Id.ToString(),
            Type = message.MessageType,
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            DeliveryMode = DeliveryModes.Persistent
        };

        properties.Headers = new System.Collections.Generic.Dictionary<string, object?>
        {
            { "x-event-id", message.Id.ToString() },
            { "x-message-type", message.MessageType },
            { "x-schema-version", message.SchemaVersion },
            { "x-source-service", "IdentityAccess" }
        };

        if (message.GetType().GetProperty("CorrelationId")?.GetValue(message) is string correlationId && !string.IsNullOrEmpty(correlationId))
        {
            properties.CorrelationId = correlationId;
            properties.Headers["x-correlation-id"] = correlationId;
        }

        var timeout = TimeSpan.FromSeconds(_options.PublisherConfirmTimeoutSeconds);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            // In RabbitMQ.Client v7, BasicPublishAsync awaits the publisher confirm if enabled
            await _channel.BasicPublishAsync(
                exchange: _options.Exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cts.Token);

            _logger.LogInformation("Published Identity outbox event. EventId={EventId} MessageType={MessageType} Attempt={Attempt}", message.Id, message.MessageType, message.AttemptCount + 1);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Publisher confirm timeout while publishing event {EventId}.", message.Id);
            throw new TimeoutException($"RabbitMQ publisher confirm timed out after {_options.PublisherConfirmTimeoutSeconds} seconds.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish Identity outbox event. EventId={EventId} MessageType={MessageType} Attempt={Attempt}", message.Id, message.MessageType, message.AttemptCount + 1);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is { IsOpen: true })
        {
            await _channel.CloseAsync();
        }

        if (_connection is { IsOpen: true })
        {
            await _connection.CloseAsync();
        }

        _semaphore.Dispose();
    }
}
