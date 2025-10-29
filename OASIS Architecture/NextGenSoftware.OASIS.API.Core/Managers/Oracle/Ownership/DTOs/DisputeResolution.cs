using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;

/// <summary>
/// Result of automated dispute resolution using multi-oracle consensus.
/// Provides court-admissible evidence and resolution.
/// </summary>
public class DisputeResolution
{
    /// <summary>
    /// Unique identifier for this resolution
    /// </summary>
    public string ResolutionId { get; set; }

    /// <summary>
    /// Asset that was disputed
    /// </summary>
    public string AssetId { get; set; }

    /// <summary>
    /// Winner of the dispute (owner with valid claim)
    /// </summary>
    public string WinningClaimant { get; set; }

    /// <summary>
    /// When the winning claimant acquired ownership
    /// </summary>
    public DateTimeOffset ClaimTime { get; set; }

    /// <summary>
    /// Oracle consensus level supporting the resolution (0-100%)
    /// </summary>
    public int ConsensusLevel { get; set; }

    /// <summary>
    /// Blockchain evidence supporting the resolution
    /// </summary>
    public List<string> Evidence { get; set; } = new();

    /// <summary>
    /// Claims that were rejected
    /// </summary>
    public List<RejectedClaim> RejectedClaims { get; set; } = new();

    /// <summary>
    /// Explanation of why this resolution was reached
    /// </summary>
    public string ResolutionReason { get; set; }

    /// <summary>
    /// Whether this resolution is court-admissible
    /// </summary>
    public bool IsCourtAdmissible { get; set; }

    /// <summary>
    /// Complete blockchain proof package
    /// </summary>
    public BlockchainProof BlockchainProof { get; set; }

    /// <summary>
    /// List of oracle nodes that participated in resolution
    /// </summary>
    public List<string> OracleNodes { get; set; } = new();

    /// <summary>
    /// When this dispute was resolved
    /// </summary>
    public DateTimeOffset ResolvedAt { get; set; }

    /// <summary>
    /// How long resolution took
    /// </summary>
    public TimeSpan ResolutionTime { get; set; }

    /// <summary>
    /// Cost of resolution (vs traditional legal proceedings)
    /// </summary>
    public decimal ResolutionCost { get; set; }

    /// <summary>
    /// Estimated savings vs traditional legal route
    /// </summary>
    public decimal EstimatedSavings { get; set; }
}

/// <summary>
/// Represents a claim that was rejected during dispute resolution
/// </summary>
public class RejectedClaim
{
    public string ClaimantId { get; set; }
    public DateTimeOffset ClaimTime { get; set; }
    public string RejectionReason { get; set; }
    public int ConsensusLevel { get; set; }  // Low consensus = rejected
}

/// <summary>
/// Blockchain proof package for court proceedings
/// </summary>
public class BlockchainProof
{
    public List<string> TransactionHashes { get; set; } = new();
    public List<long> BlockNumbers { get; set; } = new();
    public List<string> OracleSignatures { get; set; } = new();
    public string MerkleRoot { get; set; }
    public List<string> MerkleProof { get; set; } = new();
    public DateTimeOffset GeneratedAt { get; set; }
    public bool IsTamperProof { get; set; } = true;
}

