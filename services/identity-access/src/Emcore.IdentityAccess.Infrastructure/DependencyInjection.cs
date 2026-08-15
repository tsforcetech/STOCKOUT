using System;
using Emcore.IdentityAccess.Application.Abstractions;
using Emcore.IdentityAccess.Infrastructure.Integrations;
using Emcore.IdentityAccess.Infrastructure.Persistence;
using Emcore.IdentityAccess.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Emcore.IdentityAccess.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<JwtTokenGenerator>(sp => new JwtTokenGenerator(configuration));
        services.AddSingleton<ITokenGenerator>(sp => sp.GetRequiredService<JwtTokenGenerator>());
        services.AddSingleton<IJwksService>(sp => sp.GetRequiredService<JwtTokenGenerator>());
        services.AddScoped<IIdentityRepository, IdentityRepository>();

        string? provider = configuration["Email:Provider"];
        if (string.Equals(provider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            throw new InvalidOperationException($"Invalid or missing Email:Provider configuration. Expected 'Smtp' but got '{provider}'. FakeEmailSender is not permitted in runtime.");
        }
        services.AddScoped<IVerificationDeliveryService, VerificationDeliveryService>();

        return services;
    }
}
