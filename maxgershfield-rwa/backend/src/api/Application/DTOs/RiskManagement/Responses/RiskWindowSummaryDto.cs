namespace Application.DTOs.RiskManagement.Responses;

/// <summary>
/// Summary DTO for risk window (used in nested responses)
/// </summary>
public sealed record RiskWindowSummaryDto
{
    /// <summary>
    /// Risk window ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Start date of the risk window
    /// </summary>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// End date of the risk window
    /// </summary>
    public DateTime EndDate { get; init; }

    /// <summary>
    /// Risk level as string
    /// </summary>
    public string Level { get; init; } = string.Empty;
}

