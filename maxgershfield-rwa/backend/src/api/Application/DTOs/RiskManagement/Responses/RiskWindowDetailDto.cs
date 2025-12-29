namespace Application.DTOs.RiskManagement.Responses;

/// <summary>
/// Detailed DTO for risk window
/// </summary>
public sealed record RiskWindowDetailDto
{
    /// <summary>
    /// Risk window ID
    /// </summary>
    public Guid Id { get; init; }

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

    /// <summary>
    /// When this risk window was created
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

