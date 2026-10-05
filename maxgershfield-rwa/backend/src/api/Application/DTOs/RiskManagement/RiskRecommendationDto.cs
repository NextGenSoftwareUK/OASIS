using Domain.Enums;

namespace Application.DTOs.RiskManagement;

/// <summary>
/// Represents a risk management recommendation
/// </summary>
public sealed record RiskRecommendationDto
{
    /// <summary>
    /// Recommendation ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Stock ticker symbol
    /// </summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Position ID from perp DEX (nullable)
    /// </summary>
    public string? PositionId { get; init; }

    /// <summary>
    /// Recommended risk action
    /// </summary>
    public RiskAction Action { get; init; }

    /// <summary>
    /// Current leverage
    /// </summary>
    public decimal CurrentLeverage { get; init; }

    /// <summary>
    /// Target leverage
    /// </summary>
    public decimal TargetLeverage { get; init; }

    /// <summary>
    /// Reduction percentage (for deleveraging, nullable)
    /// </summary>
    public decimal? ReductionPercentage { get; init; }

    /// <summary>
    /// Increase percentage (for return to baseline, nullable)
    /// </summary>
    public decimal? IncreasePercentage { get; init; }

    /// <summary>
    /// Reason for the recommendation
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Priority level
    /// </summary>
    public Priority Priority { get; init; }

    /// <summary>
    /// When this recommendation was created
    /// </summary>
    public DateTime RecommendedBy { get; init; }

    /// <summary>
    /// When this recommendation is valid until (nullable)
    /// </summary>
    public DateTime? ValidUntil { get; init; }

    /// <summary>
    /// Whether this recommendation has been acknowledged
    /// </summary>
    public bool Acknowledged { get; init; }

    /// <summary>
    /// When this recommendation was acknowledged (nullable)
    /// </summary>
    public DateTime? AcknowledgedAt { get; init; }

    /// <summary>
    /// User/avatar ID who acknowledged (nullable)
    /// </summary>
    public string? AcknowledgedBy { get; init; }

    /// <summary>
    /// Related risk factors
    /// </summary>
    public List<RiskFactorDto> RelatedFactors { get; init; } = new();
}

