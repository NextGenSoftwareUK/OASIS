namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs.Oracle;

/// <summary>
/// Radix price feed data for oracle operations
/// </summary>
public class RadixPriceFeed
{
    public string TokenSymbol { get; set; } = "XRD";
    public string Currency { get; set; } = "USD";
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal? Change24h { get; set; }
    public decimal? Volume24h { get; set; }
    public string Source { get; set; } = string.Empty; // "CoinGecko", "CoinMarketCap", "RadixDEX", etc.
    public decimal Confidence { get; set; } = 100; // 0-100%
}


