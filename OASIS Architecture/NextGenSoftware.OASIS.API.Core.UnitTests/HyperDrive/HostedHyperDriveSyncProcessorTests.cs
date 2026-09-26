using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class HostedHyperDriveSyncProcessorTests
{
    [Fact]
    public async Task RejectsMalformedRemoteEntityChangeReturnedByHostedStore()
    {
        var avatarId = Guid.NewGuid();
        var store = new StubStore
        {
            Changes = new HostedSyncChangeBatch
            {
                Changes = new[] { new SyncRemoteChange
                {
                    ChangeId = "change-1", EntityId = Guid.NewGuid(), EntityType = "holon",
                    Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(), PayloadJson = null
                } }
            }
        };

        var result = await new HostedHyperDriveSyncProcessor(store)
            .ExchangeAsync(avatarId, NewRequest(avatarId), default);

        Assert.True(result.IsError);
        Assert.Equal("HOSTED_SYNC_INVALID_CHANGE_BATCH", result.ErrorCode);
    }

    [Fact]
    public async Task RejectsInvalidRemoteJsonReturnedByHostedStore()
    {
        var avatarId = Guid.NewGuid();
        var store = new StubStore
        {
            Changes = new HostedSyncChangeBatch
            {
                NextCheckpoint = "next",
                Changes = new[] { new SyncRemoteChange
                {
                    ChangeId = "change-invalid-json", EntityId = Guid.NewGuid(), EntityType = "holon",
                    Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(), PayloadJson = "{",
                    ChangedUtc = DateTime.UtcNow
                } }
            }
        };

        var result = await new HostedHyperDriveSyncProcessor(store)
            .ExchangeAsync(avatarId, NewRequest(avatarId), default);

        Assert.True(result.IsError);
        Assert.Equal("HOSTED_SYNC_INVALID_CHANGE_BATCH", result.ErrorCode);
    }

    [Fact]
    public async Task RejectsAvatarScopeMismatchBeforeCallingStore()
    {
        var store = new StubStore();
        var processor = new HostedHyperDriveSyncProcessor(store);
        var result = await processor.ExchangeAsync(Guid.NewGuid(), NewRequest(Guid.NewGuid()), default);
        result.ErrorCode.Should().Be("HOSTED_SYNC_AVATAR_SCOPE_MISMATCH");
        store.ApplyCalls.Should().Be(0);
        store.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task RejectsUnsupportedProtocolBeforeCallingStore()
    {
        var avatarId = Guid.NewGuid();
        var store = new StubStore();
        var request = NewRequest(avatarId);
        request.ProtocolVersion = HyperDriveSyncProtocol.CurrentVersion - 1;

        var result = await new HostedHyperDriveSyncProcessor(store).ExchangeAsync(avatarId, request, default);

        result.ErrorCode.Should().Be("HOSTED_SYNC_PROTOCOL_UNSUPPORTED");
        store.ApplyCalls.Should().Be(0);
        store.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task RejectsUnorderedDeviceSequences()
    {
        var avatarId = Guid.NewGuid();
        var request = NewRequest(avatarId);
        request.Operations = new[] { NewOperation(request, 2), NewOperation(request, 1) };
        var result = await new HostedHyperDriveSyncProcessor(new StubStore()).ExchangeAsync(avatarId, request, default);
        result.ErrorCode.Should().Be("HOSTED_SYNC_OPERATION_INVALID");
    }

    [Fact]
    public async Task RejectsMalformedOperationPayloadBeforeCallingHostedStore()
    {
        var avatarId = Guid.NewGuid();
        var request = NewRequest(avatarId);
        var operation = NewOperation(request, 1);
        operation.PayloadJson = "{not-json";
        request.Operations = new[] { operation };
        var store = new StubStore();

        var result = await new HostedHyperDriveSyncProcessor(store)
            .ExchangeAsync(avatarId, request, default);

        result.ErrorCode.Should().Be("HOSTED_SYNC_PAYLOAD_INVALID");
        store.ApplyCalls.Should().Be(0);
        store.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task RejectsMissingOrDuplicateStoreResults()
    {
        var avatarId = Guid.NewGuid();
        var request = NewRequest(avatarId);
        request.Operations = new[] { NewOperation(request, 1) };
        var store = new StubStore { Results = Array.Empty<SyncOperationResult>() };
        var result = await new HostedHyperDriveSyncProcessor(store).ExchangeAsync(avatarId, request, default);
        result.ErrorCode.Should().Be("HOSTED_SYNC_INVALID_OPERATION_RESULTS");
        store.ReadCalls.Should().Be(0);
    }

    [Fact]
    public async Task CapsPullBatchAndReturnsSharedResponse()
    {
        var avatarId = Guid.NewGuid();
        var request = NewRequest(avatarId);
        request.MaximumRemoteChanges = 999;
        var store = new StubStore { Changes = new HostedSyncChangeBatch { NextCheckpoint = "42", HasMoreChanges = true } };
        var result = await new HostedHyperDriveSyncProcessor(store, 10, 25).ExchangeAsync(avatarId, request, default);
        result.IsError.Should().BeFalse();
        result.Result.NextPullCheckpoint.Should().Be("42");
        result.Result.HasMoreRemoteChanges.Should().BeTrue();
        store.LastMaximumCount.Should().Be(25);
        store.LastReadDeviceId.Should().Be(request.DeviceId);
    }

    [Fact]
    public async Task ForwardsAndReturnsAnOrderedSnapshotPage()
    {
        var avatarId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();
        var request = NewRequest(avatarId);
        request.SnapshotId = snapshotId;
        request.SnapshotPageIndex = 3;
        var store = new StubStore
        {
            Changes = new HostedSyncChangeBatch
            {
                IsAuthoritativeSnapshot = true, SnapshotId = snapshotId, SnapshotPageIndex = 3,
                SnapshotComplete = false, HasMoreChanges = true, NextCheckpoint = "watermark"
            }
        };

        var result = await new HostedHyperDriveSyncProcessor(store).ExchangeAsync(avatarId, request, default);

        result.IsError.Should().BeFalse(result.Message);
        store.LastSnapshotId.Should().Be(snapshotId);
        store.LastSnapshotPageIndex.Should().Be(3);
        result.Result.IsAuthoritativeSnapshot.Should().BeTrue();
        result.Result.SnapshotId.Should().Be(snapshotId);
        result.Result.SnapshotPageIndex.Should().Be(3);
        result.Result.SnapshotComplete.Should().BeFalse();
    }

    [Fact]
    public async Task AllowsMaterializedSnapshotPageWidthToRemainStableAcrossClientLimitChanges()
    {
        var avatarId = Guid.NewGuid();
        var request = NewRequest(avatarId);
        request.MaximumRemoteChanges = 1;
        var snapshotId = Guid.NewGuid();
        var store = new StubStore
        {
            Changes = new HostedSyncChangeBatch
            {
                IsAuthoritativeSnapshot = true,
                SnapshotId = snapshotId,
                SnapshotPageIndex = 0,
                SnapshotComplete = false,
                HasMoreChanges = true,
                Changes = new[] { NewRemoteChange("one"), NewRemoteChange("two") }
            }
        };

        var result = await new HostedHyperDriveSyncProcessor(store, 10, 3)
            .ExchangeAsync(avatarId, request, default);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.RemoteChanges.Should().HaveCount(2);
        store.LastMaximumCount.Should().Be(1);
    }

    [Fact]
    public async Task StillRejectsDeltaBatchThatExceedsRequestedLimit()
    {
        var avatarId = Guid.NewGuid();
        var request = NewRequest(avatarId);
        request.MaximumRemoteChanges = 1;
        var store = new StubStore
        {
            Changes = new HostedSyncChangeBatch
            {
                Changes = new[] { NewRemoteChange("one"), NewRemoteChange("two") }
            }
        };

        var result = await new HostedHyperDriveSyncProcessor(store, 10, 3)
            .ExchangeAsync(avatarId, request, default);

        result.ErrorCode.Should().Be("HOSTED_SYNC_INVALID_CHANGE_BATCH");
    }

    private static SyncExchangeRequest NewRequest(Guid avatarId) => new()
    {
        AvatarId = avatarId, DeviceId = Guid.NewGuid(), Operations = Array.Empty<SyncOperation>()
    };

    private static SyncOperation NewOperation(SyncExchangeRequest request, long sequence) => new()
    {
        OperationId = Guid.NewGuid(), AvatarId = request.AvatarId, DeviceId = request.DeviceId,
        DeviceSequence = sequence, EntityId = Guid.NewGuid(), EntityType = "holon",
        Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(), PayloadJson = "{}", CreatedUtc = DateTime.UtcNow
    };

    private static SyncRemoteChange NewRemoteChange(string id) => new()
    {
        ChangeId = id,
        EntityId = Guid.NewGuid(),
        EntityType = "holon",
        Kind = SyncOperationKind.Upsert,
        VersionId = Guid.NewGuid(),
        PayloadJson = "{}"
    };

    private sealed class StubStore : IHostedHyperDriveSyncStore
    {
        public int ApplyCalls { get; private set; }
        public int ReadCalls { get; private set; }
        public Guid LastReadDeviceId { get; private set; }
        public int LastMaximumCount { get; private set; }
        public Guid LastSnapshotId { get; private set; }
        public int LastSnapshotPageIndex { get; private set; }
        public IReadOnlyList<SyncOperationResult> Results { get; set; } = Array.Empty<SyncOperationResult>();
        public HostedSyncChangeBatch Changes { get; set; } = new();

        public Task<OASISResult<HostedSyncOperationBatchResult>> ApplyOperationsAsync(Guid authenticatedAvatarId,
            Guid deviceId, IReadOnlyList<SyncOperation> operations, CancellationToken cancellationToken)
        {
            ApplyCalls++;
            return Task.FromResult(new OASISResult<HostedSyncOperationBatchResult>(new HostedSyncOperationBatchResult
            { OperationResults = Results }));
        }

        public Task<OASISResult<HostedSyncChangeBatch>> ReadChangesAsync(Guid authenticatedAvatarId,
            Guid deviceId, string checkpoint, Guid snapshotId, int snapshotPageIndex,
            int maximumCount, CancellationToken cancellationToken)
        {
            ReadCalls++; LastMaximumCount = maximumCount; LastReadDeviceId = deviceId;
            LastSnapshotId = snapshotId; LastSnapshotPageIndex = snapshotPageIndex;
            return Task.FromResult(new OASISResult<HostedSyncChangeBatch>(Changes));
        }
    }
}
