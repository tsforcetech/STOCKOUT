using System;
using System.Collections.Generic;
using Emcore.UserOrganization.Domain.Enums;

namespace Emcore.UserOrganization.Domain.Entities;

public class Organization
{
    public string Id { get; set; } = string.Empty;
    public OrganizationEntityType EntityType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public OrganizationStatus Status { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<OrganizationCapability> Capabilities { get; set; } = new List<OrganizationCapability>();
}
