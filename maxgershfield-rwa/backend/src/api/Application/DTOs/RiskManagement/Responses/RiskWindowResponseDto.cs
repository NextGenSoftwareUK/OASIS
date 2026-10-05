namespace Application.DTOs.RiskManagement.Responses;

/// <summary>
/// API response DTO for risk window query
/// </summary>
public sealed record RiskWindowResponseDto
{
    /// <summary>
    /// Stock ticker symbol
    /// </summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Whether there is an active risk window
    /// </summary>
    public bool HasActiveWindow { get; init; }

    /// <summary>
    /// Risk window details (if active)
    /// </summary>
    public RiskWindowDetailDto? RiskWindow { get; init; }
}

