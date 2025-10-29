using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.Interfaces;

/// <summary>
/// Provides real-time ownership tracking across all blockchains and legacy systems.
/// Solves the "$100-150 billion problem" of "who owns what, when" for financial institutions.
/// </summary>
public interface IOwnershipOracle
{
    /// <summary>
    /// Gets the current owner of an asset across ALL chains and systems.
    /// Returns in <1 second with multi-oracle consensus.
    /// Critical for: Real-time collateral management, margin call prevention
    /// </summary>
    /// <param name="assetId">Unique asset identifier (can be NFT ID, token address, etc.)</param>
    /// <param name="atTimestamp">Optional: Query ownership at specific point in time (time-travel)</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Ownership record with multi-oracle consensus</returns>
    Task<OASISResult<OwnershipRecord>> GetCurrentOwnerAsync(
        string assetId,
        DateTimeOffset? atTimestamp = null,
        CancellationToken token = default);

    /// <summary>
    /// Gets complete ownership history for an asset (who owned it when).
    /// Critical for: Dispute resolution, regulatory audits, court cases
    /// </summary>
    /// <param name="assetId">Asset identifier</param>
    /// <param name="fromDate">Start of time range</param>
    /// <param name="toDate">End of time range</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Chronological list of ownership events with blockchain evidence</returns>
    Task<OASISResult<List<OwnershipEvent>>> GetOwnershipHistoryAsync(
        string assetId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken token = default);

    /// <summary>
    /// Checks if an asset is encumbered (pledged, locked, liened).
    /// Critical for: Knowing what collateral is actually available
    /// </summary>
    /// <param name="assetId">Asset identifier</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Encumbrance status with list of active pledges</returns>
    Task<OASISResult<EncumbranceStatus>> CheckEncumbranceAsync(
        string assetId,
        CancellationToken token = default);

    /// <summary>
    /// Gets all assets owned by an entity across ALL chains.
    /// Critical for: Portfolio management, regulatory reporting
    /// </summary>
    /// <param name="ownerId">Owner identifier (Avatar ID, wallet address, etc.)</param>
    /// <param name="includeEncumbered">Include pledged/locked assets</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Complete portfolio with real-time valuations</returns>
    Task<OASISResult<List<AssetOwnership>>> GetPortfolioAsync(
        string ownerId,
        bool includeEncumbered = true,
        CancellationToken token = default);

    /// <summary>
    /// Gets available (unencumbered) assets for an owner.
    /// Critical for: Finding collateral to pledge, answering "what can I use?"
    /// </summary>
    /// <param name="ownerId">Owner identifier</param>
    /// <param name="minValue">Minimum asset value (optional filter)</param>
    /// <param name="assetTypes">Acceptable asset types (optional filter)</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>List of available assets with real-time values</returns>
    Task<OASISResult<List<AssetOwnership>>> GetAvailableAssetsAsync(
        string ownerId,
        decimal minValue = 0,
        List<string> assetTypes = null,
        CancellationToken token = default);

    /// <summary>
    /// Verifies ownership claim with multi-oracle consensus.
    /// Critical for: Dispute resolution, court cases
    /// </summary>
    /// <param name="assetId">Asset identifier</param>
    /// <param name="claimedOwner">Claimed owner identifier</param>
    /// <param name="claimTimestamp">Time when ownership is claimed</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Verification result with evidence and consensus level</returns>
    Task<OASISResult<OwnershipVerification>> VerifyOwnershipClaimAsync(
        string assetId,
        string claimedOwner,
        DateTimeOffset claimTimestamp,
        CancellationToken token = default);
}

