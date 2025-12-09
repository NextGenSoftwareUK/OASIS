namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

/// <summary>
/// Response model for shipping quotes
/// </summary>
public class QuoteResponse
{
    public Guid QuoteId { get; set; }
    public List<QuoteOption> Quotes { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Individual quote option from a carrier
/// </summary>
public class QuoteOption
{
    public string Carrier { get; set; } = string.Empty;
    public decimal CarrierRate { get; set; }
    public decimal ClientPrice { get; set; }
    public decimal MarkupAmount { get; set; }
    public int EstimatedDays { get; set; }
    public string? ServiceName { get; set; }
}




