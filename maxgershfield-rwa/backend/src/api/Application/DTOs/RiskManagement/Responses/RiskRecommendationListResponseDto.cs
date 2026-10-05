namespace Application.DTOs.RiskManagement.Responses;

/// <summary>
/// Paginated list response DTO for risk recommendations
/// </summary>
public sealed record RiskRecommendationListResponseDto
{
    /// <summary>
    /// Stock ticker symbol
    /// </summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// List of recommendations
    /// </summary>
    public List<RiskRecommendationResponseDto> Recommendations { get; init; } = new();

    /// <summary>
    /// Total count of recommendations
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; init; }
}

