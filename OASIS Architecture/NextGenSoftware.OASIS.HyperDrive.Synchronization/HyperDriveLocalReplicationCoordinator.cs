using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.HyperDrive.Synchronization
{
    /// <summary>
    /// Drains one durable device-local projection queue. It never retries inside a run: a failed
    /// idempotent operation remains durable and a later lifecycle/connectivity wake starts the next run.
    /// </summary>
    public sealed class HyperDriveLocalReplicationCoordinator
    {
        private readonly IHyperDriveLocalReplicationOutbox _outbox;
        private readonly IHyperDriveLocalReplicationTarget _target;
        private readonly SemaphoreSlim _runLock = new SemaphoreSlim(1, 1);

        public HyperDriveLocalReplicationCoordinator(IHyperDriveLocalReplicationOutbox outbox,
            IHyperDriveLocalReplicationTarget target)
        {
            _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
            _target = target ?? throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrWhiteSpace(target.LocalReplicationTargetId))
                throw new ArgumentException("The local replication target id is required.", nameof(target));
        }

        public async Task<OASISResult<LocalReplicationRunResult>> RunOnceAsync(int maximumCount,
            CancellationToken cancellationToken)
        {
            if (maximumCount <= 0) throw new ArgumentOutOfRangeException(nameof(maximumCount));
            await _runLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var targetId = _target.LocalReplicationTargetId;
                var pending = await _outbox.ReadPendingLocalReplicationsAsync(targetId, maximumCount, cancellationToken)
                    .ConfigureAwait(false);
                if (pending == null || pending.IsError || pending.Result == null)
                    return Error(pending?.ErrorCode ?? "LOCAL_REPLICATION_READ_FAILED",
                        pending?.Message ?? "The local replication queue returned no operations.", targetId);

                var run = new LocalReplicationRunResult { TargetId = targetId };
                foreach (var operation in pending.Result)
                {
                    run.Attempted++;
                    OASISResult<bool> applied;
                    try
                    {
                        applied = await _target.ApplyLocalMutationAsync(operation, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception error)
                    {
                        applied = new OASISResult<bool>
                        {
                            IsError = true, ErrorCount = 1,
                            ErrorCode = "LOCAL_REPLICATION_TARGET_EXCEPTION", Message = error.Message
                        };
                    }
                    if (applied == null || applied.IsError || !applied.Result)
                    {
                        var recorded = await _outbox.RecordLocalReplicationFailureAsync(targetId,
                            operation.OperationId, applied?.ErrorCode, applied?.Message, cancellationToken)
                            .ConfigureAwait(false);
                        if (recorded == null || recorded.IsError || !recorded.Result)
                            return Error(recorded?.ErrorCode ?? "LOCAL_REPLICATION_FAILURE_RECORD_FAILED",
                                recorded?.Message ?? "The projection failure could not be recorded.", targetId, run);
                        return Error(applied?.ErrorCode ?? "LOCAL_REPLICATION_APPLY_FAILED",
                            applied?.Message ?? "The local projection target rejected the operation.", targetId, run);
                    }

                    var completed = await _outbox.CompleteLocalReplicationAsync(targetId,
                        operation.OperationId, cancellationToken).ConfigureAwait(false);
                    if (completed == null || completed.IsError || !completed.Result)
                        return Error(completed?.ErrorCode ?? "LOCAL_REPLICATION_ACK_FAILED",
                            completed?.Message ?? "The local projection acknowledgement was not committed.", targetId, run);
                    run.Completed++;
                }

                var remaining = await _outbox.GetPendingLocalReplicationCountAsync(targetId, cancellationToken)
                    .ConfigureAwait(false);
                if (remaining == null || remaining.IsError)
                    return Error(remaining?.ErrorCode ?? "LOCAL_REPLICATION_COUNT_FAILED",
                        remaining?.Message ?? "The remaining projection count is unavailable.", targetId, run);
                run.Remaining = remaining.Result;
                return new OASISResult<LocalReplicationRunResult>(run);
            }
            finally { _runLock.Release(); }
        }

        private static OASISResult<LocalReplicationRunResult> Error(string code, string message,
            string targetId, LocalReplicationRunResult run = null) => new OASISResult<LocalReplicationRunResult>
        {
            IsError = true, ErrorCount = 1, ErrorCode = code, Message = message,
            Result = run ?? new LocalReplicationRunResult { TargetId = targetId }
        };
    }
}
