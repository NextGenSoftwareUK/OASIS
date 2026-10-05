namespace Domain.Enums;

/// <summary>
/// Priority levels for risk recommendations
/// </summary>
public enum Priority
{
    /// <summary>
    /// Low priority - can be addressed gradually
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium priority - should be addressed soon
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High priority - should be addressed immediately
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority - requires immediate action
    /// </summary>
    Critical = 3
}

