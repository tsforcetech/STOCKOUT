using Emcore.IdentityAccess.Application.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Emcore.IdentityAccess.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IdentityApplicationService>();
        return services;
    }
}
