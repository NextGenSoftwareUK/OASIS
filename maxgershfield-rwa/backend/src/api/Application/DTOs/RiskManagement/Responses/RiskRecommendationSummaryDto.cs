namespace Application.DTOs.RiskManagement.Responses;

/// <summary>
/// Summary DTO for risk recommendation (used in nested responses)
/// </summary>
public sealed record RiskRecommendationSummaryDto
{
    /// <summary>
    /// Recommendation ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Recommended risk action as string
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// Target leverage
    /// </summary>
    public decimal TargetLeverage { get; init; }

    /// <summary>
    /// Reduction percentage (for deleveraging, nullable)
    /// </summary>
    public decimal? ReductionPercentage { get; init; }

    /// <summary>
    /// Reason for the recommendation
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Priority level as string
    /// </summary>
    public string Priority { get; init; } = string.Empty;
}

