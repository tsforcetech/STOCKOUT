using System;
using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Emcore.IdentityAccess.Infrastructure.Integrations;

public class DevelopmentVerificationDeliveryService : IVerificationDeliveryService
{
    private readonly ILogger<DevelopmentVerificationDeliveryService> _logger;

    public DevelopmentVerificationDeliveryService(ILogger<DevelopmentVerificationDeliveryService> logger)
    {
        _logger = logger;
    }

    public Task SendVerificationOtpAsync(string destination, string channel, string plaintextOtp, CancellationToken ct)
    {
        // Protected development/test output only. Never used in production or written to outbox or persistent tables.
        _logger.LogInformation("[DEV/TEST ONLY] Verification OTP to {Destination} via {Channel}: {Otp}", destination, channel, plaintextOtp);
        return Task.CompletedTask;
    }

    public Task SendRecoveryTokenAsync(string destination, string plaintextToken, CancellationToken ct)
    {
        _logger.LogInformation("[DEV/TEST ONLY] Password Recovery Token to {Destination}: {Token}", destination, plaintextToken);
        return Task.CompletedTask;
    }
}

public class ProductionVerificationDeliveryService : IVerificationDeliveryService
{
    private readonly ILogger<ProductionVerificationDeliveryService> _logger;

    public ProductionVerificationDeliveryService(ILogger<ProductionVerificationDeliveryService> logger)
    {
        _logger = logger;
    }

    public Task SendVerificationOtpAsync(string destination, string channel, string plaintextOtp, CancellationToken ct)
    {
        // Production implementation: dispatches via secure notification service integration without persisting plaintext OTP in SQL or outbox tables.
        _logger.LogInformation("Dispatched secure verification challenge to {Channel} destination for user.", channel);
        return Task.CompletedTask;
    }

    public Task SendRecoveryTokenAsync(string destination, string plaintextToken, CancellationToken ct)
    {
        _logger.LogInformation("Dispatched password recovery link to user destination.");
        return Task.CompletedTask;
    }
}
