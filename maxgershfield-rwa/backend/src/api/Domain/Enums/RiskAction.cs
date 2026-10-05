namespace Domain.Enums;

/// <summary>
/// Types of risk management actions recommended
/// </summary>
public enum RiskAction
{
    /// <summary>
    /// Immediate deleveraging required
    /// </summary>
    Deleverage = 0,

    /// <summary>
    /// Gradual deleveraging over time
    /// </summary>
    GradualDeleverage = 1,

    /// <summary>
    /// Return to baseline leverage after risk window
    /// </summary>
    ReturnToBaseline = 2,

    /// <summary>
    /// Close position entirely (for critical risk)
    /// </summary>
    ClosePosition = 3
}

