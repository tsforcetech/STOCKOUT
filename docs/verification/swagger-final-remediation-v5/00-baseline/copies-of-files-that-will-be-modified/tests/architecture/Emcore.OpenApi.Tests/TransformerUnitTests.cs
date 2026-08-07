using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.OpenApi;
using Emcore.BuildingBlocks.Api;
using FluentAssertions;
using Xunit;

namespace Emcore.OpenApi.Tests;

public class TransformerUnitTests
{
    [Fact]
    public void AddEmcoreOpenApi_RegistersOpenApiServicesAndTransformers()
    {
        var services = new ServiceCollection();

        services.AddEmcoreOpenApi("v1", "Test API", "Test Description", "2.0.0", "Test Team", "Test Consumers", isInternal: true);

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.Should().NotBeNull();

        // Verify OpenApiOptions are registered
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<OpenApiOptions>>();
        options.Should().NotBeNull();
    }

    [Fact]
    public void OpenApiOptionsExtensions_CanBeChainedWithoutExceptions()
    {
        var options = new OpenApiOptions();

        var exception = Record.Exception(() =>
        {
            options.AddEmcoreSwaggerVersioning("v1", "1.0.0")
                   .AddEmcoreSwaggerSecurity(isInternal: false)
                   .AddEmcoreSwaggerHeaders()
                   .AddEmcoreSwaggerProblemDetails()
                   .AddEmcoreSwaggerExamples();
        });

        exception.Should().BeNull();
    }
}
