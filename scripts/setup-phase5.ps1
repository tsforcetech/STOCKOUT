$ErrorActionPreference = "Stop"
$dir = "c:\DEV\API PROJECT\STOCKOUT"
$services = Get-ChildItem -Path "$dir\services" -Directory

foreach ($svc in $services) {
    $svcName = $svc.Name
    # Get proper namespace from service-inventory.json
    $json = Get-Content "$dir\service-inventory.json" | ConvertFrom-Json
    $ns = ($json.services | Where-Object { $_.serviceKey -eq $svcName }).namespace
    
    $archTestsDir = "$($svc.FullName)\tests\$ns.ArchitectureTests"
    
    $testCode = @"
using NetArchTest.Rules;
using Xunit;

namespace $($ns).ArchitectureTests;

public class DependencyRulesTests
{
    private const string DomainNamespace = "$($ns).Domain";
    private const string ApplicationNamespace = "$($ns).Application";
    private const string InfrastructureNamespace = "$($ns).Infrastructure";
    private const string ContractsNamespace = "$($ns).Contracts";
    private const string ApiNamespace = "$($ns).Api";
    private const string WorkerNamespace = "$($ns).Worker";

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
"@
    Set-Content -Path "$archTestsDir\DependencyRulesTests.cs" -Value $testCode -Encoding utf8
}

Write-Host "Architecture Tests configured."
