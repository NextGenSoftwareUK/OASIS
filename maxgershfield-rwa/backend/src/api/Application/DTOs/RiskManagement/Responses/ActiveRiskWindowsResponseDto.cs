namespace Application.DTOs.RiskManagement.Responses;

/// <summary>
/// Response DTO for list of active risk windows
/// </summary>
public sealed record ActiveRiskWindowsResponseDto
{
    /// <summary>
    /// List of active risk windows
    /// </summary>
    public List<ActiveRiskWindowItemDto> RiskWindows { get; init; } = new();

    /// <summary>
    /// Total count
    /// </summary>
    public int TotalCount { get; init; }
}

/// <summary>
/// Active risk window item DTO
/// </summary>
public sealed record ActiveRiskWindowItemDto
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
    /// Start date of the risk window
    /// </summary>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// End date of the risk window
    /// </summary>
    public DateTime EndDate { get; init; }

    /// <summary>
    /// Risk factors contributing to this window
    /// </summary>
    public List<RiskFactorResponseDto> Factors { get; init; } = new();
}

