namespace Domain.Enums;

/// <summary>
/// Types of risk factors that contribute to risk windows
/// </summary>
public enum RiskFactorType
{
    /// <summary>
    /// Corporate action (split, dividend, merger, etc.)
    /// </summary>
    CorporateAction = 0,

    /// <summary>
    /// High volatility in price
    /// </summary>
    HighVolatility = 1,

    /// <summary>
    /// Low liquidity conditions
    /// </summary>
    LowLiquidity = 2,

    /// <summary>
    /// Large position size relative to market
    /// </summary>
    LargePosition = 3,

    /// <summary>
    /// Market-wide event (earnings, news, etc.)
    /// </summary>
    MarketEvent = 4,

    /// <summary>
    /// Regulatory event or announcement
    /// </summary>
    RegulatoryEvent = 5
}

