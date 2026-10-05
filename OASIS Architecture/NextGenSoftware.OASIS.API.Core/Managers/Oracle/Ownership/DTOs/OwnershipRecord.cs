using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Represents current ownership status of an asset with multi-oracle consensus.
/// Answers: "Who owns this asset right now?"
/// </summary>
public class OwnershipRecord
{
    /// <summary>
    /// Unique identifier for the asset (NFT ID, token address, etc.)
    /// </summary>
    public string AssetId { get; set; }

    /// <summary>
    /// Current owner identifier (Avatar ID, wallet address, institution ID)
    /// </summary>
    public string CurrentOwner { get; set; }

    /// <summary>
    /// Human-readable owner name (if available)
    /// </summary>
    public string OwnerName { get; set; }

    /// <summary>
    /// Current market value of the asset (real-time from price oracles)
    /// </summary>
    public decimal CurrentValue { get; set; }

    /// <summary>
    /// Currency of valuation (USD, EUR, etc.)
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// When the last ownership transfer occurred
    /// </summary>
    public DateTimeOffset LastTransferTime { get; set; }

    /// <summary>
    /// Transaction hash of last transfer (blockchain proof)
    /// </summary>
    public string LastTransferTxHash { get; set; }

    /// <summary>
    /// Encumbrance status (is it pledged, locked, liened?)
    /// </summary>
    public EncumbranceStatus Encumbrance { get; set; }

    /// <summary>
    /// List of chains where this asset exists (could be multi-chain)
    /// </summary>
    public List<ProviderType> LocationChains { get; set; }

    /// <summary>
    /// Oracle consensus level (0-100%)
    /// 100% = all oracles agree, <80% = flagged for review
    /// </summary>
    public int ConsensusLevel { get; set; }

    /// <summary>
    /// Whether there's a dispute about ownership
    /// </summary>
    public bool IsDisputed { get; set; }

    /// <summary>
    /// Asset type (US Treasuries, Corporate Bonds, Real Estate, etc.)
    /// </summary>
    public string AssetType { get; set; }

    /// <summary>
    /// Asset metadata (name, description, etc.)
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// Timestamp when this record was generated
    /// </summary>
    public DateTimeOffset RecordTimestamp { get; set; }

    /// <summary>
    /// List of oracle nodes that verified this record
    /// </summary>
    public List<string> VerifiedByOracles { get; set; }

    /// <summary>
    /// Whether this record is from a historical query (time-travel)
    /// </summary>
    public bool IsHistoricalRecord { get; set; }

    /// <summary>
    /// If historical, what timestamp was queried
    /// </summary>
    public DateTimeOffset? AsOfTime { get; set; }
}






