namespace Application.DTOs.RiskManagement.Responses;

/// <summary>
/// Response DTO for return-to-baseline recommendations
/// </summary>
public sealed record ReturnToBaselineRecommendationsResponseDto
{
    /// <summary>
    /// List of return-to-baseline recommendations
    /// </summary>
    public List<RiskRecommendationResponseDto> Recommendations { get; init; } = new();
}

