using System.Threading.Tasks;
using Emcore.UserOrganization.Domain.Entities;

namespace Emcore.UserOrganization.Domain.Repositories;

public interface IOrganizationRepository
{
    Task CreateAsync(Organization organization);
    Task<Organization?> GetByIdAsync(string id);
}
