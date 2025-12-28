using Domain.Enums;

namespace Application.DTOs.RiskManagement;

/// <summary>
/// Represents a risk assessment for a symbol or position
/// </summary>
public sealed record RiskAssessmentDto
{
    /// <summary>
    /// Stock ticker symbol
    /// </summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Risk level (Low, Medium, High, Critical)
    /// </summary>
    public RiskLevel Level { get; init; }

    /// <summary>
    /// Current leverage
    /// </summary>
    public decimal CurrentLeverage { get; init; }

    /// <summary>
    /// Recommended leverage based on risk level
    /// </summary>
    public decimal RecommendedLeverage { get; init; }

    /// <summary>
    /// Risk score (0-100 scale)
    /// </summary>
    public decimal RiskScore { get; init; }

    /// <summary>
    /// Risk factors contributing to this assessment
    /// </summary>
    public List<RiskFactorDto> Factors { get; init; } = new();

    /// <summary>
    /// Active risk window (if any)
    /// </summary>
    public RiskWindowDto? ActiveRiskWindow { get; init; }

    /// <summary>
    /// Risk recommendations (if any)
    /// </summary>
    public List<RiskRecommendationDto> Recommendations { get; init; } = new();

    /// <summary>
    /// When this assessment was performed
    /// </summary>
    public DateTime AssessedAt { get; init; }
}

