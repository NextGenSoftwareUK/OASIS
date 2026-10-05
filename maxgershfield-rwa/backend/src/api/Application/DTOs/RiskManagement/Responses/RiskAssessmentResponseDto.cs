using Domain.Enums;

namespace Application.DTOs.RiskManagement.Responses;

/// <summary>
/// API response DTO for risk assessment
/// </summary>
public sealed record RiskAssessmentResponseDto
{
    /// <summary>
    /// Stock ticker symbol
    /// </summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Risk level as string
    /// </summary>
    public string Level { get; init; } = string.Empty;

    /// <summary>
    /// Risk score (0-100 scale)
    /// </summary>
    public decimal RiskScore { get; init; }

    /// <summary>
    /// Current leverage
    /// </summary>
    public decimal CurrentLeverage { get; init; }

    /// <summary>
    /// Recommended leverage based on risk level
    /// </summary>
    public decimal RecommendedLeverage { get; init; }

    /// <summary>
    /// Risk factors contributing to this assessment
    /// </summary>
    public List<RiskFactorResponseDto> Factors { get; init; } = new();

    /// <summary>
    /// Active risk window summary (if any)
    /// </summary>
    public RiskWindowSummaryDto? ActiveRiskWindow { get; init; }

    /// <summary>
    /// Risk recommendations (if any)
    /// </summary>
    public List<RiskRecommendationSummaryDto> Recommendations { get; init; } = new();

    /// <summary>
    /// When this assessment was performed
    /// </summary>
    public DateTime AssessedAt { get; init; }
}

