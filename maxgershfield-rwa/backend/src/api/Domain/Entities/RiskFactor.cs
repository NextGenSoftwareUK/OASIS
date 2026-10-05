using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a risk factor contributing to a risk window
/// </summary>
public sealed class RiskFactor : BaseEntity
{
    /// <summary>
    /// Foreign key to the risk window
    /// </summary>
    public Guid RiskWindowId { get; set; }

    /// <summary>
    /// Type of risk factor
    /// </summary>
    public RiskFactorType Type { get; set; }

    /// <summary>
    /// Description of the risk factor
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Impact score (0-1 scale)
    /// </summary>
    public decimal Impact { get; set; }

    /// <summary>
    /// Effective date of this risk factor
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Additional details in JSON format
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Navigation property to the risk window
    /// </summary>
    public RiskWindow RiskWindow { get; set; } = null!;
}

