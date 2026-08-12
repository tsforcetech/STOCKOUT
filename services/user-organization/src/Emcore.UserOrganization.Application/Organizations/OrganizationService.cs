using System;
using System.Linq;
using System.Threading.Tasks;
using Emcore.UserOrganization.Contracts.Organizations;
using Emcore.UserOrganization.Domain.Entities;
using Emcore.UserOrganization.Domain.Enums;
using Emcore.UserOrganization.Domain.Repositories;

namespace Emcore.UserOrganization.Application.Organizations;

public class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _repository;

    public OrganizationService(IOrganizationRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrganizationResponse> CreateOrganizationAsync(string ownerUserId, CreateOrganizationRequest request)
    {
        if (!Enum.IsDefined(typeof(OrganizationEntityType), request.EntityType))
        {
            throw new ArgumentException("INVALID_ORGANIZATION_TYPE");
        }
        
        var entityType = (OrganizationEntityType)request.EntityType;
        if (entityType == OrganizationEntityType.Business && string.IsNullOrWhiteSpace(request.LegalName))
        {
            throw new ArgumentException("Business accounts must provide a LegalName.");
        }

        if (request.Capabilities == null || !request.Capabilities.Any())
        {
            throw new ArgumentException("Organization must have at least one capability.");
        }

        foreach (var cap in request.Capabilities)
        {
            if (!Enum.IsDefined(typeof(MarketplaceCapability), cap))
            {
                throw new ArgumentException("INVALID_MARKETPLACE_CAPABILITY");
            }
        }

        var orgId = Guid.NewGuid().ToString();

        var organization = new Organization
        {
            Id = orgId,
            EntityType = entityType,
            DisplayName = request.DisplayName,
            LegalName = entityType == OrganizationEntityType.Individual ? null : request.LegalName,
            Status = OrganizationStatus.Active,
            OwnerUserId = ownerUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var cap in request.Capabilities.Distinct())
        {
            organization.Capabilities.Add(new OrganizationCapability
            {
                OrganizationId = orgId,
                CapabilityCode = (MarketplaceCapability)cap,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _repository.CreateAsync(organization);

        return await GetOrganizationAsync(orgId) ?? throw new Exception("Failed to retrieve created organization.");
    }

    public async Task<OrganizationResponse?> GetOrganizationAsync(string id)
    {
        var org = await _repository.GetByIdAsync(id);
        if (org == null) return null;

        return new OrganizationResponse
        {
            Id = org.Id,
            EntityType = (int)org.EntityType,
            DisplayName = org.DisplayName,
            LegalName = org.LegalName,
            Status = (int)org.Status,
            OwnerUserId = org.OwnerUserId,
            Capabilities = org.Capabilities.Select(c => (int)c.CapabilityCode).ToList()
        };
    }
}
