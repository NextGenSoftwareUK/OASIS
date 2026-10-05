using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Represents a claim to ownership in a dispute.
/// Used when multiple parties claim the same asset.
/// </summary>
public class DisputeClaim
{
    /// <summary>
    /// Unique identifier for this claim
    /// </summary>
    public string ClaimId { get; set; }

    /// <summary>
    /// Asset being claimed
    /// </summary>
    public string AssetId { get; set; }

    /// <summary>
    /// Who is making the claim
    /// </summary>
    public string ClaimantId { get; set; }

    /// <summary>
    /// Claimant name
    /// </summary>
    public string ClaimantName { get; set; }

    /// <summary>
    /// When the claimant says they acquired ownership
    /// </summary>
    public DateTimeOffset ClaimTime { get; set; }

    /// <summary>
    /// Transaction hash supporting the claim
    /// </summary>
    public string TransactionHash { get; set; }

    /// <summary>
    /// Blockchain where transaction occurred
    /// </summary>
    public string Chain { get; set; }

    /// <summary>
    /// Priority claimed (1 = first lien, 2 = second lien, etc.)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Amount claimed
    /// </summary>
    public decimal AmountClaimed { get; set; }

    /// <summary>
    /// Type of claim (ownership, lien, pledge, etc.)
    /// </summary>
    public string ClaimType { get; set; }

    /// <summary>
    /// Supporting evidence/documentation
    /// </summary>
    public List<string> Evidence { get; set; } = new();

    /// <summary>
    /// Legal basis for claim
    /// </summary>
    public string LegalBasis { get; set; }

    /// <summary>
    /// When this claim was filed
    /// </summary>
    public DateTimeOffset FiledAt { get; set; }

    /// <summary>
    /// Additional notes or comments
    /// </summary>
    public string Notes { get; set; }
}






