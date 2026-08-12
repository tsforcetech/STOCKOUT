using System;

namespace Emcore.SearchDiscovery.Domain.Projections;

public class SellerInfo
{
    public string Id { get; set; } = string.Empty;
    public int EntityType { get; set; } // 1 = Individual, 2 = Business
    public string DisplayName { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
}

public class ListingSearchDocument
{
    public string ListingId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public SellerInfo Seller { get; set; } = new SellerInfo();
    public DateTime CreatedAtUtc { get; set; }
}
