namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

/// <summary>
/// Quote entity model
/// </summary>
public class Quote
{
    public Guid QuoteId { get; set; }
    public Guid MerchantId { get; set; }
    public RateRequest ShipmentDetails { get; set; } = new();
    public List<CarrierRate> CarrierRates { get; set; } = new();
    public List<ClientQuote> ClientQuotes { get; set; } = new();
    public Guid? SelectedQuote { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Carrier rate information
/// </summary>
public class CarrierRate
{
    public string Carrier { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public int EstimatedDays { get; set; }
    public string? ServiceName { get; set; }
}

/// <summary>
/// Client quote with markup applied
/// </summary>
public class ClientQuote
{
    public string Carrier { get; set; } = string.Empty;
    public decimal CarrierRate { get; set; }
    public decimal MarkupAmount { get; set; }
    public decimal ClientPrice { get; set; }
    public Guid? MarkupConfigId { get; set; }
    public int EstimatedDays { get; set; }
}
