using System.Collections.Concurrent;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public class HyperDriveSyncCoordinatorTests
{
    private readonly Guid _deviceId = Guid.NewGuid();
    private readonly Guid _avatarId = Guid.NewGuid();

    [Fact]
    public async Task SynchronizeOnce_CommitsAcknowledgementsChangesAndCheckpointAtomically()
    {
        var operation = CreateOperation(1);
        var stateStore = new RecordingStateStore(operation);
        var transport = new DelegateTransport(request => SuccessResponse(request, "checkpoint-2", oneRemoteChange: true));
        var clock = new FixedClock(new DateTime(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc));
        var coordinator = new HyperDriveSyncCoordinator(_deviceId, _avatarId, stateStore, transport, clock);

        var result = await coordinator.SynchronizeOnceAsync();

        Assert.False(result.IsError, result.Message);
        Assert.Equal(1, result.Result.SentOperationCount);
        Assert.Equal(1, result.Result.AcceptedOperationCount);
        Assert.Equal(1, result.Result.AppliedRemoteChangeCount);
        Assert.Equal("checkpoint-2", result.Result.PullCheckpoint);
        Assert.Single(stateStore.Commits);
        Assert.Equal(clock.UtcNow, stateStore.Commits[0].CommittedUtc);
        Assert.Equal("checkpoint-2", stateStore.Commits[0].NextPullCheckpoint);
    }

    [Fact]
    public async Task SynchronizeOnce_TransportFailureLeavesDurableStateUnchanged()
    {
        var stateStore = new RecordingStateStore(CreateOperation(1));
        var transport = new DelegateTransport(_ => new OASISResult<SyncExchangeResponse>
        {
            IsError = true,
            Message = "network unavailable"
        });
        var coordinator = new HyperDriveSyncCoordinator(_deviceId, _avatarId, stateStore, transport);

        var result = await coordinator.SynchronizeOnceAsync();

        Assert.True(result.IsError);
        Assert.Contains("network unavailable", result.Message);
        Assert.Empty(stateStore.Commits);
    }

    [Fact]
    public async Task SynchronizeOnce_RejectsAcknowledgementForOperationThatWasNotSent()
    {
        var stateStore = new RecordingStateStore(CreateOperation(1));
        var transport = new DelegateTransport(_ => new OASISResult<SyncExchangeResponse>
        {
            Result = new SyncExchangeResponse
            {
                NextPullCheckpoint = "checkpoint-2",
                OperationResults = new[]
                {
                    new SyncOperationResult
                    {
                        OperationId = Guid.NewGuid(),
                        Disposition = SyncOperationDisposition.Accepted
                    }
                }
            }
        });
        var coordinator = new HyperDriveSyncCoordinator(_deviceId, _avatarId, stateStore, transport);

        var result = await coordinator.SynchronizeOnceAsync();

        Assert.True(result.IsError);
        Assert.Contains("unknown operation", result.Message);
        Assert.Empty(stateStore.Commits);
    }

    [Fact]
    public async Task SynchronizeOnce_RejectsPartialAcknowledgementSet()
    {
        var stateStore = new RecordingStateStore(CreateOperation(1), CreateOperation(2));
        var transport = new DelegateTransport(request => new OASISResult<SyncExchangeResponse>
        {
            Result = new SyncExchangeResponse
            {
                NextPullCheckpoint = "checkpoint-2",
                OperationResults = new[]
                {
                    new SyncOperationResult
                    {
                        OperationId = request.Operations[0].OperationId,
                        Disposition = SyncOperationDisposition.Accepted
                    }
                }
            }
        });
        var coordinator = new HyperDriveSyncCoordinator(_deviceId, _avatarId, stateStore, transport);

        var result = await coordinator.SynchronizeOnceAsync();

        Assert.True(result.IsError);
        Assert.Contains("exactly one result", result.Message);
        Assert.Empty(stateStore.Commits);
    }

    [Fact]
    public async Task SynchronizeOnce_RejectsDifferentProtocolVersionWithoutCommitting()
    {
        var stateStore = new RecordingStateStore();
        var transport = new DelegateTransport(_ => new OASISResult<SyncExchangeResponse>
        {
            Result = new SyncExchangeResponse
            {
                ProtocolVersion = HyperDriveSyncProtocol.CurrentVersion - 1,
                NextPullCheckpoint = "checkpoint-2"
            }
        });
        var coordinator = new HyperDriveSyncCoordinator(_deviceId, _avatarId, stateStore, transport);

        var result = await coordinator.SynchronizeOnceAsync();

        Assert.True(result.IsError);
        Assert.Equal("SYNC_RESPONSE_INVALID", result.ErrorCode);
        Assert.Contains("unsupported protocol version", result.Message);
        Assert.Empty(stateStore.Commits);
    }

    [Fact]
    public async Task SynchronizeOnce_PullsRemoteChangesWhenOutboxIsEmpty()
    {
        var stateStore = new RecordingStateStore();
        var transport = new DelegateTransport(request => SuccessResponse(request, "checkpoint-2", oneRemoteChange: true));
        var coordinator = new HyperDriveSyncCoordinator(_deviceId, _avatarId, stateStore, transport);

        var result = await coordinator.SynchronizeOnceAsync();

        Assert.False(result.IsError, result.Message);
        Assert.Equal(0, result.Result.SentOperationCount);
        Assert.Equal(1, result.Result.AppliedRemoteChangeCount);
        Assert.Single(stateStore.Commits);
    }

    [Fact]
    public async Task SynchronizeOnce_RejectsCommandInRemoteChangeStreamBeforeLocalCommit()
    {
        var stateStore = new RecordingStateStore();
        var transport = new DelegateTransport(_ => new OASISResult<SyncExchangeResponse>
        {
            Result = new SyncExchangeResponse
            {
                NextPullCheckpoint = "checkpoint-2",
                RemoteChanges = new[]
                {
                    new SyncRemoteChange
                    {
                        ChangeId = "invalid-command",
                        EntityId = Guid.NewGuid(),
                        EntityType = "QuestProgressCommand",
                        Kind = SyncOperationKind.Command,
                        VersionId = Guid.NewGuid(),
                        PayloadJson = "{}",
                        ChangedUtc = DateTime.UtcNow
                    }
                }
            }
        });
        var coordinator = new HyperDriveSyncCoordinator(_deviceId, _avatarId, stateStore, transport);

        var result = await coordinator.SynchronizeOnceAsync();

        Assert.True(result.IsError);
        Assert.Equal("SYNC_RESPONSE_INVALID", result.ErrorCode);
        Assert.Contains("invalid or duplicate remote change", result.Message);
        Assert.Empty(stateStore.Commits);
    }

    [Fact]
    public async Task SynchronizeOnce_RejectsInvalidRemoteJsonBeforeLocalCommit()
    {
        var stateStore = new RecordingStateStore();
        var transport = new DelegateTransport(_ => new OASISResult<SyncExchangeResponse>
        {
            Result = new SyncExchangeResponse
            {
                NextPullCheckpoint = "checkpoint-2",
                RemoteChanges = new[]
                {
                    new SyncRemoteChange
                    {
                        ChangeId = "invalid-json",
                        EntityId = Guid.NewGuid(),
                        EntityType = "Holon",
                        Kind = SyncOperationKind.Upsert,
                        VersionId = Guid.NewGuid(),
                        PayloadJson = "{",
                        ChangedUtc = DateTime.UtcNow
                    }
                }
            }
        });

        var result = await new HyperDriveSyncCoordinator(_deviceId, _avatarId, stateStore, transport)
            .SynchronizeOnceAsync();

        Assert.True(result.IsError);
        Assert.Equal("SYNC_RESPONSE_INVALID", result.ErrorCode);
        Assert.Empty(stateStore.Commits);
    }

    [Fact]
    public async Task SynchronizeOnce_SerializesConcurrentCycles()
    {
        var stateStore = new RecordingStateStore();
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = 0;
        var maximumActive = 0;
        var transport = new AsyncDelegateTransport(async (request, cancellationToken) =>
        {
            var current = Interlocked.Increment(ref active);
            maximumActive = Math.Max(maximumActive, current);
            entered.TrySetResult(true);
            await release.Task.WaitAsync(cancellationToken);
            Interlocked.Decrement(ref active);
            return SuccessResponse(request, "checkpoint-2", oneRemoteChange: false);
        });
        var coordinator = new HyperDriveSyncCoordinator(_deviceId, _avatarId, stateStore, transport);

        var first = coordinator.SynchronizeOnceAsync();
        await entered.Task;
        var second = coordinator.SynchronizeOnceAsync();
        release.SetResult(true);
        await Task.WhenAll(first, second);

        Assert.Equal(1, maximumActive);
    }

    [Fact]
    public async Task SynchronizeOnce_ContinuesAndCommitsDurableSnapshotCursor()
    {
        var snapshotId = Guid.NewGuid();
        var stateStore = new RecordingStateStore
        {
            Checkpoint = new SyncCheckpoint
            {
                PullCheckpoint = "old-checkpoint", SnapshotId = snapshotId, SnapshotPageIndex = 2
            }
        };
        SyncExchangeRequest captured = null;
        var transport = new DelegateTransport(request =>
        {
            captured = request;
            return new OASISResult<SyncExchangeResponse>
            {
                Result = new SyncExchangeResponse
                {
                    IsAuthoritativeSnapshot = true, SnapshotId = snapshotId, SnapshotPageIndex = 2,
                    SnapshotComplete = true, HasMoreRemoteChanges = false,
                    NextPullCheckpoint = "new-checkpoint"
                }
            };
        });

        var result = await new HyperDriveSyncCoordinator(_deviceId, _avatarId, stateStore, transport)
            .SynchronizeOnceAsync();

        Assert.False(result.IsError, result.Message);
        Assert.Equal(snapshotId, captured.SnapshotId);
        Assert.Equal(2, captured.SnapshotPageIndex);
        Assert.True(stateStore.Commits.Single().IsAuthoritativeSnapshot);
        Assert.True(stateStore.Commits.Single().SnapshotComplete);
    }

    private SyncOperation CreateOperation(long sequence) => new()
    {
        OperationId = Guid.NewGuid(),
        DeviceId = _deviceId,
        AvatarId = _avatarId,
        DeviceSequence = sequence,
        EntityId = Guid.NewGuid(),
        EntityType = "Holon",
        Kind = SyncOperationKind.Upsert,
        VersionId = Guid.NewGuid(),
        PayloadJson = "{}",
        CreatedUtc = DateTime.UtcNow
    };

    private static OASISResult<SyncExchangeResponse> SuccessResponse(
        SyncExchangeRequest request, string checkpoint, bool oneRemoteChange)
        => new()
        {
            Result = new SyncExchangeResponse
            {
                NextPullCheckpoint = checkpoint,
                OperationResults = request.Operations.Select(operation => new SyncOperationResult
                {
                    OperationId = operation.OperationId,
                    Disposition = SyncOperationDisposition.Accepted,
                    ResultVersionId = Guid.NewGuid()
                }).ToArray(),
                RemoteChanges = oneRemoteChange
                    ? new[]
                    {
                        new SyncRemoteChange
                        {
                            ChangeId = "2",
                            EntityId = Guid.NewGuid(),
                            EntityType = "Holon",
                            Kind = SyncOperationKind.Upsert,
                            VersionId = Guid.NewGuid(),
                            PayloadJson = "{}",
                            ChangedUtc = DateTime.UtcNow
                        }
                    }
                    : Array.Empty<SyncRemoteChange>()
            }
        };

    private sealed class RecordingStateStore : IHyperDriveSyncStateStore
    {
        private readonly IReadOnlyList<SyncOperation> _operations;
        public List<SyncCommit> Commits { get; } = new();
        public SyncCheckpoint Checkpoint { get; set; } = new SyncCheckpoint { PullCheckpoint = "checkpoint-1" };

        public RecordingStateStore(params SyncOperation[] operations) => _operations = operations;

        public Task<OASISResult<SyncCheckpoint>> GetCheckpointAsync(CancellationToken cancellationToken)
            => Task.FromResult(new OASISResult<SyncCheckpoint>
            {
                Result = Checkpoint
            });

        public Task<OASISResult<IReadOnlyList<SyncOperation>>> ReadPendingOperationsAsync(
            int maximumCount, CancellationToken cancellationToken)
            => Task.FromResult(new OASISResult<IReadOnlyList<SyncOperation>>
            {
                Result = _operations.Take(maximumCount).ToArray()
            });

        public Task<OASISResult<bool>> CommitExchangeAsync(SyncCommit commit, CancellationToken cancellationToken)
        {
            Commits.Add(commit);
            return Task.FromResult(new OASISResult<bool> { Result = true });
        }
    }

    private sealed class DelegateTransport : IHyperDriveSyncTransport
    {
        private readonly Func<SyncExchangeRequest, OASISResult<SyncExchangeResponse>> _exchange;
        public DelegateTransport(Func<SyncExchangeRequest, OASISResult<SyncExchangeResponse>> exchange) => _exchange = exchange;
        public Task<OASISResult<SyncExchangeResponse>> ExchangeAsync(SyncExchangeRequest request, CancellationToken cancellationToken)
            => Task.FromResult(_exchange(request));
    }

    private sealed class AsyncDelegateTransport : IHyperDriveSyncTransport
    {
        private readonly Func<SyncExchangeRequest, CancellationToken, Task<OASISResult<SyncExchangeResponse>>> _exchange;
        public AsyncDelegateTransport(Func<SyncExchangeRequest, CancellationToken, Task<OASISResult<SyncExchangeResponse>>> exchange)
            => _exchange = exchange;
        public Task<OASISResult<SyncExchangeResponse>> ExchangeAsync(SyncExchangeRequest request, CancellationToken cancellationToken)
            => _exchange(request, cancellationToken);
    }

    private sealed class FixedClock : IHyperDriveClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }
}
