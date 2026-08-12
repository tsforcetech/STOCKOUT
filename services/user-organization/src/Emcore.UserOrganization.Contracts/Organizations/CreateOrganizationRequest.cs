using System.Collections.Generic;

namespace Emcore.UserOrganization.Contracts.Organizations;

public class CreateOrganizationRequest
{
    public int EntityType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public List<int> Capabilities { get; set; } = new();
}

public class OrganizationResponse
{
    public string Id { get; set; } = string.Empty;
    public int EntityType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public int Status { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public List<int> Capabilities { get; set; } = new();
}
