using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using Emcore.UserOrganization.Domain.Entities;
using Emcore.UserOrganization.Domain.Enums;
using Emcore.UserOrganization.Domain.Repositories;

namespace Emcore.UserOrganization.Infrastructure.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly string _connectionString;

    public OrganizationRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("OrganizationDatabase") ?? "";
    }

    public async Task CreateAsync(Organization organization)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(
                "PR_ORGANIZATION_CREATE",
                new
                {
                    Id = Guid.Parse(organization.Id),
                    EntityType = (int)organization.EntityType,
                    DisplayName = organization.DisplayName,
                    LegalName = organization.LegalName,
                    Status = (int)organization.Status,
                    OwnerUserId = Guid.Parse(organization.OwnerUserId)
                },
                transaction,
                commandType: System.Data.CommandType.StoredProcedure);

            foreach (var cap in organization.Capabilities)
            {
                await connection.ExecuteAsync(
                    "PR_ORGANIZATION_ASSIGN_CAPABILITY",
                    new
                    {
                        OrganizationId = Guid.Parse(cap.OrganizationId),
                        CapabilityCode = (int)cap.CapabilityCode
                    },
                    transaction,
                    commandType: System.Data.CommandType.StoredProcedure);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<Organization?> GetByIdAsync(string id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var multi = await connection.QueryMultipleAsync(
            "PR_ORGANIZATION_GET_BY_ID",
            new { Id = Guid.Parse(id) },
            commandType: System.Data.CommandType.StoredProcedure);

        var orgDto = await multi.ReadSingleOrDefaultAsync<dynamic>();
        if (orgDto == null) return null;

        var capsDto = await multi.ReadAsync<dynamic>();

        var org = new Organization
        {
            Id = orgDto.Id.ToString(),
            EntityType = (OrganizationEntityType)orgDto.EntityType,
            DisplayName = orgDto.DisplayName,
            LegalName = orgDto.LegalName,
            Status = (OrganizationStatus)orgDto.Status,
            OwnerUserId = orgDto.OwnerUserId.ToString(),
            CreatedAtUtc = orgDto.CreatedAtUtc,
            UpdatedAtUtc = orgDto.UpdatedAtUtc
        };

        foreach (var cap in capsDto)
        {
            org.Capabilities.Add(new OrganizationCapability
            {
                OrganizationId = cap.OrganizationId.ToString(),
                CapabilityCode = (MarketplaceCapability)cap.CapabilityCode,
                CreatedAtUtc = cap.CreatedAtUtc
            });
        }

        return org;
    }
}
