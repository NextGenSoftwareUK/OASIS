using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.Interfaces;

/// <summary>
/// Provides time-travel queries for ownership tracking.
/// Critical for: Regulatory audits, dispute resolution, court cases
/// Enables: "Who owned asset X at time Y?" queries with blockchain proof
/// </summary>
public interface IOwnershipTimeOracle
{
    /// <summary>
    /// Gets the owner of an asset at a specific point in time.
    /// Uses blockchain history to provide tamper-proof evidence.
    /// Critical for: Dispute resolution, regulatory inquiries
    /// </summary>
    /// <param name="assetId">Asset identifier</param>
    /// <param name="timestamp">Point in time to query</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Ownership record as it was at that moment with blockchain proof</returns>
    Task<OASISResult<OwnershipRecord>> GetOwnerAtTimeAsync(
        string assetId,
        DateTimeOffset timestamp,
        CancellationToken token = default);

    /// <summary>
    /// Checks if an asset was available (unencumbered) at a specific time.
    /// Critical for: Margin call disputes, "could we have used it then?"
    /// </summary>
    /// <param name="assetId">Asset identifier</param>
    /// <param name="timestamp">Point in time to check</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Availability status with encumbrance details</returns>
    Task<OASISResult<AvailabilityRecord>> CheckAvailabilityAtTimeAsync(
        string assetId,
        DateTimeOffset timestamp,
        CancellationToken token = default);

    /// <summary>
    /// Gets a complete portfolio snapshot at a specific point in time.
    /// Critical for: Regulatory reporting, audits, "what did we own on quarter-end?"
    /// </summary>
    /// <param name="ownerId">Owner identifier</param>
    /// <param name="timestamp">Point in time for snapshot</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Complete portfolio as it was at that moment</returns>
    Task<OASISResult<PortfolioSnapshot>> GetPortfolioSnapshotAsync(
        string ownerId,
        DateTimeOffset timestamp,
        CancellationToken token = default);

    /// <summary>
    /// Generates court-admissible evidence for ownership at a specific time.
    /// Includes: Blockchain transactions, oracle consensus, cryptographic proofs
    /// </summary>
    /// <param name="assetId">Asset identifier</param>
    /// <param name="timestamp">Point in time</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Complete evidence package for legal proceedings</returns>
    Task<OASISResult<OwnershipEvidence>> GenerateOwnershipEvidenceAsync(
        string assetId,
        DateTimeOffset timestamp,
        CancellationToken token = default);
}

