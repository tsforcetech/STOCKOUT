using System;
using System.Threading;
using System.Threading.Tasks;
using Emcore.IdentityAccess.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Emcore.IdentityAccess.Infrastructure.Integrations;

public class VerificationDeliveryService : IVerificationDeliveryService
{
    private readonly ILogger<VerificationDeliveryService> _logger;
    private readonly IEmailSender _emailSender;
    private readonly int _verificationLifetimeMinutes;
    private readonly int _passwordResetLifetimeMinutes;

    public VerificationDeliveryService(
        ILogger<VerificationDeliveryService> logger, 
        IEmailSender emailSender, 
        Emcore.IdentityAccess.Application.Configuration.IdentityOptions options)
    {
        _logger = logger;
        _emailSender = emailSender;
        _verificationLifetimeMinutes = options?.VerificationLifetimeMinutes ?? 10;
        _passwordResetLifetimeMinutes = options?.PasswordResetLifetimeMinutes ?? 15;
    }

    public async Task SendVerificationOtpAsync(string destination, string channel, string plaintextOtp, CancellationToken ct)
    {
        if (channel == "Email" || channel == "StepUp" || channel == "TOTP" || channel == "EMAIL_OTP")
        {
            string subject = "Your STOCKOUT verification code";
            string textBody = $@"Your verification code is:

{plaintextOtp}

This code expires in {_verificationLifetimeMinutes} minutes.

If you did not attempt to sign in, you can ignore this email.
This code confirms enabling or accessing multi-factor authentication.";

            string htmlBody = $@"<p>Your verification code is:</p>
<h2>{plaintextOtp}</h2>
<p>This code expires in {_verificationLifetimeMinutes} minutes.</p>
<p>If you did not attempt to sign in, you can ignore this email.</p>
<p><small>This code confirms enabling or accessing multi-factor authentication.</small></p>";

            await _emailSender.SendEmailAsync(destination, subject, textBody, htmlBody, ct);
            _logger.LogInformation("Email OTP dispatched. Purpose={Channel} UserId=<safe identifier>", channel);
        }
        else
        {
            _logger.LogWarning("Unsupported delivery channel requested: {Channel}", channel);
        }
    }

    public async Task SendRecoveryTokenAsync(string destination, string plaintextToken, CancellationToken ct)
    {
        string subject = "STOCKOUT Password Recovery";
        string textBody = $@"Your password recovery token is:

{plaintextToken}

This token expires in {_passwordResetLifetimeMinutes} minutes.

If you did not request a password reset, you can safely ignore this email.";

        string htmlBody = $@"<p>Your password recovery token is:</p>
<h2>{plaintextToken}</h2>
<p>This token expires in {_passwordResetLifetimeMinutes} minutes.</p>
<p>If you did not request a password reset, you can safely ignore this email.</p>";

        await _emailSender.SendEmailAsync(destination, subject, textBody, htmlBody, ct);
        _logger.LogInformation("Recovery Token dispatched. UserId=<safe identifier>");
    }
}
