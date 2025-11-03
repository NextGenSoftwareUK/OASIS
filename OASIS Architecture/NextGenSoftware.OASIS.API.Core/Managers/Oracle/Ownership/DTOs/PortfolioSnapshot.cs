using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Complete portfolio snapshot at a specific point in time.
/// Critical for: Regulatory reporting, audits, quarter-end positions
/// </summary>
public class PortfolioSnapshot
{
    /// <summary>
    /// Owner identifier
    /// </summary>
    public string OwnerId { get; set; }

    /// <summary>
    /// Owner name
    /// </summary>
    public string OwnerName { get; set; }

    /// <summary>
    /// Snapshot timestamp
    /// </summary>
    public DateTimeOffset SnapshotTime { get; set; }

    /// <summary>
    /// All assets owned at snapshot time
    /// </summary>
    public List<AssetOwnership> Assets { get; set; } = new();

    /// <summary>
    /// Total portfolio value at snapshot time
    /// </summary>
    public decimal TotalValue => Assets?.Sum(a => a.Value) ?? 0;

    /// <summary>
    /// Available (unencumbered) value
    /// </summary>
    public decimal AvailableValue => Assets?
        .Where(a => !a.IsEncumbered)
        .Sum(a => a.Value) ?? 0;

    /// <summary>
    /// Encumbered (pledged) value
    /// </summary>
    public decimal EncumberedValue => Assets?
        .Where(a => a.IsEncumbered)
        .Sum(a => a.Value) ?? 0;

    /// <summary>
    /// Breakdown by asset type
    /// </summary>
    public Dictionary<string, decimal> ByAssetType { get; set; } = new();

    /// <summary>
    /// Breakdown by chain
    /// </summary>
    public Dictionary<string, decimal> ByChain { get; set; } = new();

    /// <summary>
    /// Active encumbrances at snapshot time
    /// </summary>
    public List<Encumbrance> ActiveEncumbrances { get; set; } = new();

    /// <summary>
    /// Utilization rate (encumbered / total)
    /// </summary>
    public decimal UtilizationRate => TotalValue > 0 
        ? (EncumberedValue / TotalValue) * 100 
        : 0;

    /// <summary>
    /// Oracle consensus level for this snapshot (0-100%)
    /// </summary>
    public int ConsensusLevel { get; set; }

    /// <summary>
    /// Whether this snapshot is court-admissible
    /// </summary>
    public bool IsCourtAdmissible { get; set; }

    /// <summary>
    /// Blockchain proof supporting this snapshot
    /// </summary>
    public BlockchainProof Proof { get; set; }

    /// <summary>
    /// When this snapshot was generated
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; }
}





