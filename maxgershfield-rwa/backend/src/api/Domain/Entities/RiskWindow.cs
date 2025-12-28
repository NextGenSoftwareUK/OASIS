using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a risk window (period of elevated risk for a symbol)
/// </summary>
public sealed class RiskWindow : BaseEntity
{
    /// <summary>
    /// Stock ticker symbol
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// Risk level for this window
    /// </summary>
    public RiskLevel Level { get; set; }

    /// <summary>
    /// Start date of the risk window
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the risk window
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Risk factors contributing to this window
    /// </summary>
    public ICollection<RiskFactor> Factors { get; set; } = [];
}

