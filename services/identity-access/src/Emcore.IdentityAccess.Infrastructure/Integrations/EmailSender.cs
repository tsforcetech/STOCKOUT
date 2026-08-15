using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Emcore.IdentityAccess.Infrastructure.Integrations;

public interface IEmailSender
{
    Task SendEmailAsync(string toAddress, string subject, string textBody, string htmlBody, CancellationToken ct);
}

public class SmtpEmailSender : IEmailSender, IDisposable
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly SmtpClient _client;
    private readonly string _fromAddress;
    private readonly string _fromName;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _logger = logger;
        
        string host = config["Email:Host"] ?? string.Empty;
        int port = int.TryParse(config["Email:Port"], out int p) ? p : 587;
        bool useSsl = bool.TryParse(config["Email:UseSsl"], out bool s) ? s : true;
        string username = config["Email:Username"] ?? string.Empty;
        string password = config["Email:Password"] ?? string.Empty;
        _fromAddress = config["Email:FromAddress"] ?? "noreply@example.com";
        _fromName = config["Email:FromName"] ?? "STOCKOUT";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(_fromAddress))
        {
            throw new InvalidOperationException("Email:Host and Email:FromAddress must be configured when using SmtpEmailSender.");
        }

        _client = new SmtpClient(host, port)
        {
            EnableSsl = useSsl
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            _client.UseDefaultCredentials = false;
            _client.Credentials = new NetworkCredential(username, password);
        }
    }

    public async Task SendEmailAsync(string toAddress, string subject, string textBody, string htmlBody, CancellationToken ct)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_fromAddress, _fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            
            message.To.Add(toAddress);

            var plainTextView = AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain");
            message.AlternateViews.Add(plainTextView);
            
            ct.Register(() => _client.SendAsyncCancel());
            await _client.SendMailAsync(message, ct);
            _logger.LogInformation("Email sent successfully to a secure destination. Subject: {Subject}", subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to secure destination. Subject: {Subject}", subject);
            throw;
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

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
        // We do NOT log the OTP plaintext to the console per security requirements.
        _logger.LogInformation("Fake email dispatched to {Destination}. Subject: {Subject}", toAddress, subject);
        return Task.CompletedTask;
    }
}
