using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a risk management recommendation (deleveraging, return to baseline, etc.)
/// </summary>
public sealed class RiskRecommendation : BaseEntity
{
    /// <summary>
    /// Stock ticker symbol
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Position ID from perp DEX (nullable)
    /// </summary>
    public string? PositionId { get; set; }

    /// <summary>
    /// Recommended risk action
    /// </summary>
    public RiskAction Action { get; set; }

    /// <summary>
    /// Current leverage
    /// </summary>
    public decimal CurrentLeverage { get; set; }

    /// <summary>
    /// Target leverage
    /// </summary>
    public decimal TargetLeverage { get; set; }

    /// <summary>
    /// Reduction percentage (for deleveraging)
    /// </summary>
    public decimal? ReductionPercentage { get; set; }

    /// <summary>
    /// Increase percentage (for return to baseline)
    /// </summary>
    public decimal? IncreasePercentage { get; set; }

    /// <summary>
    /// Reason for the recommendation
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Priority level
    /// </summary>
    public Priority Priority { get; set; }

    /// <summary>
    /// When this recommendation was created
    /// </summary>
    public DateTime RecommendedBy { get; set; }

    /// <summary>
    /// When this recommendation is valid until (nullable)
    /// </summary>
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// Whether this recommendation has been acknowledged
    /// </summary>
    public bool Acknowledged { get; set; }

    /// <summary>
    /// When this recommendation was acknowledged (nullable)
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>
    /// User/avatar ID who acknowledged (nullable)
    /// </summary>
    public string? AcknowledgedBy { get; set; }
}

