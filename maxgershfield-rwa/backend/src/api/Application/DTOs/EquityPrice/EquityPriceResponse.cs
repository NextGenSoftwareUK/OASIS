namespace Application.DTOs.EquityPrice;

/// <summary>
/// Response containing equity price information with source breakdown
/// </summary>
public sealed record EquityPriceResponse
{
    public string Symbol { get; init; } = string.Empty;
    public decimal RawPrice { get; init; }
    public decimal AdjustedPrice { get; init; }
    public decimal Confidence { get; init; }
    public DateTime PriceDate { get; init; }
    public List<SourcePriceDto> Sources { get; init; } = new();
    public List<CorporateActionInfoDto> CorporateActionsApplied { get; init; } = new();
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Information about a price source
/// </summary>
public sealed record SourcePriceDto
{
    public string SourceName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public DateTime Timestamp { get; init; }
    public decimal ReliabilityScore { get; init; }
    public int LatencyMs { get; init; }
}

/// <summary>
/// Information about corporate actions applied to the price
/// </summary>
public sealed record CorporateActionInfoDto
{
    public CorporateActionType Type { get; init; }
    public DateTime EffectiveDate { get; init; }
    public decimal AdjustmentFactor { get; init; }
}

