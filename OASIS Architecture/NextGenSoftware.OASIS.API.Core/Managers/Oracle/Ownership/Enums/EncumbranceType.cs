namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Types of encumbrances that can be placed on assets
/// </summary>
public enum EncumbranceType
{
    /// <summary>
    /// Repurchase agreement - temporary pledge with buyback obligation
    /// </summary>
    Repo,

    /// <summary>
    /// Derivative swap collateral
    /// </summary>
    Swap,

    /// <summary>
    /// Loan collateral
    /// </summary>
    Loan,

    /// <summary>
    /// Legal lien (e.g., tax lien, judgment lien)
    /// </summary>
    Lien,

    /// <summary>
    /// Temporary lock (e.g., during transfer, escrow)
    /// </summary>
    Lock,

    /// <summary>
    /// Margin/trading collateral
    /// </summary>
    Margin,

    /// <summary>
    /// Derivatives collateral (futures, options)
    /// </summary>
    Derivatives,

    /// <summary>
    /// Other encumbrance type
    /// </summary>
    Other
}

