using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Emcore.UserOrganization.Application.Organizations;
using Emcore.UserOrganization.Contracts.Organizations;
using Emcore.UserOrganization.Domain.Entities;
using Emcore.UserOrganization.Domain.Repositories;

namespace Emcore.UserOrganization.UnitTests.Application;

public class OrganizationServiceTests
{
    private class MockOrganizationRepository : IOrganizationRepository
    {
        private readonly List<Organization> _organizations = new();

        public Task CreateAsync(Organization organization)
        {
            _organizations.Add(organization);
            return Task.CompletedTask;
        }

        public Task<Organization?> GetByIdAsync(string id)
        {
            return Task.FromResult(_organizations.FirstOrDefault(o => o.Id == id));
        }
    }

    [Fact]
    public async Task CreateOrganization_Individual_Succeeds()
    {
        var repo = new MockOrganizationRepository();
        var service = new OrganizationService(repo);

        var request = new CreateOrganizationRequest
        {
            EntityType = 1, // Individual
            DisplayName = "John Doe",
            Capabilities = new List<int> { 1, 2 } // Buyer, Seller
        };

        var response = await service.CreateOrganizationAsync("user-1", request);

        Assert.NotNull(response);
        Assert.Equal("John Doe", response.DisplayName);
        Assert.Null(response.LegalName);
        Assert.Equal(1, response.EntityType);
        Assert.Equal(2, response.Capabilities.Count);
    }

    [Fact]
    public async Task CreateOrganization_Business_MissingLegalName_Throws()
    {
        var repo = new MockOrganizationRepository();
        var service = new OrganizationService(repo);

        var request = new CreateOrganizationRequest
        {
            EntityType = 2, // Business
            DisplayName = "ACME Corp",
            // LegalName is missing
            Capabilities = new List<int> { 1 }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateOrganizationAsync("user-2", request));
    }
}
