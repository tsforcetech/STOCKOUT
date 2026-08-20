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
    services.Configure<RabbitMqOptions>(hostContext.Configuration.GetSection("RabbitMq"));
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
                if (outboxEnabled && !string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains("dummy") && !connectionString.Contains("inmemory"))
                {
                    await ProcessOutboxBatchAsync(rabbitMqEnabled, batchSize, maxAttempts, stoppingToken);
                }
                else
                {
                    _logger.LogDebug("Outbox polling bypassed (disabled or in-memory fallback mode).");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while relaying identity outbox events.");
            }

            await Task.Delay(pollingInterval * 1000, stoppingToken);
        }
    }

    private async Task ProcessOutboxBatchAsync(bool rabbitMqEnabled, int batchSize, int maxAttempts, CancellationToken stoppingToken)
    {
        var pending = await _repository.GetPendingBatchAsync(batchSize, stoppingToken);

        foreach (var item in pending)
        {
            try
            {
                if (rabbitMqEnabled)
                {
                    await _publisher.PublishAsync(item, stoppingToken);
                }
                else
                {
                    _logger.LogWarning("RabbitMQ is disabled. Skipping publish for event {Id}. Leaving as Pending.", item.Id);
                    continue; // Do NOT mark published if RabbitMQ is disabled
                }

                await _repository.MarkPublishedAsync(item.Id, stoppingToken);
            }
            catch (Exception pubEx)
            {
                var sanitizedError = pubEx.Message;
                if (sanitizedError.Length > 2000) sanitizedError = sanitizedError.Substring(0, 2000);

                await _repository.MarkFailedAsync(item.Id, sanitizedError, maxAttempts, stoppingToken);
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
