using Emcore.IdentityAccess.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Dapper;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    services.AddOptions<RabbitMqOptions>()
        .Bind(hostContext.Configuration.GetSection("RabbitMq"))
        .Validate(opts =>
        {
            if (!opts.Enabled) return true;
            return !string.IsNullOrWhiteSpace(opts.HostName) &&
                   !string.IsNullOrWhiteSpace(opts.UserName) &&
                   !string.IsNullOrWhiteSpace(opts.Password) &&
                   !string.IsNullOrWhiteSpace(opts.Exchange) &&
                   !string.IsNullOrWhiteSpace(opts.ConnectionName) &&
                   opts.Port > 0 && opts.Port <= 65535 &&
                   opts.PublisherConfirmTimeoutSeconds > 0;
        }, "Invalid RabbitMQ configuration. HostName, UserName, Password, Exchange, and ConnectionName must not be empty when RabbitMQ is enabled.")
        .ValidateOnStart();

    services.AddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
    services.AddSingleton<IOutboxRepository>(sp => new OutboxRepository(hostContext.Configuration.GetConnectionString("IdentityDatabase") ?? ""));
    services.AddHostedService<RabbitMqOutboxRelayWorker>();
    services.AddHostedService<IdentitySecurityDataCleanupWorker>();
});

var host = builder.Build();
await host.RunAsync();

public class OutboxRow
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = "1.0.0";
    public string Payload { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAtUtc { get; set; }
}

public class RabbitMqOutboxRelayWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqOutboxRelayWorker> _logger;
    private readonly IIntegrationEventPublisher _publisher;
    private readonly IOutboxRepository _repository;

    public RabbitMqOutboxRelayWorker(IConfiguration configuration, ILogger<RabbitMqOutboxRelayWorker> logger, IIntegrationEventPublisher publisher, IOutboxRepository repository)
    {
        _configuration = configuration;
        _logger = logger;
        _publisher = publisher;
        _repository = repository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var outboxEnabled = _configuration.GetValue<bool>("Outbox:Enabled", true);
        var rabbitMqEnabled = _configuration.GetValue<bool>("RabbitMq:Enabled", true);
        var pollingInterval = _configuration.GetValue<int>("Outbox:PollingIntervalSeconds", 5);
        var batchSize = _configuration.GetValue<int>("Outbox:BatchSize", 50);
        var maxAttempts = _configuration.GetValue<int>("Outbox:MaxPublishAttempts", 10);
        var connectionString = _configuration.GetConnectionString("IdentityDatabase");

        _logger.LogInformation("Identity Outbox Relay Worker starting. Outbox Enabled: {OutboxEnabled}, RabbitMQ Enabled: {RabbitMqEnabled}", outboxEnabled, rabbitMqEnabled);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!outboxEnabled || string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("dummy") || connectionString.Contains("inmemory"))
                {
                    _logger.LogDebug("Outbox polling bypassed (disabled or in-memory fallback mode).");
                }
                else if (!rabbitMqEnabled)
                {
                    _logger.LogDebug("RabbitMQ is disabled. Outbox polling bypassed.");
                }
                else
                {
                    await ProcessOutboxBatchAsync(batchSize, maxAttempts, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while relaying identity outbox events.");
            }

            await Task.Delay(pollingInterval * 1000, stoppingToken);
        }
    }

    private async Task ProcessOutboxBatchAsync(int batchSize, int maxAttempts, CancellationToken stoppingToken)
    {
        var pending = await _repository.GetPendingBatchAsync(batchSize, maxAttempts, stoppingToken);

        foreach (var item in pending)
        {
            try
            {
                await _publisher.PublishAsync(item, stoppingToken);

                var marked = await _repository.MarkPublishedAsync(item.Id, item.RowVersion, stoppingToken);
                if (!marked)
                {
                    _logger.LogWarning("Outbox claim no longer owned when marking published. EventId={EventId}", item.Id);
                }
            }
            catch (Exception pubEx)
            {
                var sanitizedError = pubEx.Message;
                if (sanitizedError.Length > 2000) sanitizedError = sanitizedError.Substring(0, 2000);

                var marked = await _repository.MarkFailedAsync(item.Id, item.RowVersion, sanitizedError, maxAttempts, stoppingToken);
                if (!marked)
                {
                    _logger.LogWarning("Outbox claim no longer owned when marking failed. EventId={EventId}", item.Id);
                }
            }
        }
    }
}

public class IdentitySecurityDataCleanupWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentitySecurityDataCleanupWorker> _logger;

    public IdentitySecurityDataCleanupWorker(IConfiguration configuration, ILogger<IdentitySecurityDataCleanupWorker> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = _configuration.GetValue<int>("Cleanup:IntervalHours", 1);
        var retentionHours = _configuration.GetValue<int>("Cleanup:RetentionHours", 24);
        var connectionString = _configuration.GetConnectionString("IdentityDatabase");

        _logger.LogInformation("Identity Security Data Cleanup Worker started. Interval: {Interval}h, Retention: {Retention}h.", intervalHours, retentionHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains("dummy") && !connectionString.Contains("inmemory"))
                {
                    using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync(stoppingToken);
                    await connection.ExecuteAsync(
                        "dbo.PR_IDENTITY_CLEANUP_EXPIRED_SECURITY_DATA",
                        new { RetentionHours = retentionHours },
                        commandType: System.Data.CommandType.StoredProcedure);
                    _logger.LogInformation("Executed security data cleanup stored procedure successfully.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed executing periodic security data cleanup.");
            }

            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }
}
