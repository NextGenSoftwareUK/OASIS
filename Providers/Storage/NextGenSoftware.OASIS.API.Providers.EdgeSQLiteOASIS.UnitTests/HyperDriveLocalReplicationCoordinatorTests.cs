using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.HyperDrive.Synchronization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS.UnitTests;

public sealed class HyperDriveLocalReplicationCoordinatorTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"oasis-local-replication-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task SuccessfulRunAppliesInSequenceAndDurablyAcknowledges()
    {
        using var store = Store();
        await Save(store, 1);
        await Save(store, 2);
        var target = new Target();
        var coordinator = new HyperDriveLocalReplicationCoordinator(store, target);

        var run = await coordinator.RunOnceAsync(10, default);

        run.IsError.Should().BeFalse(run.Message);
        run.Result.Completed.Should().Be(2);
        run.Result.Remaining.Should().Be(0);
        target.Sequences.Should().Equal(1, 2);
        (await store.GetPendingLocalReplicationCountAsync(Target.Id, default)).Result.Should().Be(0);
    }

    [Fact]
    public async Task FailureStopsRunAndLeavesFailedAndLaterOperationsDurable()
    {
        using var store = Store();
        await Save(store, 1);
        await Save(store, 2);
        var target = new Target { FailSequence = 1 };
        var coordinator = new HyperDriveLocalReplicationCoordinator(store, target);

        var failed = await coordinator.RunOnceAsync(10, default);

        failed.ErrorCode.Should().Be("TARGET_OFFLINE");
        failed.Result.Attempted.Should().Be(1);
        target.Sequences.Should().Equal(1);

        target.FailSequence = null;
        var resumed = await coordinator.RunOnceAsync(10, default);
        resumed.IsError.Should().BeFalse(resumed.Message);
        resumed.Result.Completed.Should().Be(2);
        target.Sequences.Should().Equal(new long[] { 1, 1, 2 },
            "the idempotent failed operation is retried before the next sequence");
    }

    private EdgeSQLiteSyncStateStore Store() => new(_databasePath,
        localReplicationTargetIds: new[] { Target.Id });

    private static Task<OASISResult<SyncOperation>> Save(EdgeSQLiteSyncStateStore store, int ordinal) =>
        store.ApplyLocalMutationAsync(new EdgeLocalMutation
        {
            OperationId = Guid.NewGuid(), DeviceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            AvatarId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), EntityId = Guid.NewGuid(),
            EntityType = "holon", Kind = SyncOperationKind.Upsert, LocalVersionId = Guid.NewGuid(),
            PayloadJson = $"{{\"ordinal\":{ordinal}}}", CreatedUtc = DateTime.UtcNow
        }, default);

    private sealed class Target : IHyperDriveLocalReplicationTarget
    {
        public const string Id = "HoloOASIS.Edge";
        public string LocalReplicationTargetId => Id;
        public long? FailSequence { get; set; }
        public List<long> Sequences { get; } = new();

        public Task<OASISResult<bool>> ApplyLocalMutationAsync(SyncOperation operation,
            CancellationToken cancellationToken)
        {
            Sequences.Add(operation.DeviceSequence);
            return Task.FromResult(FailSequence == operation.DeviceSequence
                ? new OASISResult<bool> { IsError = true, ErrorCode = "TARGET_OFFLINE", Message = "offline" }
                : new OASISResult<bool>(true));
        }
    }

    public void Dispose()
    {
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            var path = _databasePath + suffix;
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
