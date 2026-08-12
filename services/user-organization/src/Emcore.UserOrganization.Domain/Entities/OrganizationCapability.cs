using System;
using Emcore.UserOrganization.Domain.Enums;

namespace Emcore.UserOrganization.Domain.Entities;

public class OrganizationCapability
{
    public string OrganizationId { get; set; } = string.Empty;
    public MarketplaceCapability CapabilityCode { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
