using Domain.Enums;

namespace Application.DTOs.PriceAdjustment;

/// <summary>
/// Represents a price adjustment applied due to a corporate action
/// </summary>
public class PriceAdjustmentDto
{
    /// <summary>
    /// Corporate action that caused the adjustment
    /// </summary>
    public CorporateActionInfoDto CorporateAction { get; set; } = null!;

    /// <summary>
    /// Price before adjustment
    /// </summary>
    public decimal PriceBefore { get; set; }

    /// <summary>
    /// Price after adjustment
    /// </summary>
    public decimal PriceAfter { get; set; }

    /// <summary>
    /// Adjustment factor (multiplier)
    /// </summary>
    public decimal AdjustmentFactor { get; set; }

    /// <summary>
    /// Date when adjustment was applied
    /// </summary>
    public DateTime AppliedAt { get; set; }
}

/// <summary>
/// Information about a corporate action
/// </summary>
public class CorporateActionInfoDto
{
    /// <summary>
    /// Corporate action ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of corporate action
    /// </summary>
    public CorporateActionType Type { get; set; }

    /// <summary>
    /// Effective date
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Split ratio (if applicable)
    /// </summary>
    public decimal? SplitRatio { get; set; }

    /// <summary>
    /// Exchange ratio (if applicable)
    /// </summary>
    public decimal? ExchangeRatio { get; set; }
}

/// <summary>
/// Result of price adjustment calculation
/// </summary>
public class AdjustedPriceResultDto
{
    /// <summary>
    /// Stock ticker symbol
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Raw price before adjustments
    /// </summary>
    public decimal RawPrice { get; set; }

    /// <summary>
    /// Adjusted price after applying corporate actions
    /// </summary>
    public decimal AdjustedPrice { get; set; }

    /// <summary>
    /// Cumulative adjustment factor
    /// </summary>
    public decimal AdjustmentFactor { get; set; }

    /// <summary>
    /// Date of the price
    /// </summary>
    public DateTime PriceDate { get; set; }

    /// <summary>
    /// List of adjustments applied
    /// </summary>
    public List<PriceAdjustmentDto> AppliedAdjustments { get; set; } = new();
}

