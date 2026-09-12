namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;

public class PriceData
{
    public string Symbol { get; set; } = string.Empty;
    public string TokenSymbol { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public decimal? Change24h { get; set; }
    public decimal? Volume24h { get; set; }
}

