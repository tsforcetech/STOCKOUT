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

        string? env = configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IVerificationDeliveryService, ProductionVerificationDeliveryService>();
        }
        else
        {
            services.AddScoped<IVerificationDeliveryService, DevelopmentVerificationDeliveryService>();
        }

        return services;
    }
}
