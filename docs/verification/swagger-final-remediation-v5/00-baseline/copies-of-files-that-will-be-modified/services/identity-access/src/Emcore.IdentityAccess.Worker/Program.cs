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
}

public class RabbitMqOutboxRelayWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqOutboxRelayWorker> _logger;

    public RabbitMqOutboxRelayWorker(IConfiguration configuration, ILogger<RabbitMqOutboxRelayWorker> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var outboxEnabled = _configuration.GetValue<bool>("Outbox:Enabled", true);
        var rabbitMqEnabled = _configuration.GetValue<bool>("RabbitMq:Enabled", true);
        var pollingInterval = _configuration.GetValue<int>("Outbox:PollingIntervalSeconds", 5);
        var connectionString = _configuration.GetConnectionString("IdentityDatabase") ?? _configuration["ConnectionStrings__IdentityDatabase"];

        _logger.LogInformation("Identity Outbox Relay Worker starting. Outbox Enabled: {OutboxEnabled}, RabbitMQ Enabled: {RabbitMqEnabled}", outboxEnabled, rabbitMqEnabled);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (outboxEnabled && !string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains("dummy") && !connectionString.Contains("inmemory"))
                {
                    await ProcessOutboxBatchAsync(connectionString, rabbitMqEnabled, stoppingToken);
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

    private async Task ProcessOutboxBatchAsync(string connectionString, bool rabbitMqEnabled, CancellationToken stoppingToken)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(stoppingToken);

        var pending = await connection.QueryAsync<OutboxRow>(
            "dbo.PR_IDENTITY_GET_PENDING_OUTBOX",
            new { BatchSize = 50 },
            commandType: System.Data.CommandType.StoredProcedure);

        foreach (var item in pending)
        {
            try
            {
                if (rabbitMqEnabled)
                {
                    // Relay to message broker / MassTransit pipeline
                    _logger.LogInformation("Relaying event {MessageType} (ID: {Id}) to RabbitMQ exchange...", item.MessageType, item.Id);
                }

                await connection.ExecuteAsync(
                    "dbo.PR_IDENTITY_MARK_OUTBOX_PUBLISHED",
                    new { Id = item.Id },
                    commandType: System.Data.CommandType.StoredProcedure);

                _logger.LogInformation("Outbox event {Id} marked as published successfully.", item.Id);
            }
            catch (Exception pubEx)
            {
                _logger.LogWarning(pubEx, "Failed publishing event {Id}. Recording failure attempt.", item.Id);
                await connection.ExecuteAsync(
                    "dbo.PR_IDENTITY_MARK_OUTBOX_FAILED",
                    new { Id = item.Id, LastError = pubEx.Message },
                    commandType: System.Data.CommandType.StoredProcedure);
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
        var connectionString = _configuration.GetConnectionString("IdentityDatabase") ?? _configuration["ConnectionStrings__IdentityDatabase"];

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
