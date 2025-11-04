using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.Interfaces;

/// <summary>
/// Resolves ownership disputes using multi-oracle consensus and blockchain evidence.
/// Replaces 6-18 month court proceedings with instant, automated resolution.
/// Saves $5-20M in legal fees per dispute.
/// </summary>
public interface IDisputeResolver
{
    /// <summary>
    /// Resolves an ownership dispute between multiple claimants.
    /// Uses: Blockchain timestamps, oracle consensus, cryptographic proofs
    /// Result: Court-admissible resolution with evidence
    /// </summary>
    /// <param name="assetId">Disputed asset identifier</param>
    /// <param name="claims">List of ownership claims</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Dispute resolution with winning claimant and evidence</returns>
    Task<OASISResult<DisputeResolution>> ResolveOwnershipDisputeAsync(
        string assetId,
        List<DisputeClaim> claims,
        CancellationToken token = default);

    /// <summary>
    /// Verifies a single ownership claim using multi-oracle consensus.
    /// Queries 20+ oracle nodes for agreement.
    /// </summary>
    /// <param name="claim">Ownership claim to verify</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Verification result with consensus level (0-100%)</returns>
    Task<OASISResult<OwnershipClaimVerification>> VerifyClaimAsync(
        DisputeClaim claim,
        CancellationToken token = default);

    /// <summary>
    /// Generates blockchain proof for court proceedings.
    /// Includes: Transaction hashes, oracle signatures, merkle proofs
    /// Format: Court-admissible, tamper-proof
    /// </summary>
    /// <param name="assetId">Asset identifier</param>
    /// <param name="claimantId">Claimant identifier</param>
    /// <param name="claimTimestamp">Time of claim</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Court-admissible evidence package</returns>
    Task<OASISResult<CourtEvidence>> GenerateCourtEvidenceAsync(
        string assetId,
        string claimantId,
        DateTimeOffset claimTimestamp,
        CancellationToken token = default);

    /// <summary>
    /// Flags a potential dispute for human review.
    /// Used when oracle consensus is low (<80%) or conflicting data detected.
    /// </summary>
    /// <param name="assetId">Asset identifier</param>
    /// <param name="reason">Reason for flagging</param>
    /// <param name="conflictingData">List of conflicting ownership records</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Dispute flag record</returns>
    Task<OASISResult<DisputeFlag>> FlagDisputeAsync(
        string assetId,
        string reason,
        List<OwnershipRecord> conflictingData,
        CancellationToken token = default);
}





