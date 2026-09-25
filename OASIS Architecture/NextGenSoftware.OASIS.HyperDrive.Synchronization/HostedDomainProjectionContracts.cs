using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization
{
    /// <summary>
    /// Applies a versioned sync mutation to the authoritative full OASIS domain model. Implementations
    /// must commit the domain mutation and its operation receipt atomically so replay is idempotent.
    /// </summary>
    public interface IHostedHyperDriveDomainMutationStore
    {
        Task<OASISResult<HostedDomainMutationResult>> ApplyDomainMutationAsync(
            HostedSyncFanOutItem mutation, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Captures domain mutations committed through the normal OASIS APIs and projects them into the
    /// hosted HyperDrive entity/change feed. A successful result means the durable capture checkpoint
    /// and projected change were committed atomically.
    /// </summary>
    public interface IHostedHyperDriveDomainChangeCaptureStore
    {
        Task<OASISResult<HostedDomainChangeCaptureResult>> CaptureNextDomainChangesAsync(
            int maximumCount, TimeSpan maximumWait, CancellationToken cancellationToken);
    }

    /// <summary>One-time, resumable preparation of existing hosted domain data for change capture.</summary>
    public interface IHostedHyperDriveDomainBackfillStore
    {
        Task<OASISResult<HostedDomainBackfillResult>> BackfillDomainStateAsync(
            int batchSize, CancellationToken cancellationToken);
    }

    public sealed class HostedDomainMutationResult
    {
        public bool AlreadyApplied { get; set; }
        public Guid VersionId { get; set; }
    }

    public sealed class HostedDomainChangeCaptureResult
    {
        public int CapturedCount { get; set; }
        public int RejectedCount { get; set; }
        public string Checkpoint { get; set; }
    }

    public sealed class HostedDomainBackfillResult
    {
        public int ProjectedCount { get; set; }
        public int RejectedCount { get; set; }
        public bool CaptureInitialized { get; set; }
    }
}
