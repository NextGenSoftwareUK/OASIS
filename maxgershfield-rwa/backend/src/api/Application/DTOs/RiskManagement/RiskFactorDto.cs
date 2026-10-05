using Domain.Enums;

namespace Application.DTOs.RiskManagement;

/// <summary>
/// Represents a risk factor contributing to a risk window
/// </summary>
public sealed record RiskFactorDto
{
    /// <summary>
    /// Type of risk factor
    /// </summary>
    public RiskFactorType Type { get; init; }

    /// <summary>
    /// Description of the risk factor
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Impact score (0-1 scale)
    /// </summary>
    public decimal Impact { get; init; }

    /// <summary>
    /// Effective date of this risk factor
    /// </summary>
    public DateTime EffectiveDate { get; init; }

    /// <summary>
    /// Additional details in JSON format (nullable)
    /// </summary>
    public string? Details { get; init; }
}

