using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    services.AddHostedService<RabbitMqOutboxRelayWorker>();
});

var host = builder.Build();
await host.RunAsync();

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
        var outboxEnabled = _configuration.GetValue<bool>("Outbox:Enabled");
        var rabbitMqEnabled = _configuration.GetValue<bool>("RabbitMq:Enabled");
        var pollingInterval = _configuration.GetValue<int>("Outbox:PollingIntervalSeconds", 5);

        _logger.LogInformation("Identity Outbox Relay Worker starting. Outbox Enabled: {OutboxEnabled}, RabbitMQ Enabled: {RabbitMqEnabled}", outboxEnabled, rabbitMqEnabled);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (outboxEnabled && rabbitMqEnabled)
            {
                // Poll outbox and publish to RabbitMQ
                // _logger.LogInformation("Polling outbox...");
            }
            
            await Task.Delay(pollingInterval * 1000, stoppingToken);
        }
    }
}
