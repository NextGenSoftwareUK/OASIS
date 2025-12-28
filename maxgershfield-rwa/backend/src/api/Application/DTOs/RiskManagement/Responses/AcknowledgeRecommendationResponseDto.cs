namespace Application.DTOs.RiskManagement.Responses;

/// <summary>
/// Response DTO for acknowledging a recommendation
/// </summary>
public sealed record AcknowledgeRecommendationResponseDto
{
    /// <summary>
    /// Recommendation ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Whether the recommendation has been acknowledged
    /// </summary>
    public bool Acknowledged { get; init; }

    /// <summary>
    /// When this recommendation was acknowledged
    /// </summary>
    public DateTime AcknowledgedAt { get; init; }

    /// <summary>
    /// User/avatar ID who acknowledged (nullable)
    /// </summary>
    public string? AcknowledgedBy { get; init; }
}

