using Domain.Enums;

namespace Application.DTOs.RiskManagement;

/// <summary>
/// Represents a risk window (period of elevated risk for a symbol)
/// </summary>
public sealed record RiskWindowDto
{
    /// <summary>
    /// Risk window ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Stock ticker symbol
    /// </summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    /// Risk level for this window
    /// </summary>
    public RiskLevel Level { get; init; }

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
    public List<RiskFactorDto> Factors { get; init; } = new();

    /// <summary>
    /// When this risk window was created
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

