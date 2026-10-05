namespace Application.DTOs.EquityPrice;

/// <summary>
/// Response containing batch equity prices for multiple symbols
/// </summary>
public sealed record BatchEquityPriceResponseDto
{
    public List<EquityPriceSummaryDto> Prices { get; init; } = new();
    public DateTime RequestedAt { get; init; }
}

/// <summary>
/// Summary of equity price for batch responses
/// </summary>
public sealed record EquityPriceSummaryDto
{
    public string Symbol { get; init; } = string.Empty;
    public decimal RawPrice { get; init; }
    public decimal AdjustedPrice { get; init; }
    public decimal Confidence { get; init; }
    public DateTime PriceDate { get; init; }
    public DateTime LastUpdated { get; init; }
}

