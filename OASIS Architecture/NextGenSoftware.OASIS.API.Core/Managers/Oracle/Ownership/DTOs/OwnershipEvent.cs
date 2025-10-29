using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Represents a change in ownership or encumbrance status.
/// Immutable record of "what happened when" for audit trails.
/// </summary>
public class OwnershipEvent
{
    /// <summary>
    /// Unique identifier for this event
    /// </summary>
    public string EventId { get; set; }

    /// <summary>
    /// Type of ownership event
    /// </summary>
    public OwnershipEventType EventType { get; set; }

    /// <summary>
    /// Asset identifier
    /// </summary>
    public string AssetId { get; set; }

    /// <summary>
    /// Previous owner (for transfers)
    /// </summary>
    public string FromOwner { get; set; }

    /// <summary>
    /// New owner (for transfers)
    /// </summary>
    public string ToOwner { get; set; }

    /// <summary>
    /// Asset value at time of event
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Blockchain where event occurred
    /// </summary>
    public ProviderType Chain { get; set; }

    /// <summary>
    /// Transaction hash (blockchain proof)
    /// </summary>
    public string TransactionHash { get; set; }

    /// <summary>
    /// Block number
    /// </summary>
    public long BlockNumber { get; set; }

    /// <summary>
    /// Exact timestamp of event
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// For pledge events: who is the counterparty?
    /// </summary>
    public string Counterparty { get; set; }

    /// <summary>
    /// For pledge events: what type?
    /// </summary>
    public EncumbranceType? EncumbranceType { get; set; }

    /// <summary>
    /// For pledge events: when does it mature?
    /// </summary>
    public DateTimeOffset? MaturityTime { get; set; }

    /// <summary>
    /// Oracle nodes that verified this event
    /// </summary>
    public List<string> VerifiedBy { get; set; } = new();

    /// <summary>
    /// Consensus level for this event (0-100%)
    /// </summary>
    public int ConsensusLevel { get; set; }

    /// <summary>
    /// Whether this event was flagged for dispute
    /// </summary>
    public bool IsFlagged { get; set; }

    /// <summary>
    /// Additional event metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// When this event record was created in the oracle system
    /// </summary>
    public DateTimeOffset RecordedAt { get; set; }
}

/// <summary>
/// Types of ownership events tracked by the oracle
/// </summary>
public enum OwnershipEventType
{
    /// <summary>
    /// Asset transferred from one owner to another
    /// </summary>
    Transfer,

    /// <summary>
    /// Asset pledged as collateral
    /// </summary>
    Pledge,

    /// <summary>
    /// Pledge released (matured or repaid)
    /// </summary>
    Release,

    /// <summary>
    /// Asset locked (e.g., during escrow)
    /// </summary>
    Lock,

    /// <summary>
    /// Lock removed
    /// </summary>
    Unlock,

    /// <summary>
    /// Asset created/minted
    /// </summary>
    Mint,

    /// <summary>
    /// Asset burned/destroyed
    /// </summary>
    Burn,

    /// <summary>
    /// Asset liquidated (forced sale)
    /// </summary>
    Liquidation,

    /// <summary>
    /// Dispute flagged
    /// </summary>
    DisputeFlagged,

    /// <summary>
    /// Dispute resolved
    /// </summary>
    DisputeResolved
}

