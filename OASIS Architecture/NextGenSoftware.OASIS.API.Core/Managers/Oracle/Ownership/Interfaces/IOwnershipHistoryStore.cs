using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.DTOs;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.Oracle.Ownership.Interfaces;

/// <summary>
/// Stores and retrieves ownership history for time-travel queries and audits.
/// Uses MongoDB/IPFS for immutable, tamper-proof historical records.
/// </summary>
public interface IOwnershipHistoryStore
{
    /// <summary>
    /// Records an ownership event (transfer, pledge, release) to permanent storage.
    /// </summary>
    Task<OASISResult<string>> RecordOwnershipEventAsync(
        OwnershipEvent ownershipEvent,
        CancellationToken token = default);

    /// <summary>
    /// Gets all ownership events for an asset within a time range.
    /// </summary>
    Task<OASISResult<List<OwnershipEvent>>> GetEventsAsync(
        string assetId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken token = default);

    /// <summary>
    /// Gets ownership history up to a specific timestamp (for time-travel queries).
    /// </summary>
    Task<OASISResult<List<OwnershipEvent>>> GetHistoryUpToAsync(
        string assetId,
        DateTimeOffset timestamp,
        CancellationToken token = default);

    /// <summary>
    /// Flags a dispute for human review when oracle consensus is low.
    /// </summary>
    Task<OASISResult<string>> FlagDisputeAsync(
        string assetId,
        List<OwnershipRecord> conflictingRecords,
        CancellationToken token = default);

    /// <summary>
    /// Gets all flagged disputes for review.
    /// </summary>
    Task<OASISResult<List<DisputeFlag>>> GetFlaggedDisputesAsync(
        bool includeResolved = false,
        CancellationToken token = default);
}

