using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Represents a funding rate calculation for perpetual futures
/// </summary>
public sealed class FundingRate : BaseEntity
{
    /// <summary>
    /// Stock ticker symbol
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Annualized funding rate percentage
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    /// Hourly funding rate
    /// </summary>
    public decimal HourlyRate { get; set; }

    /// <summary>
    /// Mark price (perpetual futures price)
    /// </summary>
    public decimal MarkPrice { get; set; }

    /// <summary>
    /// Spot price (raw)
    /// </summary>
    public decimal SpotPrice { get; set; }

    /// <summary>
    /// Adjusted spot price (after corporate action adjustments)
    /// </summary>
    public decimal AdjustedSpotPrice { get; set; }

    /// <summary>
    /// Premium (MarkPrice - AdjustedSpotPrice, can be negative)
    /// </summary>
    public decimal Premium { get; set; }

    /// <summary>
    /// Premium percentage ((Premium / AdjustedSpotPrice) * 100)
    /// </summary>
    public decimal PremiumPercentage { get; set; }

    /// <summary>
    /// Base funding rate (from premium)
    /// </summary>
    public decimal BaseRate { get; set; }

    /// <summary>
    /// Corporate action adjustment to funding rate
    /// </summary>
    public decimal CorporateActionAdjustment { get; set; }

    /// <summary>
    /// Liquidity adjustment to funding rate
    /// </summary>
    public decimal LiquidityAdjustment { get; set; }

    /// <summary>
    /// Volatility adjustment to funding rate
    /// </summary>
    public decimal VolatilityAdjustment { get; set; }

    /// <summary>
    /// When this funding rate was calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; }

    /// <summary>
    /// When this funding rate is valid until (usually +1 hour)
    /// </summary>
    public DateTime ValidUntil { get; set; }

    /// <summary>
    /// Solana transaction hash if published on-chain
    /// </summary>
    public string? OnChainTransactionHash { get; set; }
}

