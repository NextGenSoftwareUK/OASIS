namespace Domain.Enums;

/// <summary>
/// Risk levels for risk windows and assessments
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Low risk - normal trading conditions
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium risk - increased volatility expected
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High risk - significant volatility or corporate action expected
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical risk - extreme volatility or major corporate action imminent
    /// </summary>
    Critical = 3
}

