using NetArchTest.Rules;
using Xunit;

namespace Emcore.BiddingDeal.ArchitectureTests;

public class DependencyRulesTests
{
    private const string DomainNamespace = "Emcore.BiddingDeal.Domain";
    private const string ApplicationNamespace = "Emcore.BiddingDeal.Application";
    private const string InfrastructureNamespace = "Emcore.BiddingDeal.Infrastructure";
    private const string ContractsNamespace = "Emcore.BiddingDeal.Contracts";
    private const string ApiNamespace = "Emcore.BiddingDeal.Api";
    private const string WorkerNamespace = "Emcore.BiddingDeal.Worker";

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
