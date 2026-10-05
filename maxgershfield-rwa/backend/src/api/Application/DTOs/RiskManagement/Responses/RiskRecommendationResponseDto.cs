namespace Application.DTOs.RiskManagement.Responses;

/// <summary>
/// Detailed API response DTO for risk recommendation
/// </summary>
public sealed record RiskRecommendationResponseDto
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
    /// Recommended risk action as string
    /// </summary>
    public string Action { get; init; } = string.Empty;

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
    /// Priority level as string
    /// </summary>
    public string Priority { get; init; } = string.Empty;

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
    /// Related risk factors
    /// </summary>
    public List<RiskFactorResponseDto> RelatedFactors { get; init; } = new();
}

