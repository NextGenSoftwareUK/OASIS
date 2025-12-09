using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Represents the encumbrance status of an asset.
/// Answers: "Is this asset available or locked?"
/// </summary>
public class EncumbranceStatus
{
    /// <summary>
    /// Whether the asset has any active encumbrances
    /// </summary>
    public bool IsEncumbered { get; set; }

    /// <summary>
    /// List of all active encumbrances on this asset
    /// </summary>
    public List<Encumbrance> ActiveEncumbrances { get; set; } = new();

    /// <summary>
    /// Total value that is encumbered
    /// </summary>
    public decimal TotalEncumberedValue { get; set; }

    /// <summary>
    /// Total value of the asset
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Value that is available (not encumbered)
    /// </summary>
    public decimal AvailableValue { get; set; }

    /// <summary>
    /// Percentage of asset that is encumbered
    /// </summary>
    public decimal UtilizationRate => TotalValue > 0 
        ? (TotalEncumberedValue / TotalValue) * 100 
        : 0;

    /// <summary>
    /// Whether the asset is fully available (no encumbrances)
    /// </summary>
    public bool IsFullyAvailable => !IsEncumbered && AvailableValue == TotalValue;

    /// <summary>
    /// Whether the asset is fully encumbered (no available value)
    /// </summary>
    public bool IsFullyEncumbered => IsEncumbered && AvailableValue <= 0;

    /// <summary>
    /// Number of active encumbrances
    /// </summary>
    public int EncumbranceCount => ActiveEncumbrances?.Count ?? 0;

    /// <summary>
    /// Highest priority encumbrance (first lien)
    /// </summary>
    public Encumbrance FirstLien => ActiveEncumbrances?
        .OrderBy(e => e.Priority)
        .FirstOrDefault();

    /// <summary>
    /// Next encumbrance to mature (soonest maturity date)
    /// </summary>
    public Encumbrance NextToMature => ActiveEncumbrances?
        .Where(e => e.MaturityTime > System.DateTimeOffset.Now)
        .OrderBy(e => e.MaturityTime)
        .FirstOrDefault();
}






