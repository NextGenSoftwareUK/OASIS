using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Represents an equity price with both raw and adjusted values
/// </summary>
public sealed class EquityPrice : BaseEntity
{
    /// <summary>
    /// Stock ticker symbol
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Raw price from data source (before corporate action adjustments)
    /// </summary>
    public decimal RawPrice { get; set; }

    /// <summary>
    /// Adjusted price (after applying corporate action adjustments)
    /// </summary>
    public decimal AdjustedPrice { get; set; }

    /// <summary>
    /// Confidence score (0-1 scale) based on source reliability and consensus
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Date/time of the price
    /// </summary>
    public DateTime PriceDate { get; set; }

    /// <summary>
    /// Primary data source for this price
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// JSON string containing source breakdown (List&lt;SourcePrice&gt;)
    /// </summary>
    public string? SourceBreakdownJson { get; set; }
}

