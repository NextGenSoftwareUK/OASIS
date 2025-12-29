namespace Application.DTOs.EquityPrice;

/// <summary>
/// Response containing historical equity prices
/// </summary>
public sealed record EquityPriceHistoryResponse
{
    public string Symbol { get; init; } = string.Empty;
    public List<EquityPriceDataPointDto> Prices { get; init; } = new();
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public bool Adjusted { get; init; }
}

/// <summary>
/// A single data point in price history
/// </summary>
public sealed record EquityPriceDataPointDto
{
    public DateTime PriceDate { get; init; }
    public decimal RawPrice { get; init; }
    public decimal AdjustedPrice { get; init; }
    public decimal Confidence { get; init; }
}

