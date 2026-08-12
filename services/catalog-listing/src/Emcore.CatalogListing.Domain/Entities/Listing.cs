using System;

namespace Emcore.CatalogListing.Domain.Entities;

public class Listing
{
    public string Id { get; set; } = string.Empty;
    public string SellerOrganizationId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
