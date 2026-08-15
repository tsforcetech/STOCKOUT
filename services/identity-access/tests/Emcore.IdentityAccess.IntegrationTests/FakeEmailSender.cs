using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Infrastructure.Integrations;
using Microsoft.Extensions.Logging;

namespace Emcore.IdentityAccess.IntegrationTests;

public class FakeEmailSender : IEmailSender
{
    private readonly ILogger<FakeEmailSender> _logger;
    public readonly ConcurrentBag<(string To, string Subject, string TextBody, string HtmlBody)> SentEmails = new();

    public FakeEmailSender(ILogger<FakeEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string toAddress, string subject, string textBody, string htmlBody, CancellationToken ct)
    {
        SentEmails.Add((toAddress, subject, textBody, htmlBody));
        _logger.LogInformation("Fake email dispatched to {Destination}. Subject: {Subject}", toAddress, subject);
        return Task.CompletedTask;
    }
}
