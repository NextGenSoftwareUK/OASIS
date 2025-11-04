using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Represents a pledge, lien, or lock on an asset.
/// Critical for: Understanding what collateral is actually available
/// </summary>
public class Encumbrance
{
    /// <summary>
    /// Unique identifier for this encumbrance
    /// </summary>
    public string EncumbranceId { get; set; }

    /// <summary>
    /// Asset being encumbered
    /// </summary>
    public string AssetId { get; set; }

    /// <summary>
    /// Type of encumbrance (Repo, Swap, Loan, Lien, Lock)
    /// </summary>
    public EncumbranceType Type { get; set; }

    /// <summary>
    /// Who owns the asset (pledgor)
    /// </summary>
    public string Owner { get; set; }

    /// <summary>
    /// Who the asset is pledged to (pledgee)
    /// </summary>
    public string Counterparty { get; set; }

    /// <summary>
    /// Amount encumbered (could be partial)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// When the encumbrance was created
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// When the encumbrance matures/expires
    /// </summary>
    public DateTimeOffset MaturityTime { get; set; }

    /// <summary>
    /// Priority level (1 = first lien, 2 = second lien, etc.)
    /// Lower number = higher priority in bankruptcy
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Transaction hash of pledge transaction
    /// </summary>
    public string TransactionHash { get; set; }

    /// <summary>
    /// Chain where the encumbrance is recorded
    /// </summary>
    public ProviderType Chain { get; set; }

    /// <summary>
    /// Smart contract address managing this encumbrance
    /// </summary>
    public string SmartContractAddress { get; set; }

    /// <summary>
    /// Whether this encumbrance is still active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Interest rate (if applicable, e.g., for repo)
    /// </summary>
    public decimal? InterestRate { get; set; }

    /// <summary>
    /// Haircut applied (percentage reduction in collateral value)
    /// </summary>
    public decimal Haircut { get; set; }

    /// <summary>
    /// Whether auto-release is enabled on maturity
    /// </summary>
    public bool AutoRelease { get; set; }

    /// <summary>
    /// Additional terms or conditions
    /// </summary>
    public Dictionary<string, string> Terms { get; set; }

    /// <summary>
    /// When this record was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When this record was last updated
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Helper: Check if encumbrance has matured
    /// </summary>
    public bool HasMatured => DateTimeOffset.Now >= MaturityTime;

    /// <summary>
    /// Helper: Time until maturity
    /// </summary>
    public TimeSpan TimeUntilMaturity => MaturityTime - DateTimeOffset.Now;
}





