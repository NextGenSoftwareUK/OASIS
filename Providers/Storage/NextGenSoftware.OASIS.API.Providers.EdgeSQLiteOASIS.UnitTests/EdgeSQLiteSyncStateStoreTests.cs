using FluentAssertions;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS.UnitTests;

public sealed class EdgeSQLiteSyncStateStoreTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"oasis-edge-{Guid.NewGuid():N}.db");
    private readonly Guid _deviceId = Guid.NewGuid();
    private readonly Guid _avatarId = Guid.NewGuid();

    [Fact]
    public void LegacyCompletedOutboxIsMigratedWithStableVersionIdentity()
    {
        var operationId = Guid.NewGuid();
        var resultVersionId = Guid.NewGuid();
        CreateLegacyOutbox(operationId, state: 1, resultVersionId);

        using (CreateStore()) { }

        using var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT local_version_id FROM sync_outbox WHERE operation_id=$operation_id;";
        command.Parameters.AddWithValue("$operation_id", operationId.ToString("D"));
        command.ExecuteScalar().Should().Be(resultVersionId.ToString("D"));
    }

    [Fact]
    public void LegacyPendingOutboxFailsClosedWithoutInventingVersionIdentity()
    {
        CreateLegacyOutbox(Guid.NewGuid(), state: 0, resultVersionId: null);

        Action open = () => { using var ignored = CreateStore(); };

        open.Should().Throw<InvalidOperationException>()
            .WithMessage("*pending pre-version-protocol operations*");
        using var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('sync_outbox') WHERE name='local_version_id';";
        Convert.ToInt64(command.ExecuteScalar()).Should().Be(0, "the rejected migration must roll back its schema change");
    }

    [Fact]
    public async Task LocalMutationPersistsEntityAndOutboxAcrossRestart()
    {
        var entityId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        using (var store = CreateStore())
        {
            var saved = await store.ApplyLocalMutationAsync(NewMutation(operationId, entityId, versionId, "{\"name\":\"offline\"}"), default);
            saved.IsError.Should().BeFalse(saved.Message + " " + saved.Exception?.Message);
            saved.Result.DeviceSequence.Should().Be(1);
        }

        using (var reopened = CreateStore())
        {
            var entity = await reopened.LoadEntityAsync("holon", entityId, default);
            entity.Result.PayloadJson.Should().Be("{\"name\":\"offline\"}");
            entity.Result.VersionId.Should().Be(versionId);
            var pending = await reopened.ReadPendingOperationsAsync(10, default);
            pending.Result.Should().ContainSingle(x => x.OperationId == operationId && x.DeviceSequence == 1);
        }
    }

    [Fact]
    public async Task DeviceSequenceIsMonotonicAndDurable()
    {
        using (var store = CreateStore())
        {
            (await store.ApplyLocalMutationAsync(NewMutation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{}"), default)).Result.DeviceSequence.Should().Be(1);
            (await store.ApplyLocalMutationAsync(NewMutation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{}"), default)).Result.DeviceSequence.Should().Be(2);
        }
        using (var reopened = CreateStore())
            (await reopened.ApplyLocalMutationAsync(NewMutation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{}"), default)).Result.DeviceSequence.Should().Be(3);
    }

    [Fact]
    public async Task ChainedOfflineEditsPreserveClientGeneratedVersionHistory()
    {
        var entityId = Guid.NewGuid();
        var firstVersion = Guid.NewGuid();
        var secondVersion = Guid.NewGuid();
        using var store = CreateStore();
        var first = NewMutation(Guid.NewGuid(), entityId, firstVersion, "{\"v\":1}");
        var second = NewMutation(Guid.NewGuid(), entityId, secondVersion, "{\"v\":2}");
        second.BaseVersionId = firstVersion;

        (await store.ApplyLocalMutationAsync(first, default)).IsError.Should().BeFalse();
        (await store.ApplyLocalMutationAsync(second, default)).IsError.Should().BeFalse();
        var pending = (await store.ReadPendingOperationsAsync(10, default)).Result;

        pending.Should().HaveCount(2);
        pending[0].VersionId.Should().Be(firstVersion);
        pending[1].BaseVersionId.Should().Be(firstVersion);
        pending[1].VersionId.Should().Be(secondVersion);
    }

    [Fact]
    public async Task DuplicateOperationRollsBackEntityAndSequence()
    {
        var operationId = Guid.NewGuid();
        using var store = CreateStore();
        (await store.ApplyLocalMutationAsync(NewMutation(operationId, Guid.NewGuid(), Guid.NewGuid(), "{\"v\":1}"), default)).IsError.Should().BeFalse();
        var rejectedEntityId = Guid.NewGuid();
        var duplicate = await store.ApplyLocalMutationAsync(NewMutation(operationId, rejectedEntityId, Guid.NewGuid(), "{\"v\":2}"), default);
        duplicate.IsError.Should().BeTrue();
        (await store.LoadEntityAsync("holon", rejectedEntityId, default)).Result.Should().BeNull();
        var next = await store.ApplyLocalMutationAsync(NewMutation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{}"), default);
        next.Result.DeviceSequence.Should().Be(2);
    }

    [Fact]
    public async Task IdenticalOperationRetryReturnsOriginalWithoutConsumingASequence()
    {
        using var store = CreateStore();
        var mutation = NewMutation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{\"v\":1}");
        mutation.CreatedUtc = DateTime.UtcNow;

        var first = await store.ApplyLocalMutationAsync(mutation, default);
        var replay = await store.ApplyLocalMutationAsync(mutation, default);
        var next = await store.ApplyLocalMutationAsync(
            NewMutation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{}"), default);

        replay.IsError.Should().BeFalse(replay.Message);
        replay.Result.OperationId.Should().Be(first.Result.OperationId);
        replay.Result.DeviceSequence.Should().Be(first.Result.DeviceSequence);
        replay.Message.Should().Contain("already committed");
        next.Result.DeviceSequence.Should().Be(2);
        (await store.ReadPendingOperationsAsync(10, default)).Result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ConcurrentLocalMutationsReceiveUniqueContiguousDeviceSequences()
    {
        using var store = CreateStore();
        var writes = Enumerable.Range(0, 20)
            .Select(_ => store.ApplyLocalMutationAsync(
                NewMutation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{}"), default));

        var results = await Task.WhenAll(writes);

        results.Should().OnlyContain(x => !x.IsError, string.Join(" | ", results.Where(x => x.IsError).Select(x => x.Exception?.Message)));
        results.Select(x => x.Result.DeviceSequence).OrderBy(x => x).Should().Equal(Enumerable.Range(1, 20).Select(x => (long)x));
        (await store.ReadPendingOperationsAsync(25, default)).Result.Should().HaveCount(20);
    }

    [Fact]
    public async Task UnknownAcknowledgementRollsBackEntireExchange()
    {
        using var store = CreateStore();
        var operationId = Guid.NewGuid();
        await store.ApplyLocalMutationAsync(NewMutation(operationId, Guid.NewGuid(), Guid.NewGuid(), "{}"), default);
        var remoteEntityId = Guid.NewGuid();
        var commit = new SyncCommit
        {
            CommittedUtc = DateTime.UtcNow,
            NextPullCheckpoint = "cp-2",
            OperationResults = new[] { new SyncOperationResult { OperationId = Guid.NewGuid(), Disposition = SyncOperationDisposition.Accepted } },
            RemoteChanges = new[] { NewRemoteChange("change-1", remoteEntityId) }
        };

        (await store.CommitExchangeAsync(commit, default)).IsError.Should().BeTrue();
        (await store.GetCheckpointAsync(default)).Result.PullCheckpoint.Should().BeNull();
        (await store.LoadEntityAsync("holon", remoteEntityId, default)).Result.Should().BeNull();
        (await store.ReadPendingOperationsAsync(10, default)).Result.Should().ContainSingle(x => x.OperationId == operationId);
    }

    [Fact]
    public async Task CommitIsDurableAndInboundReplayIsIdempotent()
    {
        var operationId = Guid.NewGuid();
        var remoteEntityId = Guid.NewGuid();
        using (var store = CreateStore())
        {
            await store.ApplyLocalMutationAsync(NewMutation(operationId, Guid.NewGuid(), Guid.NewGuid(), "{}"), default);
            var commit = new SyncCommit
            {
                CommittedUtc = DateTime.UtcNow,
                NextPullCheckpoint = "cp-1",
                OperationResults = new[] { new SyncOperationResult { OperationId = operationId, Disposition = SyncOperationDisposition.Accepted, ResultVersionId = Guid.NewGuid() } },
                RemoteChanges = new[] { NewRemoteChange("change-1", remoteEntityId) }
            };
            (await store.CommitExchangeAsync(commit, default)).IsError.Should().BeFalse();
            (await store.CommitExchangeAsync(new SyncCommit
            {
                CommittedUtc = DateTime.UtcNow, NextPullCheckpoint = "cp-2",
                RemoteChanges = commit.RemoteChanges
            }, default)).IsError.Should().BeFalse();
        }
        using (var reopened = CreateStore())
        {
            (await reopened.ReadPendingOperationsAsync(10, default)).Result.Should().BeEmpty();
            (await reopened.GetCheckpointAsync(default)).Result.PullCheckpoint.Should().Be("cp-2");
            (await reopened.LoadEntityAsync("holon", remoteEntityId, default)).Result.PayloadJson.Should().Be("{\"remote\":true}");
        }
    }

    [Fact]
    public async Task ConflictCanBeRetriedAtomicallyAgainstServerVersion()
    {
        var operationId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var localVersion = Guid.NewGuid();
        var serverVersion = Guid.NewGuid();
        using var store = CreateStore();
        await store.ApplyLocalMutationAsync(NewMutation(operationId, entityId, localVersion, "{\"local\":true}"), default);
        var committed = await store.CommitExchangeAsync(new SyncCommit
        {
            CommittedUtc = DateTime.UtcNow,
            NextPullCheckpoint = "cp-conflict",
            OperationResults = new[] { new SyncOperationResult
            {
                OperationId = operationId, Disposition = SyncOperationDisposition.Conflict,
                ResultVersionId = serverVersion, Code = "BASE_VERSION_CONFLICT"
            }}
        }, default);
        committed.IsError.Should().BeFalse(committed.Message);
        (await store.ReadUnresolvedConflictsAsync(default)).Result.Should().ContainSingle(x =>
            x.OperationId == operationId && x.ServerVersionId == serverVersion && x.LocalVersionId == localVersion);

        var retryOperationId = Guid.NewGuid();
        var retryVersion = Guid.NewGuid();
        var resolved = await store.ResolveConflictAsync(new EdgeConflictResolution
        {
            OperationId = operationId,
            Kind = EdgeConflictResolutionKind.RetryLocal,
            ResolutionOperationId = retryOperationId,
            ResolutionVersionId = retryVersion
        }, default);

        resolved.IsError.Should().BeFalse(resolved.Message);
        resolved.Result.BaseVersionId.Should().Be(serverVersion);
        resolved.Result.VersionId.Should().Be(retryVersion);
        (await store.ReadUnresolvedConflictsAsync(default)).Result.Should().BeEmpty();
        (await store.ReadPendingOperationsAsync(10, default)).Result.Should().ContainSingle(x =>
            x.OperationId == retryOperationId && x.BaseVersionId == serverVersion);
    }

    [Fact]
    public async Task ServerWinsRequiresWinningVersionToBeAppliedLocally()
    {
        var operationId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var serverVersion = Guid.NewGuid();
        using var store = CreateStore();
        await store.ApplyLocalMutationAsync(NewMutation(operationId, entityId, Guid.NewGuid(), "{\"local\":true}"), default);
        (await store.CommitExchangeAsync(new SyncCommit
        {
            CommittedUtc = DateTime.UtcNow,
            NextPullCheckpoint = "cp-conflict",
            OperationResults = new[] { new SyncOperationResult
            {
                OperationId = operationId, Disposition = SyncOperationDisposition.Conflict,
                ResultVersionId = serverVersion, Code = "BASE_VERSION_CONFLICT"
            }}
        }, default)).IsError.Should().BeFalse();

        var premature = await store.ResolveConflictAsync(new EdgeConflictResolution
        {
            OperationId = operationId, Kind = EdgeConflictResolutionKind.ServerWins
        }, default);
        premature.IsError.Should().BeTrue();
        (await store.ReadUnresolvedConflictsAsync(default)).Result.Should().ContainSingle();

        (await store.CommitExchangeAsync(new SyncCommit
        {
            CommittedUtc = DateTime.UtcNow,
            NextPullCheckpoint = "cp-server",
            RemoteChanges = new[] { new SyncRemoteChange
            {
                ChangeId = "server-change", EntityId = entityId, EntityType = "holon",
                Kind = SyncOperationKind.Upsert, VersionId = serverVersion,
                PayloadJson = "{\"server\":true}", ChangedUtc = DateTime.UtcNow
            }}
        }, default)).IsError.Should().BeFalse();

        var resolved = await store.ResolveConflictAsync(new EdgeConflictResolution
        {
            OperationId = operationId, Kind = EdgeConflictResolutionKind.ServerWins
        }, default);
        resolved.IsError.Should().BeFalse(resolved.Message);
        (await store.ReadUnresolvedConflictsAsync(default)).Result.Should().BeEmpty();
        (await store.LoadEntityAsync("holon", entityId, default)).Result.VersionId.Should().Be(serverVersion);
    }

    [Fact]
    public async Task OutOfOrderRemoteUpdateCannotOverwriteANewerLocalTombstoneOrAdvanceCheckpoint()
    {
        var entityId = Guid.NewGuid();
        var originalVersion = Guid.NewGuid();
        var tombstoneVersion = Guid.NewGuid();
        using var store = CreateStore();
        await store.ApplyLocalMutationAsync(NewMutation(Guid.NewGuid(), entityId, originalVersion, "{\"v\":1}"), default);
        var delete = NewMutation(Guid.NewGuid(), entityId, tombstoneVersion, null);
        delete.Kind = SyncOperationKind.Delete;
        delete.BaseVersionId = originalVersion;
        (await store.ApplyLocalMutationAsync(delete, default)).IsError.Should().BeFalse();

        var staleUpdate = new SyncRemoteChange
        {
            ChangeId = "stale-update",
            EntityId = entityId,
            EntityType = "holon",
            Kind = SyncOperationKind.Upsert,
            PreviousVersionId = originalVersion,
            VersionId = Guid.NewGuid(),
            PayloadJson = "{\"stale\":true}",
            ChangedUtc = DateTime.UtcNow
        };
        var committed = await store.CommitExchangeAsync(new SyncCommit
        {
            CommittedUtc = DateTime.UtcNow,
            NextPullCheckpoint = "must-not-advance",
            RemoteChanges = new[] { staleUpdate }
        }, default);

        committed.IsError.Should().BeTrue();
        committed.ErrorCode.Should().Be("EDGE_SQLITE_REMOTE_VERSION_CONFLICT");
        committed.Exception.Should().BeAssignableTo<InvalidOperationException>();
        (await store.GetCheckpointAsync(default)).Result.PullCheckpoint.Should().BeNull();
        var current = (await store.LoadEntityAsync("holon", entityId, default)).Result;
        current.IsDeleted.Should().BeTrue();
        current.VersionId.Should().Be(tombstoneVersion);
    }

    [Fact]
    public async Task CausallyOrderedRemoteDeleteAdvancesAnExistingEntityToATombstone()
    {
        var entityId = Guid.NewGuid();
        var originalVersion = Guid.NewGuid();
        var remoteTombstoneVersion = Guid.NewGuid();
        using var store = CreateStore();
        await store.ApplyReplicatedMutationAsync(new HostedSyncFanOutItem
        {
            OperationId = Guid.NewGuid(), AvatarId = _avatarId, EntityId = entityId,
            EntityType = "holon", Kind = SyncOperationKind.Upsert, VersionId = originalVersion,
            PayloadJson = "{\"v\":1}"
        }, default);

        var committed = await store.CommitExchangeAsync(new SyncCommit
        {
            CommittedUtc = DateTime.UtcNow,
            NextPullCheckpoint = "delete-checkpoint",
            RemoteChanges = new[] { new SyncRemoteChange
            {
                ChangeId = "remote-delete", EntityId = entityId, EntityType = "holon",
                Kind = SyncOperationKind.Delete, PreviousVersionId = originalVersion,
                VersionId = remoteTombstoneVersion, ChangedUtc = DateTime.UtcNow
            } }
        }, default);

        committed.IsError.Should().BeFalse(committed.Exception?.Message);
        var current = (await store.LoadEntityAsync("holon", entityId, default)).Result;
        current.IsDeleted.Should().BeTrue();
        current.VersionId.Should().Be(remoteTombstoneVersion);
        (await store.GetCheckpointAsync(default)).Result.PullCheckpoint.Should().Be("delete-checkpoint");
    }

    [Fact]
    public async Task ReplicatedMutationAndOperationIdAreCommittedAtomicallyAndReplayIsNoOp()
    {
        var entityId = Guid.NewGuid();
        var first = new HostedSyncFanOutItem
        {
            OperationId = Guid.NewGuid(), AvatarId = _avatarId, EntityId = entityId,
            EntityType = "holon", Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(),
            PayloadJson = "{\"v\":1}"
        };
        var second = new HostedSyncFanOutItem
        {
            OperationId = Guid.NewGuid(), AvatarId = _avatarId, EntityId = entityId,
            EntityType = "holon", Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(),
            PayloadJson = "{\"v\":2}"
        };

        using (var store = CreateStore())
        {
            (await store.ApplyReplicatedMutationAsync(first, default)).IsError.Should().BeFalse();
            (await store.ApplyReplicatedMutationAsync(second, default)).IsError.Should().BeFalse();
        }
        using (var reopened = CreateStore())
        {
            var replay = await reopened.ApplyReplicatedMutationAsync(first, default);
            replay.IsError.Should().BeFalse(replay.Message);
            var entity = (await reopened.LoadEntityAsync("holon", entityId, default)).Result;
            entity.VersionId.Should().Be(second.VersionId);
            entity.PayloadJson.Should().Be("{\"v\":2}");
        }
    }

    [Fact]
    public async Task AuthoritativeSnapshotIsStagedAcrossRestartAndReplacesLiveStateOnlyWhenComplete()
    {
        var staleId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var snapshotId = Guid.NewGuid();
        using (var store = CreateStore())
        {
            await store.ApplyReplicatedMutationAsync(new HostedSyncFanOutItem
            {
                OperationId = Guid.NewGuid(), AvatarId = _avatarId, EntityId = staleId,
                EntityType = "holon", Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(),
                PayloadJson = "{\"stale\":true}"
            }, default);
            var firstPage = await store.CommitExchangeAsync(new SyncCommit
            {
                IsAuthoritativeSnapshot = true, SnapshotId = snapshotId, SnapshotPageIndex = 0,
                SnapshotComplete = false, NextPullCheckpoint = "snapshot-checkpoint",
                CommittedUtc = DateTime.UtcNow,
                RemoteChanges = new[] { NewRemoteChange("snapshot-1", firstId) }
            }, default);
            firstPage.IsError.Should().BeFalse(firstPage.Message);
            (await store.LoadEntityAsync("holon", staleId, default)).Result.Should().NotBeNull();
            (await store.LoadEntityAsync("holon", firstId, default)).Result.Should().BeNull();
        }
        using (var reopened = CreateStore())
        {
            var cursor = (await reopened.GetCheckpointAsync(default)).Result;
            cursor.SnapshotId.Should().Be(snapshotId);
            cursor.SnapshotPageIndex.Should().Be(1);
            var finalPage = await reopened.CommitExchangeAsync(new SyncCommit
            {
                IsAuthoritativeSnapshot = true, SnapshotId = snapshotId, SnapshotPageIndex = 1,
                SnapshotComplete = true, NextPullCheckpoint = "snapshot-checkpoint",
                CommittedUtc = DateTime.UtcNow,
                RemoteChanges = new[] { NewRemoteChange("snapshot-2", secondId) }
            }, default);
            finalPage.IsError.Should().BeFalse(finalPage.Message);
            (await reopened.LoadEntityAsync("holon", staleId, default)).Result.Should().BeNull();
            (await reopened.LoadEntityAsync("holon", firstId, default)).Result.Should().NotBeNull();
            (await reopened.LoadEntityAsync("holon", secondId, default)).Result.Should().NotBeNull();
            var completed = (await reopened.GetCheckpointAsync(default)).Result;
            completed.PullCheckpoint.Should().Be("snapshot-checkpoint");
            completed.SnapshotId.Should().Be(Guid.Empty);
            completed.SnapshotPageIndex.Should().Be(0);
        }
    }

    [Fact]
    public async Task DuplicateEntityAcrossSnapshotPagesRollsBackWithoutAdvancingCursor()
    {
        var snapshotId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        using var store = CreateStore();
        var firstPage = await store.CommitExchangeAsync(new SyncCommit
        {
            IsAuthoritativeSnapshot = true, SnapshotId = snapshotId, SnapshotPageIndex = 0,
            SnapshotComplete = false, NextPullCheckpoint = "snapshot-checkpoint",
            CommittedUtc = DateTime.UtcNow,
            RemoteChanges = new[] { NewRemoteChange("snapshot-1", firstId) }
        }, default);
        firstPage.IsError.Should().BeFalse(firstPage.Message);

        var duplicatePage = await store.CommitExchangeAsync(new SyncCommit
        {
            IsAuthoritativeSnapshot = true, SnapshotId = snapshotId, SnapshotPageIndex = 1,
            SnapshotComplete = true, NextPullCheckpoint = "snapshot-checkpoint",
            CommittedUtc = DateTime.UtcNow,
            RemoteChanges = new[] { NewRemoteChange("snapshot-duplicate", firstId) }
        }, default);

        duplicatePage.IsError.Should().BeTrue();
        duplicatePage.ErrorCode.Should().Be("EDGE_SQLITE_EXCHANGE_COMMIT_FAILED");
        var cursor = (await store.GetCheckpointAsync(default)).Result;
        cursor.SnapshotId.Should().Be(snapshotId);
        cursor.SnapshotPageIndex.Should().Be(1);
        cursor.PullCheckpoint.Should().BeNull();

        var validFinalPage = await store.CommitExchangeAsync(new SyncCommit
        {
            IsAuthoritativeSnapshot = true, SnapshotId = snapshotId, SnapshotPageIndex = 1,
            SnapshotComplete = true, NextPullCheckpoint = "snapshot-checkpoint",
            CommittedUtc = DateTime.UtcNow,
            RemoteChanges = new[] { NewRemoteChange("snapshot-2", secondId) }
        }, default);
        validFinalPage.IsError.Should().BeFalse(validFinalPage.Message);
        (await store.LoadEntityAsync("holon", firstId, default)).Result.Should().NotBeNull();
        (await store.LoadEntityAsync("holon", secondId, default)).Result.Should().NotBeNull();
    }

    [Fact]
    public async Task ExplicitReplacementSnapshotDiscardsOnlyObsoleteStaging()
    {
        var obsoleteSnapshot = Guid.NewGuid();
        var replacementSnapshot = Guid.NewGuid();
        var obsoleteEntity = Guid.NewGuid();
        var replacementEntity = Guid.NewGuid();
        using var store = CreateStore();
        (await store.CommitExchangeAsync(new SyncCommit
        {
            IsAuthoritativeSnapshot = true, SnapshotId = obsoleteSnapshot, SnapshotPageIndex = 0,
            SnapshotComplete = false, NextPullCheckpoint = "obsolete", CommittedUtc = DateTime.UtcNow,
            RemoteChanges = new[] { NewRemoteChange("obsolete", obsoleteEntity) }
        }, default)).IsError.Should().BeFalse();

        var replaced = await store.CommitExchangeAsync(new SyncCommit
        {
            IsAuthoritativeSnapshot = true, SnapshotId = replacementSnapshot,
            ReplacesSnapshotId = obsoleteSnapshot, SnapshotPageIndex = 0, SnapshotComplete = true,
            NextPullCheckpoint = "replacement", CommittedUtc = DateTime.UtcNow,
            RemoteChanges = new[] { NewRemoteChange("replacement", replacementEntity) }
        }, default);

        replaced.IsError.Should().BeFalse(replaced.Message);
        (await store.LoadEntityAsync("holon", obsoleteEntity, default)).Result.Should().BeNull();
        (await store.LoadEntityAsync("holon", replacementEntity, default)).Result.Should().NotBeNull();
        (await store.GetCheckpointAsync(default)).Result.PullCheckpoint.Should().Be("replacement");
    }

    [Theory]
    [InlineData(EdgeSQLiteTransactionBoundary.LocalEntityWrittenBeforeOutbox)]
    [InlineData(EdgeSQLiteTransactionBoundary.LocalOutboxWrittenBeforeCommit)]
    public async Task LocalMutationRollsBackAtEveryInstrumentedTransactionBoundary(
        EdgeSQLiteTransactionBoundary boundary)
    {
        var entityId = Guid.NewGuid();
        using (var failing = new EdgeSQLiteSyncStateStore(_databasePath, new ThrowAtBoundary(boundary)))
        {
            var result = await failing.ApplyLocalMutationAsync(
                NewMutation(Guid.NewGuid(), entityId, Guid.NewGuid(), "{\"local\":true}"), default);
            result.IsError.Should().BeTrue();
            result.ErrorCode.Should().Be("EDGE_SQLITE_LOCAL_MUTATION_FAILED");
        }
        using var reopened = CreateStore();
        (await reopened.LoadEntityAsync("holon", entityId, default)).Result.Should().BeNull();
        (await reopened.GetPendingOperationCountAsync(default)).Result.Should().Be(0);
    }

    [Theory]
    [InlineData(EdgeSQLiteTransactionBoundary.ReplicatedEntityWrittenBeforeInbox)]
    [InlineData(EdgeSQLiteTransactionBoundary.ReplicationInboxWrittenBeforeCommit)]
    public async Task ReplicationRollsBackEntityAndIdempotencyRecordAtEveryBoundary(
        EdgeSQLiteTransactionBoundary boundary)
    {
        var entityId = Guid.NewGuid();
        var item = new HostedSyncFanOutItem
        {
            OperationId = Guid.NewGuid(), AvatarId = _avatarId, EntityId = entityId,
            EntityType = "holon", Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(),
            PayloadJson = "{\"replicated\":true}"
        };
        using (var failing = new EdgeSQLiteSyncStateStore(_databasePath, new ThrowAtBoundary(boundary)))
            (await failing.ApplyReplicatedMutationAsync(item, default)).IsError.Should().BeTrue();

        using var reopened = CreateStore();
        (await reopened.LoadEntityAsync("holon", entityId, default)).Result.Should().BeNull();
        var retry = await reopened.ApplyReplicatedMutationAsync(item, default);
        retry.IsError.Should().BeFalse(retry.Message);
        (await reopened.LoadEntityAsync("holon", entityId, default)).Result.VersionId.Should().Be(item.VersionId);
    }

    [Theory]
    [InlineData(EdgeSQLiteTransactionBoundary.ExchangeOperationResultsWritten)]
    [InlineData(EdgeSQLiteTransactionBoundary.ExchangeRemoteChangesWritten)]
    [InlineData(EdgeSQLiteTransactionBoundary.ExchangeCheckpointWrittenBeforeCommit)]
    public async Task ExchangeRollsBackAcknowledgementRemoteChangeAndCheckpointAtEveryBoundary(
        EdgeSQLiteTransactionBoundary boundary)
    {
        var operationId = Guid.NewGuid();
        var remoteEntityId = Guid.NewGuid();
        using (var seed = CreateStore())
            (await seed.ApplyLocalMutationAsync(NewMutation(operationId, Guid.NewGuid(), Guid.NewGuid(), "{}"), default))
                .IsError.Should().BeFalse();
        using (var failing = new EdgeSQLiteSyncStateStore(_databasePath, new ThrowAtBoundary(boundary)))
        {
            var result = await failing.CommitExchangeAsync(new SyncCommit
            {
                CommittedUtc = DateTime.UtcNow, NextPullCheckpoint = "must-rollback",
                OperationResults = new[] { new SyncOperationResult
                {
                    OperationId = operationId, Disposition = SyncOperationDisposition.Accepted,
                    ResultVersionId = Guid.NewGuid()
                } },
                RemoteChanges = new[] { NewRemoteChange("fault-change", remoteEntityId) }
            }, default);
            result.IsError.Should().BeTrue();
            result.ErrorCode.Should().Be("EDGE_SQLITE_EXCHANGE_COMMIT_FAILED");
        }
        using var reopened = CreateStore();
        (await reopened.ReadPendingOperationsAsync(10, default)).Result.Should().ContainSingle(x => x.OperationId == operationId);
        (await reopened.LoadEntityAsync("holon", remoteEntityId, default)).Result.Should().BeNull();
        (await reopened.GetCheckpointAsync(default)).Result.PullCheckpoint.Should().BeNull();
    }

    [Fact]
    public async Task EntityTypeEnumerationIsDeterministicAndExcludesTombstonesByDefault()
    {
        using var store = CreateStore();
        var first = Guid.NewGuid();
        var deleted = Guid.NewGuid();
        var second = Guid.NewGuid();
        await store.ApplyReplicatedMutationAsync(new HostedSyncFanOutItem
        {
            OperationId = Guid.NewGuid(), AvatarId = _avatarId, EntityId = first, EntityType = "holon",
            Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(), PayloadJson = "{\"first\":true}"
        }, default);
        await store.ApplyReplicatedMutationAsync(new HostedSyncFanOutItem
        {
            OperationId = Guid.NewGuid(), AvatarId = _avatarId, EntityId = deleted, EntityType = "holon",
            Kind = SyncOperationKind.Delete, VersionId = Guid.NewGuid()
        }, default);
        await store.ApplyReplicatedMutationAsync(new HostedSyncFanOutItem
        {
            OperationId = Guid.NewGuid(), AvatarId = _avatarId, EntityId = second, EntityType = "holon",
            Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(), PayloadJson = "{\"second\":true}"
        }, default);

        var active = await store.LoadEntitiesAsync("holon", false, default);
        active.IsError.Should().BeFalse(active.Message);
        active.Result.Select(x => x.EntityId).Should().BeEquivalentTo(new[] { first, second });

        var all = await store.LoadEntitiesAsync("holon", true, default);
        all.Result.Should().HaveCount(3);
        all.Result.Single(x => x.EntityId == deleted).IsDeleted.Should().BeTrue();
    }

    private EdgeSQLiteSyncStateStore CreateStore() => new(_databasePath);

    private void CreateLegacyOutbox(Guid operationId, int state, Guid? resultVersionId)
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE sync_outbox(
 operation_id TEXT PRIMARY KEY, device_id TEXT NOT NULL, avatar_id TEXT NOT NULL,
 device_sequence INTEGER NOT NULL, entity_id TEXT NOT NULL, entity_type TEXT NOT NULL,
 kind INTEGER NOT NULL, base_version_id TEXT NOT NULL, payload_json TEXT NULL,
 created_utc TEXT NOT NULL, state INTEGER NOT NULL, result_version_id TEXT NULL,
 result_code TEXT NULL, result_message TEXT NULL, completed_utc TEXT NULL,
 UNIQUE(device_id, device_sequence));
INSERT INTO sync_outbox(operation_id,device_id,avatar_id,device_sequence,entity_id,entity_type,
 kind,base_version_id,payload_json,created_utc,state,result_version_id)
VALUES($operation_id,$device_id,$avatar_id,1,$entity_id,'holon',0,$base_version_id,'{}',$created_utc,$state,$result_version_id);";
        command.Parameters.AddWithValue("$operation_id", operationId.ToString("D"));
        command.Parameters.AddWithValue("$device_id", _deviceId.ToString("D"));
        command.Parameters.AddWithValue("$avatar_id", _avatarId.ToString("D"));
        command.Parameters.AddWithValue("$entity_id", Guid.NewGuid().ToString("D"));
        command.Parameters.AddWithValue("$base_version_id", Guid.Empty.ToString("D"));
        command.Parameters.AddWithValue("$created_utc", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$state", state);
        command.Parameters.AddWithValue("$result_version_id",
            resultVersionId.HasValue ? resultVersionId.Value.ToString("D") : DBNull.Value);
        command.ExecuteNonQuery();
    }

    private EdgeLocalMutation NewMutation(Guid operationId, Guid entityId, Guid versionId, string json) => new()
    {
        OperationId = operationId, DeviceId = _deviceId, AvatarId = _avatarId, EntityId = entityId,
        EntityType = "holon", Kind = SyncOperationKind.Upsert, BaseVersionId = Guid.Empty,
        LocalVersionId = versionId, PayloadJson = json, CreatedUtc = DateTime.UtcNow
    };

    private static SyncRemoteChange NewRemoteChange(string changeId, Guid entityId) => new()
    {
        ChangeId = changeId, EntityId = entityId, EntityType = "holon", Kind = SyncOperationKind.Upsert,
        VersionId = Guid.NewGuid(), PayloadJson = "{\"remote\":true}", ChangedUtc = DateTime.UtcNow
    };

    private sealed class ThrowAtBoundary : IEdgeSQLiteTransactionFaultInjector
    {
        private readonly EdgeSQLiteTransactionBoundary _boundary;
        public ThrowAtBoundary(EdgeSQLiteTransactionBoundary boundary) => _boundary = boundary;
        public Task OnBoundaryAsync(EdgeSQLiteTransactionBoundary boundary, CancellationToken cancellationToken)
        {
            if (boundary == _boundary) throw new InvalidOperationException($"Injected failure at {boundary}.");
            return Task.CompletedTask;
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
