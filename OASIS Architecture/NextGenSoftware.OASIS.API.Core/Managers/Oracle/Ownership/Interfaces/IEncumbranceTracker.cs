using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.Interfaces;

/// <summary>
/// Tracks all encumbrances (pledges, liens, locks) across all blockchains.
/// Critical for knowing what collateral is actually available vs locked.
/// </summary>
public interface IEncumbranceTracker
{
    /// <summary>
    /// Gets all active encumbrances for an asset.
    /// Shows what pledges, liens, locks are currently active.
    /// </summary>
    /// <param name="assetId">Asset identifier</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>List of active encumbrances with priorities</returns>
    Task<OASISResult<List<Encumbrance>>> GetActiveEncumbrancesAsync(
        string assetId,
        CancellationToken token = default);

    /// <summary>
    /// Gets all pledges made by an owner across all chains.
    /// Critical for: "What have I pledged to whom?"
    /// </summary>
    /// <param name="ownerId">Owner identifier</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>List of all active pledges</returns>
    Task<OASISResult<List<Encumbrance>>> GetAllPledgesAsync(
        string ownerId,
        CancellationToken token = default);

    /// <summary>
    /// Gets maturity schedule showing when pledged collateral will become available.
    /// Critical for: "When will I have $X available?"
    /// </summary>
    /// <param name="ownerId">Owner identifier</param>
    /// <param name="hoursAhead">How many hours to look ahead</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Schedule of upcoming maturities</returns>
    Task<OASISResult<List<MaturitySchedule>>> GetMaturityScheduleAsync(
        string ownerId,
        int hoursAhead = 24,
        CancellationToken token = default);

    /// <summary>
    /// Creates a new encumbrance (pledge, lien, lock).
    /// Records across all relevant chains and systems.
    /// </summary>
    /// <param name="encumbrance">Encumbrance details</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Created encumbrance with transaction hashes</returns>
    Task<OASISResult<Encumbrance>> CreateEncumbranceAsync(
        CreateEncumbranceRequest encumbrance,
        CancellationToken token = default);

    /// <summary>
    /// Releases an encumbrance (e.g., repo matures, loan repaid).
    /// Updates all chains and systems automatically.
    /// </summary>
    /// <param name="encumbranceId">Encumbrance identifier</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Release confirmation with timestamps</returns>
    Task<OASISResult<EncumbranceRelease>> ReleaseEncumbranceAsync(
        string encumbranceId,
        CancellationToken token = default);

    /// <summary>
    /// Monitors for encumbrances approaching maturity and triggers auto-release.
    /// Runs continuously in background.
    /// </summary>
    Task StartMaturityMonitoringAsync(CancellationToken token = default);
}

