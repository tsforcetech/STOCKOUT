using System;
using Emcore.InspectionTrust.Domain.Enums;

namespace Emcore.InspectionTrust.Domain.Entities;

public class VerificationCase
{
    public string Id { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public VerificationType Type { get; set; }
    public VerificationStatus Status { get; set; }
    public string? ReviewerNotes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
