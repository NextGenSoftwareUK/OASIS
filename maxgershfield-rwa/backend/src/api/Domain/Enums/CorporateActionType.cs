namespace Domain.Enums;

/// <summary>
/// Types of corporate actions that can affect equity prices
/// </summary>
public enum CorporateActionType
{
    /// <summary>
    /// Stock split (e.g., 2-for-1, 4-for-1)
    /// </summary>
    StockSplit = 0,

    /// <summary>
    /// Reverse stock split (e.g., 1-for-5)
    /// </summary>
    ReverseSplit = 1,

    /// <summary>
    /// Regular dividend payment
    /// </summary>
    Dividend = 2,

    /// <summary>
    /// Special one-time dividend
    /// </summary>
    SpecialDividend = 3,

    /// <summary>
    /// Merger with another company
    /// </summary>
    Merger = 4,

    /// <summary>
    /// Acquisition of another company
    /// </summary>
    Acquisition = 5,

    /// <summary>
    /// Spinoff of a subsidiary
    /// </summary>
    Spinoff = 6,

    /// <summary>
    /// Stock dividend (paid in shares)
    /// </summary>
    StockDividend = 7,

    /// <summary>
    /// Rights issue to existing shareholders
    /// </summary>
    RightsIssue = 8
}

