using NetArchTest.Rules;
using Xunit;

namespace Emcore.NotificationIntegration.ArchitectureTests;

public class DependencyRulesTests
{
    private const string DomainNamespace = "Emcore.NotificationIntegration.Domain";
    private const string ApplicationNamespace = "Emcore.NotificationIntegration.Application";
    private const string InfrastructureNamespace = "Emcore.NotificationIntegration.Infrastructure";
    private const string ContractsNamespace = "Emcore.NotificationIntegration.Contracts";
    private const string ApiNamespace = "Emcore.NotificationIntegration.Api";
    private const string WorkerNamespace = "Emcore.NotificationIntegration.Worker";

    [Fact]
    public void Domain_Should_Not_DependOn_OtherLayers()
    {
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace, WorkerNamespace, ApplicationNamespace, ContractsNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_Should_Not_DependOn_ApiOrWorker()
    {
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(ApiNamespace, WorkerNamespace, InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Contracts_Should_Not_DependOn_Infrastructure()
    {
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(ContractsNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace, WorkerNamespace, ApplicationNamespace, DomainNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Api_Should_Not_Directly_Reference_Dapper_Or_SqlClient()
    {
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace(ApiNamespace)
            .ShouldNot()
            .HaveDependencyOnAny("Dapper", "Microsoft.Data.SqlClient")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
