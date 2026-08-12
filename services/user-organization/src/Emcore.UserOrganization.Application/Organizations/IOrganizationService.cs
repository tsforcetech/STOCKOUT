using System.Threading.Tasks;
using Emcore.UserOrganization.Contracts.Organizations;

namespace Emcore.UserOrganization.Application.Organizations;

public interface IOrganizationService
{
    Task<OrganizationResponse> CreateOrganizationAsync(string ownerUserId, CreateOrganizationRequest request);
    Task<OrganizationResponse?> GetOrganizationAsync(string id);
}
