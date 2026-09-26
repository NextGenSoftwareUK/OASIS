using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS
{
    /// <summary>
    /// Lightweight authoritative Edge store. Entity state, local outbox operations, inbound changes,
    /// conflicts and the pull checkpoint share one SQLite transaction boundary.
    /// </summary>
    public sealed partial class EdgeSQLiteSyncStateStore : IHyperDriveSyncStateStore,
        IHyperDriveIdempotentReplicationTarget, IDisposable
    {
        private readonly string _connectionString;
        private readonly IEdgeSQLiteTransactionFaultInjector _faultInjector;
        public string ReplicationTargetId => "EdgeSQLiteOASIS";

        public EdgeSQLiteSyncStateStore(string databasePath,
            IEdgeSQLiteTransactionFaultInjector faultInjector = null)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                throw new ArgumentException("A SQLite database path is required.", nameof(databasePath));

            SQLitePCL.Batteries_V2.Init();
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();
            _faultInjector = faultInjector;
            Initialize();
        }

        public async Task<OASISResult<SyncOperation>> ApplyLocalMutationAsync(
            EdgeLocalMutation mutation, CancellationToken cancellationToken)
        {
            var result = new OASISResult<SyncOperation>();
            try
            {
                ValidateMutation(mutation);
                using (var connection = await OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    var existing = await LoadOperationAsync(connection, transaction, mutation.OperationId, cancellationToken)
                        .ConfigureAwait(false);
                    if (existing != null)
                    {
                        if (!Matches(existing, mutation))
                            throw new InvalidOperationException("The operation id is already assigned to a different immutable mutation.");
                        transaction.Commit();
                        result.Result = existing;
                        result.IsSaved = true;
                        result.Message = "The local mutation was already committed.";
                        return result;
                    }

                    long sequence = await NextDeviceSequenceAsync(connection, transaction, mutation.DeviceId, cancellationToken)
                        .ConfigureAwait(false);
                    DateTime createdUtc = mutation.CreatedUtc == default ? DateTime.UtcNow : mutation.CreatedUtc;

                    // Commands are durable intents, not speculative authoritative entity state. Only the
                    // hosted command result and CDC domain changes may update the local projection.
                    if (mutation.Kind != SyncOperationKind.Command)
                        await ApplyEntityAsync(connection, transaction, mutation.EntityId, mutation.EntityType,
                            mutation.Kind, mutation.LocalVersionId, mutation.PayloadJson, createdUtc, cancellationToken)
                            .ConfigureAwait(false);
                    await InjectAsync(EdgeSQLiteTransactionBoundary.LocalEntityWrittenBeforeOutbox, cancellationToken)
                        .ConfigureAwait(false);

                    using (var command = CreateCommand(connection, transaction, @"
INSERT INTO sync_outbox(operation_id, device_id, avatar_id, device_sequence, entity_id, entity_type,
 kind, base_version_id, local_version_id, payload_json, created_utc, state)
VALUES($operation_id, $device_id, $avatar_id, $sequence, $entity_id, $entity_type,
 $kind, $base_version_id, $local_version_id, $payload_json, $created_utc, 0);"))
                    {
                        Add(command, "$operation_id", mutation.OperationId.ToString("D"));
                        Add(command, "$device_id", mutation.DeviceId.ToString("D"));
                        Add(command, "$avatar_id", mutation.AvatarId.ToString("D"));
                        Add(command, "$sequence", sequence);
                        Add(command, "$entity_id", mutation.EntityId.ToString("D"));
                        Add(command, "$entity_type", mutation.EntityType);
                        Add(command, "$kind", (int)mutation.Kind);
                        Add(command, "$base_version_id", mutation.BaseVersionId.ToString("D"));
                        Add(command, "$local_version_id", mutation.LocalVersionId.ToString("D"));
                        Add(command, "$payload_json", (object)mutation.PayloadJson ?? DBNull.Value);
                        Add(command, "$created_utc", createdUtc.ToString("O"));
                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }

                    await InjectAsync(EdgeSQLiteTransactionBoundary.LocalOutboxWrittenBeforeCommit, cancellationToken)
                        .ConfigureAwait(false);

                    transaction.Commit();
                    result.Result = new SyncOperation
                    {
                        OperationId = mutation.OperationId,
                        DeviceId = mutation.DeviceId,
                        AvatarId = mutation.AvatarId,
                        DeviceSequence = sequence,
                        EntityId = mutation.EntityId,
                        EntityType = mutation.EntityType,
                        Kind = mutation.Kind,
                        BaseVersionId = mutation.BaseVersionId,
                        VersionId = mutation.LocalVersionId,
                        PayloadJson = mutation.PayloadJson,
                        CreatedUtc = createdUtc
                    };
                    result.IsSaved = true;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetError(result, "EDGE_SQLITE_LOCAL_MUTATION_FAILED", "The local mutation and sync operation were not committed.", ex); }
            return result;
        }

        public async Task<OASISResult<SyncCheckpoint>> GetCheckpointAsync(CancellationToken cancellationToken)
        {
            var result = new OASISResult<SyncCheckpoint>();
            try
            {
                using (var connection = await OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT pull_checkpoint, last_successful_sync_utc, snapshot_id, snapshot_page_index FROM sync_state WHERE singleton_id = 1;";
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                        result.Result = new SyncCheckpoint
                        {
                            PullCheckpoint = reader.IsDBNull(0) ? null : reader.GetString(0),
                            LastSuccessfulSyncUtc = reader.IsDBNull(1) ? (DateTime?)null : DateTime.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind),
                            SnapshotId = reader.IsDBNull(2) ? Guid.Empty : Guid.Parse(reader.GetString(2)),
                            SnapshotPageIndex = reader.GetInt32(3)
                        };
                        result.IsLoaded = true;
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetError(result, "EDGE_SQLITE_CHECKPOINT_READ_FAILED", "The sync checkpoint could not be read.", ex); }
            return result;
        }

        public async Task<OASISResult<bool>> ApplyReplicatedMutationAsync(HostedSyncFanOutItem item,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (item == null || item.OperationId == Guid.Empty || item.EntityId == Guid.Empty ||
                    item.VersionId == Guid.Empty || string.IsNullOrWhiteSpace(item.EntityType))
                    throw new ArgumentException("A complete hosted fan-out item is required.", nameof(item));
                if (item.Kind != SyncOperationKind.Upsert && item.Kind != SyncOperationKind.Delete)
                    throw new ArgumentException("Only entity upserts and deletes can be replicated.", nameof(item));
                if (item.Kind == SyncOperationKind.Upsert && string.IsNullOrWhiteSpace(item.PayloadJson))
                    throw new ArgumentException("A replicated upsert requires a payload.", nameof(item));

                using (var connection = await OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    using (var exists = CreateCommand(connection, transaction,
                        "SELECT 1 FROM replication_inbox WHERE operation_id=$operation_id;"))
                    {
                        Add(exists, "$operation_id", item.OperationId.ToString("D"));
                        if (await exists.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) != null)
                        {
                            transaction.Commit();
                            result.Result = true;
                            result.IsSaved = true;
                            result.Message = "The replicated mutation was already applied.";
                            return result;
                        }
                    }

                    DateTime appliedUtc = DateTime.UtcNow;
                    await ApplyEntityAsync(connection, transaction, item.EntityId, item.EntityType,
                        item.Kind, item.VersionId, item.PayloadJson, appliedUtc, cancellationToken)
                        .ConfigureAwait(false);
                    await InjectAsync(EdgeSQLiteTransactionBoundary.ReplicatedEntityWrittenBeforeInbox, cancellationToken)
                        .ConfigureAwait(false);
                    using (var record = CreateCommand(connection, transaction, @"
INSERT INTO replication_inbox(operation_id, applied_utc) VALUES($operation_id, $applied_utc);"))
                    {
                        Add(record, "$operation_id", item.OperationId.ToString("D"));
                        Add(record, "$applied_utc", appliedUtc.ToString("O"));
                        await record.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                    await InjectAsync(EdgeSQLiteTransactionBoundary.ReplicationInboxWrittenBeforeCommit, cancellationToken)
                        .ConfigureAwait(false);
                    transaction.Commit();
                }
                result.Result = true;
                result.IsSaved = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                SetError(result, "EDGE_SQLITE_REPLICATION_FAILED",
                    "The replicated mutation and its idempotency record were not committed.", ex);
            }
            return result;
        }

        public async Task<OASISResult<EdgeStoredEntity>> LoadEntityAsync(
            string entityType, Guid entityId, CancellationToken cancellationToken)
        {
            var result = new OASISResult<EdgeStoredEntity>();
            try
            {
                using (var connection = await OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT version_id, payload_json, is_deleted, changed_utc
FROM edge_entities WHERE entity_type=$entity_type AND entity_id=$entity_id;";
                    Add(command, "$entity_type", entityType);
                    Add(command, "$entity_id", entityId.ToString("D"));
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            result.IsLoaded = true;
                            return result;
                        }
                        result.Result = new EdgeStoredEntity
                        {
                            EntityType = entityType,
                            EntityId = entityId,
                            VersionId = Guid.Parse(reader.GetString(0)),
                            PayloadJson = reader.IsDBNull(1) ? null : reader.GetString(1),
                            IsDeleted = reader.GetInt32(2) != 0,
                            ChangedUtc = DateTime.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind)
                        };
                        result.IsLoaded = true;
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetError(result, "EDGE_SQLITE_ENTITY_READ_FAILED", "The local entity could not be read.", ex); }
            return result;
        }

        public async Task<OASISResult<IReadOnlyList<EdgeStoredEntity>>> LoadEntitiesAsync(
            string entityType, bool includeDeleted, CancellationToken cancellationToken)
        {
            var result = new OASISResult<IReadOnlyList<EdgeStoredEntity>>();
            if (string.IsNullOrWhiteSpace(entityType))
            {
                SetError(result, "EDGE_SQLITE_ENTITY_TYPE_REQUIRED", "An entity type is required.", null);
                return result;
            }
            try
            {
                var entities = new List<EdgeStoredEntity>();
                using (var connection = await OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT entity_id, version_id, payload_json, is_deleted, changed_utc
FROM edge_entities WHERE entity_type=$entity_type AND ($include_deleted=1 OR is_deleted=0)
ORDER BY changed_utc, entity_id;";
                    Add(command, "$entity_type", entityType);
                    Add(command, "$include_deleted", includeDeleted ? 1 : 0);
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            entities.Add(new EdgeStoredEntity
                            {
                                EntityType = entityType,
                                EntityId = Guid.Parse(reader.GetString(0)),
                                VersionId = Guid.Parse(reader.GetString(1)),
                                PayloadJson = reader.IsDBNull(2) ? null : reader.GetString(2),
                                IsDeleted = reader.GetInt32(3) != 0,
                                ChangedUtc = DateTime.Parse(reader.GetString(4), null,
                                    System.Globalization.DateTimeStyles.RoundtripKind)
                            });
                        }
                    }
                }
                result.Result = entities;
                result.IsLoaded = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                SetError(result, "EDGE_SQLITE_ENTITIES_READ_FAILED", "The local entities could not be read.", ex);
            }
            return result;
        }

        public async Task<OASISResult<IReadOnlyList<SyncOperation>>> ReadPendingOperationsAsync(int maximumCount, CancellationToken cancellationToken)
        {
            var result = new OASISResult<IReadOnlyList<SyncOperation>>();
            if (maximumCount <= 0)
            {
                SetError(result, "EDGE_SQLITE_INVALID_BATCH_SIZE", "The pending operation batch size must be greater than zero.", null);
                return result;
            }

            try
            {
                var operations = new List<SyncOperation>();
                using (var connection = await OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT operation_id, device_id, avatar_id, device_sequence, entity_id,
 entity_type, kind, base_version_id, local_version_id, payload_json, created_utc
FROM sync_outbox WHERE state = 0 ORDER BY device_sequence LIMIT $maximum_count;";
                    Add(command, "$maximum_count", maximumCount);
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            operations.Add(new SyncOperation
                            {
                                OperationId = Guid.Parse(reader.GetString(0)), DeviceId = Guid.Parse(reader.GetString(1)),
                                AvatarId = Guid.Parse(reader.GetString(2)), DeviceSequence = reader.GetInt64(3),
                                EntityId = Guid.Parse(reader.GetString(4)), EntityType = reader.GetString(5),
                                Kind = (SyncOperationKind)reader.GetInt32(6), BaseVersionId = Guid.Parse(reader.GetString(7)),
                                VersionId = Guid.Parse(reader.GetString(8)),
                                PayloadJson = reader.IsDBNull(9) ? null : reader.GetString(9),
                                CreatedUtc = DateTime.Parse(reader.GetString(10), null, System.Globalization.DateTimeStyles.RoundtripKind)
                            });
                        }
                    }
                }
                result.Result = operations;
                result.IsLoaded = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetError(result, "EDGE_SQLITE_OUTBOX_READ_FAILED", "Pending sync operations could not be read.", ex); }
            return result;
        }

        public async Task<OASISResult<long>> GetPendingOperationCountAsync(CancellationToken cancellationToken)
        {
            var result = new OASISResult<long>();
            try
            {
                using (var connection = await OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM sync_outbox WHERE state = 0;";
                    result.Result = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)
                        .ConfigureAwait(false));
                    result.IsLoaded = true;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                SetError(result, "EDGE_SQLITE_PENDING_COUNT_FAILED",
                    "The number of queued synchronization operations could not be read.", ex);
            }
            return result;
        }

        public async Task<OASISResult<bool>> CommitExchangeAsync(SyncCommit commit, CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (commit == null) throw new ArgumentNullException(nameof(commit));
                using (var connection = await OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    foreach (var operationResult in commit.OperationResults)
                        await CommitOperationResultAsync(connection, transaction, operationResult, commit.CommittedUtc, cancellationToken).ConfigureAwait(false);
                    await InjectAsync(EdgeSQLiteTransactionBoundary.ExchangeOperationResultsWritten, cancellationToken)
                        .ConfigureAwait(false);

                    if (commit.IsAuthoritativeSnapshot)
                    {
                        if (commit.SnapshotId == Guid.Empty || commit.SnapshotPageIndex < 0)
                            throw new InvalidOperationException("The snapshot commit cursor is invalid.");
                        Guid currentSnapshotId = Guid.Empty;
                        int expectedPageIndex;
                        using (var cursor = CreateCommand(connection, transaction,
                            "SELECT snapshot_id, snapshot_page_index FROM sync_state WHERE singleton_id=1;"))
                        using (var reader = await cursor.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                        {
                            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                            currentSnapshotId = reader.IsDBNull(0) ? Guid.Empty : Guid.Parse(reader.GetString(0));
                            expectedPageIndex = reader.GetInt32(1);
                        }
                        bool replacesCurrent = currentSnapshotId != Guid.Empty &&
                            commit.ReplacesSnapshotId == currentSnapshotId && commit.SnapshotId != currentSnapshotId &&
                            commit.SnapshotPageIndex == 0;
                        if ((currentSnapshotId == Guid.Empty && commit.SnapshotPageIndex != 0) ||
                            (currentSnapshotId != Guid.Empty && !replacesCurrent &&
                             (currentSnapshotId != commit.SnapshotId || expectedPageIndex != commit.SnapshotPageIndex)))
                            throw new InvalidOperationException("The snapshot page is not the next page in the durable rebase sequence.");
                        if (currentSnapshotId == Guid.Empty || replacesCurrent)
                        {
                            using var clear = CreateCommand(connection, transaction, "DELETE FROM sync_snapshot_entities;");
                            await clear.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                        }
                        foreach (var change in commit.RemoteChanges)
                        {
                            using var stage = CreateCommand(connection, transaction, @"INSERT INTO sync_snapshot_entities(
snapshot_id,entity_type,entity_id,version_id,payload_json,is_deleted,changed_utc)
VALUES($snapshot_id,$entity_type,$entity_id,$version_id,$payload_json,$is_deleted,$changed_utc);");
                            Add(stage, "$snapshot_id", commit.SnapshotId.ToString("D"));
                            Add(stage, "$entity_type", change.EntityType); Add(stage, "$entity_id", change.EntityId.ToString("D"));
                            Add(stage, "$version_id", change.VersionId.ToString("D"));
                            Add(stage, "$payload_json", change.Kind == SyncOperationKind.Delete ? DBNull.Value : (object)change.PayloadJson ?? DBNull.Value);
                            Add(stage, "$is_deleted", change.Kind == SyncOperationKind.Delete ? 1 : 0);
                            Add(stage, "$changed_utc", change.ChangedUtc.ToString("O"));
                            await stage.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                        }
                        if (commit.SnapshotComplete)
                        {
                            using (var replace = CreateCommand(connection, transaction, @"DELETE FROM edge_entities;
INSERT INTO edge_entities(entity_type,entity_id,version_id,payload_json,is_deleted,changed_utc)
SELECT entity_type,entity_id,version_id,payload_json,is_deleted,changed_utc
FROM sync_snapshot_entities WHERE snapshot_id=$snapshot_id;
DELETE FROM sync_snapshot_entities;"))
                            {
                                Add(replace, "$snapshot_id", commit.SnapshotId.ToString("D"));
                                await replace.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                            }
                        }
                        using (var cursorUpdate = CreateCommand(connection, transaction, @"UPDATE sync_state SET
snapshot_id=$snapshot_id,snapshot_page_index=$snapshot_page_index WHERE singleton_id=1;"))
                        {
                            Add(cursorUpdate, "$snapshot_id", commit.SnapshotComplete ? DBNull.Value : (object)commit.SnapshotId.ToString("D"));
                            Add(cursorUpdate, "$snapshot_page_index", commit.SnapshotComplete ? 0 : commit.SnapshotPageIndex + 1);
                            await cursorUpdate.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                        }
                    }
                    else foreach (var change in commit.RemoteChanges)
                    {
                        if (!await WasRemoteChangeAppliedAsync(connection, transaction, change.ChangeId, cancellationToken).ConfigureAwait(false))
                        {
                            await EnsureRemoteChangeCanAdvanceEntityAsync(connection, transaction, change, cancellationToken)
                                .ConfigureAwait(false);
                            await ApplyEntityAsync(connection, transaction, change.EntityId, change.EntityType, change.Kind,
                                change.VersionId, change.PayloadJson, change.ChangedUtc, cancellationToken).ConfigureAwait(false);
                            using (var command = CreateCommand(connection, transaction,
                                "INSERT INTO sync_inbox(change_id, applied_utc) VALUES($change_id, $applied_utc);"))
                            {
                                Add(command, "$change_id", change.ChangeId);
                                Add(command, "$applied_utc", commit.CommittedUtc.ToString("O"));
                                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }

                    await InjectAsync(EdgeSQLiteTransactionBoundary.ExchangeRemoteChangesWritten, cancellationToken)
                        .ConfigureAwait(false);

                    using (var command = CreateCommand(connection, transaction, @"
UPDATE sync_state SET pull_checkpoint = CASE WHEN $advance_checkpoint=1 THEN $checkpoint ELSE pull_checkpoint END,
last_successful_sync_utc = $committed_utc
WHERE singleton_id = 1;"))
                    {
                        Add(command, "$advance_checkpoint", !commit.IsAuthoritativeSnapshot || commit.SnapshotComplete ? 1 : 0);
                        Add(command, "$checkpoint", (object)commit.NextPullCheckpoint ?? DBNull.Value);
                        Add(command, "$committed_utc", commit.CommittedUtc.ToString("O"));
                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    }
                    await InjectAsync(EdgeSQLiteTransactionBoundary.ExchangeCheckpointWrittenBeforeCommit, cancellationToken)
                        .ConfigureAwait(false);
                    transaction.Commit();
                }
                result.Result = true;
                result.IsSaved = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (RemoteVersionConflictException ex)
            {
                SetError(result, "EDGE_SQLITE_REMOTE_VERSION_CONFLICT",
                    "The inbound change was not causally based on the current local entity version; the exchange and checkpoint were not committed.", ex);
            }
            catch (Exception ex) { SetError(result, "EDGE_SQLITE_EXCHANGE_COMMIT_FAILED", "The exchange was not committed; its checkpoint and operation states are unchanged.", ex); }
            return result;
        }

        private Task InjectAsync(EdgeSQLiteTransactionBoundary boundary, CancellationToken cancellationToken) =>
            _faultInjector == null ? Task.CompletedTask : _faultInjector.OnBoundaryAsync(boundary, cancellationToken);

        public async Task<OASISResult<IReadOnlyList<EdgeSyncConflict>>> ReadUnresolvedConflictsAsync(
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<IReadOnlyList<EdgeSyncConflict>>();
            try
            {
                var conflicts = new List<EdgeSyncConflict>();
                using (var connection = await OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT c.operation_id, o.entity_id, o.entity_type,
 o.result_version_id, o.local_version_id, o.payload_json, c.code, c.message, c.recorded_utc
FROM sync_conflicts c JOIN sync_outbox o ON o.operation_id = c.operation_id
WHERE c.resolved_utc IS NULL ORDER BY o.device_sequence;";
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            conflicts.Add(new EdgeSyncConflict
                            {
                                OperationId = Guid.Parse(reader.GetString(0)),
                                EntityId = Guid.Parse(reader.GetString(1)),
                                EntityType = reader.GetString(2),
                                ServerVersionId = reader.IsDBNull(3) ? Guid.Empty : Guid.Parse(reader.GetString(3)),
                                LocalVersionId = Guid.Parse(reader.GetString(4)),
                                LocalPayloadJson = reader.IsDBNull(5) ? null : reader.GetString(5),
                                Code = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Message = reader.IsDBNull(7) ? null : reader.GetString(7),
                                RecordedUtc = DateTime.Parse(reader.GetString(8), null,
                                    System.Globalization.DateTimeStyles.RoundtripKind)
                            });
                }
                result.Result = conflicts;
                result.IsLoaded = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetError(result, "EDGE_SQLITE_CONFLICT_READ_FAILED", "Unresolved conflicts could not be read.", ex); }
            return result;
        }

        public async Task<OASISResult<SyncOperation>> ResolveConflictAsync(EdgeConflictResolution resolution,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<SyncOperation>();
            try
            {
                if (resolution == null || resolution.OperationId == Guid.Empty)
                    throw new ArgumentException("A conflict operation id is required.", nameof(resolution));
                if (resolution.Kind != EdgeConflictResolutionKind.ServerWins &&
                    resolution.Kind != EdgeConflictResolutionKind.RetryLocal &&
                    resolution.Kind != EdgeConflictResolutionKind.ManualMerge)
                    throw new ArgumentException("The conflict resolution kind is invalid.", nameof(resolution));

                using (var connection = await OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    SyncOperation original;
                    using (var command = CreateCommand(connection, transaction, @"SELECT o.device_id, o.avatar_id,
 o.entity_id, o.entity_type, o.kind, o.result_version_id, o.local_version_id, o.payload_json
FROM sync_outbox o JOIN sync_conflicts c ON c.operation_id=o.operation_id
WHERE o.operation_id=$operation_id AND c.resolved_utc IS NULL;"))
                    {
                        Add(command, "$operation_id", resolution.OperationId.ToString("D"));
                        using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                        {
                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                throw new InvalidOperationException("The unresolved conflict does not exist.");
                            original = new SyncOperation
                            {
                                OperationId = resolution.OperationId,
                                DeviceId = Guid.Parse(reader.GetString(0)), AvatarId = Guid.Parse(reader.GetString(1)),
                                EntityId = Guid.Parse(reader.GetString(2)), EntityType = reader.GetString(3),
                                Kind = (SyncOperationKind)reader.GetInt32(4),
                                BaseVersionId = reader.IsDBNull(5) ? Guid.Empty : Guid.Parse(reader.GetString(5)),
                                VersionId = Guid.Parse(reader.GetString(6)),
                                PayloadJson = reader.IsDBNull(7) ? null : reader.GetString(7)
                            };
                        }
                    }

                    SyncOperation retry = null;
                    if (resolution.Kind == EdgeConflictResolutionKind.ServerWins)
                    {
                        if (original.BaseVersionId == Guid.Empty)
                            throw new InvalidOperationException("The server did not return a version that can win this conflict.");

                        using (var current = CreateCommand(connection, transaction, @"SELECT version_id FROM edge_entities
WHERE entity_type=$entity_type AND entity_id=$entity_id;"))
                        {
                            Add(current, "$entity_type", original.EntityType);
                            Add(current, "$entity_id", original.EntityId.ToString("D"));
                            var localVersion = await current.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                            if (localVersion == null || localVersion == DBNull.Value ||
                                !Guid.TryParse(Convert.ToString(localVersion), out var appliedVersion) ||
                                appliedVersion != original.BaseVersionId)
                                throw new InvalidOperationException("The winning server version has not been pulled to this device yet. Synchronize again before choosing ServerWins.");
                        }
                    }
                    else
                    {
                        if (resolution.ResolutionOperationId == Guid.Empty || resolution.ResolutionVersionId == Guid.Empty)
                            throw new ArgumentException("Retry and manual-merge resolutions require new operation and version ids.", nameof(resolution));
                        string payload = resolution.Kind == EdgeConflictResolutionKind.ManualMerge
                            ? resolution.MergedPayloadJson : original.PayloadJson;
                        var resolvedKind = resolution.Kind == EdgeConflictResolutionKind.ManualMerge
                            ? SyncOperationKind.Upsert : original.Kind;
                        if (resolvedKind != SyncOperationKind.Delete && string.IsNullOrWhiteSpace(payload))
                            throw new ArgumentException("The resolved mutation requires a payload.", nameof(resolution));
                        long sequence = await NextDeviceSequenceAsync(connection, transaction, original.DeviceId, cancellationToken).ConfigureAwait(false);
                        DateTime createdUtc = DateTime.UtcNow;
                        await ApplyEntityAsync(connection, transaction, original.EntityId, original.EntityType,
                            resolvedKind, resolution.ResolutionVersionId, payload, createdUtc, cancellationToken).ConfigureAwait(false);
                        using (var insert = CreateCommand(connection, transaction, @"INSERT INTO sync_outbox(
operation_id, device_id, avatar_id, device_sequence, entity_id, entity_type, kind, base_version_id,
local_version_id, payload_json, created_utc, state) VALUES($operation_id,$device_id,$avatar_id,$sequence,
$entity_id,$entity_type,$kind,$base_version_id,$local_version_id,$payload_json,$created_utc,0);"))
                        {
                            Add(insert, "$operation_id", resolution.ResolutionOperationId.ToString("D"));
                            Add(insert, "$device_id", original.DeviceId.ToString("D")); Add(insert, "$avatar_id", original.AvatarId.ToString("D"));
                            Add(insert, "$sequence", sequence); Add(insert, "$entity_id", original.EntityId.ToString("D"));
                            Add(insert, "$entity_type", original.EntityType); Add(insert, "$kind", (int)resolvedKind);
                            Add(insert, "$base_version_id", original.BaseVersionId.ToString("D"));
                            Add(insert, "$local_version_id", resolution.ResolutionVersionId.ToString("D"));
                            Add(insert, "$payload_json", (object)payload ?? DBNull.Value); Add(insert, "$created_utc", createdUtc.ToString("O"));
                            await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                        }
                        retry = new SyncOperation { OperationId = resolution.ResolutionOperationId, DeviceId = original.DeviceId,
                            AvatarId = original.AvatarId, DeviceSequence = sequence, EntityId = original.EntityId,
                            EntityType = original.EntityType, Kind = resolvedKind, BaseVersionId = original.BaseVersionId,
                            VersionId = resolution.ResolutionVersionId, PayloadJson = payload, CreatedUtc = createdUtc };
                    }

                    using (var update = CreateCommand(connection, transaction, @"UPDATE sync_conflicts SET
resolved_utc=$resolved_utc, resolution_kind=$resolution_kind, resolution_operation_id=$resolution_operation_id
WHERE operation_id=$operation_id AND resolved_utc IS NULL;"))
                    {
                        Add(update, "$resolved_utc", DateTime.UtcNow.ToString("O")); Add(update, "$resolution_kind", (int)resolution.Kind);
                        Add(update, "$resolution_operation_id", retry == null ? DBNull.Value : (object)retry.OperationId.ToString("D"));
                        Add(update, "$operation_id", resolution.OperationId.ToString("D"));
                        if (await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
                            throw new InvalidOperationException("The conflict changed before it could be resolved.");
                    }
                    transaction.Commit();
                    result.Result = retry;
                    result.IsSaved = true;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetError(result, "EDGE_SQLITE_CONFLICT_RESOLUTION_FAILED", "The conflict resolution was not committed.", ex); }
            return result;
        }

        public void Dispose() { }

        private void Initialize()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
PRAGMA journal_mode=WAL;
PRAGMA foreign_keys=ON;
CREATE TABLE IF NOT EXISTS edge_entities(
 entity_type TEXT NOT NULL, entity_id TEXT NOT NULL, version_id TEXT NOT NULL,
 payload_json TEXT NULL, is_deleted INTEGER NOT NULL, changed_utc TEXT NOT NULL,
 PRIMARY KEY(entity_type, entity_id));
CREATE TABLE IF NOT EXISTS sync_device_sequence(device_id TEXT PRIMARY KEY, last_sequence INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS sync_outbox(
 operation_id TEXT PRIMARY KEY, device_id TEXT NOT NULL, avatar_id TEXT NOT NULL,
 device_sequence INTEGER NOT NULL, entity_id TEXT NOT NULL, entity_type TEXT NOT NULL,
 kind INTEGER NOT NULL, base_version_id TEXT NOT NULL, local_version_id TEXT NOT NULL, payload_json TEXT NULL,
 created_utc TEXT NOT NULL, state INTEGER NOT NULL, result_version_id TEXT NULL,
 result_code TEXT NULL, result_message TEXT NULL, completed_utc TEXT NULL,
 UNIQUE(device_id, device_sequence));
CREATE TABLE IF NOT EXISTS sync_conflicts(
 operation_id TEXT PRIMARY KEY, code TEXT NULL, message TEXT NULL, recorded_utc TEXT NOT NULL,
 resolved_utc TEXT NULL, resolution_kind INTEGER NULL, resolution_operation_id TEXT NULL,
 FOREIGN KEY(operation_id) REFERENCES sync_outbox(operation_id));
CREATE TABLE IF NOT EXISTS sync_inbox(change_id TEXT PRIMARY KEY, applied_utc TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS replication_inbox(operation_id TEXT PRIMARY KEY, applied_utc TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS sync_state(singleton_id INTEGER PRIMARY KEY CHECK(singleton_id = 1),
 pull_checkpoint TEXT NULL, last_successful_sync_utc TEXT NULL,
 snapshot_id TEXT NULL, snapshot_page_index INTEGER NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS sync_snapshot_entities(
 snapshot_id TEXT NOT NULL, entity_type TEXT NOT NULL, entity_id TEXT NOT NULL,
 version_id TEXT NOT NULL, payload_json TEXT NULL, is_deleted INTEGER NOT NULL, changed_utc TEXT NOT NULL,
 PRIMARY KEY(snapshot_id, entity_type, entity_id));
INSERT OR IGNORE INTO sync_state(singleton_id) VALUES(1);";
                    command.ExecuteNonQuery();
                }
                EnsureOutboxVersionColumn(connection);
                EnsureConflictResolutionColumns(connection);
                EnsureSnapshotStateColumns(connection);
            }
        }

        private static void EnsureSnapshotStateColumns(SqliteConnection connection)
        {
            var columns = new HashSet<string>(StringComparer.Ordinal);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(sync_state);";
                using (var reader = command.ExecuteReader()) while (reader.Read()) columns.Add(reader.GetString(1));
            }
            foreach (var definition in new[] { "snapshot_id TEXT NULL", "snapshot_page_index INTEGER NOT NULL DEFAULT 0" })
            {
                var name = definition.Substring(0, definition.IndexOf(' '));
                if (columns.Contains(name)) continue;
                using (var command = connection.CreateCommand())
                { command.CommandText = $"ALTER TABLE sync_state ADD COLUMN {definition};"; command.ExecuteNonQuery(); }
            }
        }

        private static void EnsureConflictResolutionColumns(SqliteConnection connection)
        {
            var columns = new HashSet<string>(StringComparer.Ordinal);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(sync_conflicts);";
                using (var reader = command.ExecuteReader()) while (reader.Read()) columns.Add(reader.GetString(1));
            }
            foreach (var definition in new[] { "resolved_utc TEXT NULL", "resolution_kind INTEGER NULL", "resolution_operation_id TEXT NULL" })
            {
                var name = definition.Substring(0, definition.IndexOf(' '));
                if (columns.Contains(name)) continue;
                using (var command = connection.CreateCommand())
                { command.CommandText = $"ALTER TABLE sync_conflicts ADD COLUMN {definition};"; command.ExecuteNonQuery(); }
            }
        }

        private static void EnsureOutboxVersionColumn(SqliteConnection connection)
        {
            bool exists = false;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(sync_outbox);";
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        if (string.Equals(reader.GetString(1), "local_version_id", StringComparison.Ordinal))
                        { exists = true; break; }
            }
            if (exists) return;

            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                using (var alter = CreateCommand(connection, transaction,
                    "ALTER TABLE sync_outbox ADD COLUMN local_version_id TEXT NULL;"))
                    alter.ExecuteNonQuery();
                using (var pending = CreateCommand(connection, transaction,
                    "SELECT COUNT(*) FROM sync_outbox WHERE state = 0 AND local_version_id IS NULL;"))
                {
                    if (Convert.ToInt64(pending.ExecuteScalar()) != 0)
                        throw new InvalidOperationException(
                            "The Edge sync database contains pending pre-version-protocol operations whose immutable local versions cannot be reconstructed. Export or resolve that outbox before upgrading.");
                }
                using (var backfill = CreateCommand(connection, transaction,
                    "UPDATE sync_outbox SET local_version_id = COALESCE(result_version_id, operation_id) WHERE local_version_id IS NULL;"))
                    backfill.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
        {
            var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            return connection;
        }

        private static async Task<long> NextDeviceSequenceAsync(SqliteConnection connection, SqliteTransaction transaction,
            Guid deviceId, CancellationToken cancellationToken)
        {
            using (var insert = CreateCommand(connection, transaction,
                "INSERT OR IGNORE INTO sync_device_sequence(device_id, last_sequence) VALUES($device_id, 0);"))
            {
                Add(insert, "$device_id", deviceId.ToString("D"));
                await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            using (var update = CreateCommand(connection, transaction,
                "UPDATE sync_device_sequence SET last_sequence = last_sequence + 1 WHERE device_id = $device_id RETURNING last_sequence;"))
            {
                Add(update, "$device_id", deviceId.ToString("D"));
                return Convert.ToInt64(await update.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
            }
        }

        private static async Task<SyncOperation> LoadOperationAsync(SqliteConnection connection,
            SqliteTransaction transaction, Guid operationId, CancellationToken cancellationToken)
        {
            using (var command = CreateCommand(connection, transaction, @"
SELECT operation_id, device_id, avatar_id, device_sequence, entity_id, entity_type, kind,
 base_version_id, local_version_id, payload_json, created_utc
FROM sync_outbox WHERE operation_id=$operation_id;"))
            {
                Add(command, "$operation_id", operationId.ToString("D"));
                using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
                    return new SyncOperation
                    {
                        OperationId = Guid.Parse(reader.GetString(0)), DeviceId = Guid.Parse(reader.GetString(1)),
                        AvatarId = Guid.Parse(reader.GetString(2)), DeviceSequence = reader.GetInt64(3),
                        EntityId = Guid.Parse(reader.GetString(4)), EntityType = reader.GetString(5),
                        Kind = (SyncOperationKind)reader.GetInt32(6), BaseVersionId = Guid.Parse(reader.GetString(7)),
                        VersionId = Guid.Parse(reader.GetString(8)),
                        PayloadJson = reader.IsDBNull(9) ? null : reader.GetString(9),
                        CreatedUtc = DateTime.Parse(reader.GetString(10), null,
                            System.Globalization.DateTimeStyles.RoundtripKind)
                    };
                }
            }
        }

        private static bool Matches(SyncOperation existing, EdgeLocalMutation mutation) =>
            existing.DeviceId == mutation.DeviceId && existing.AvatarId == mutation.AvatarId &&
            existing.EntityId == mutation.EntityId &&
            string.Equals(existing.EntityType, mutation.EntityType, StringComparison.Ordinal) &&
            existing.Kind == mutation.Kind && existing.BaseVersionId == mutation.BaseVersionId &&
            existing.VersionId == mutation.LocalVersionId &&
            string.Equals(existing.PayloadJson, mutation.PayloadJson, StringComparison.Ordinal) &&
            (mutation.CreatedUtc == default || existing.CreatedUtc == mutation.CreatedUtc);

        private static async Task ApplyEntityAsync(SqliteConnection connection, SqliteTransaction transaction, Guid entityId,
            string entityType, SyncOperationKind kind, Guid versionId, string payloadJson, DateTime changedUtc,
            CancellationToken cancellationToken)
        {
            using (var command = CreateCommand(connection, transaction, @"
INSERT INTO edge_entities(entity_type, entity_id, version_id, payload_json, is_deleted, changed_utc)
VALUES($entity_type, $entity_id, $version_id, $payload_json, $is_deleted, $changed_utc)
ON CONFLICT(entity_type, entity_id) DO UPDATE SET version_id=excluded.version_id,
 payload_json=excluded.payload_json, is_deleted=excluded.is_deleted, changed_utc=excluded.changed_utc;"))
            {
                Add(command, "$entity_type", entityType);
                Add(command, "$entity_id", entityId.ToString("D"));
                Add(command, "$version_id", versionId.ToString("D"));
                Add(command, "$payload_json", kind == SyncOperationKind.Delete ? DBNull.Value : (object)payloadJson ?? DBNull.Value);
                Add(command, "$is_deleted", kind == SyncOperationKind.Delete ? 1 : 0);
                Add(command, "$changed_utc", changedUtc.ToString("O"));
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task CommitOperationResultAsync(SqliteConnection connection, SqliteTransaction transaction,
            SyncOperationResult operationResult, DateTime committedUtc, CancellationToken cancellationToken)
        {
            int state = operationResult.Disposition == SyncOperationDisposition.Accepted || operationResult.Disposition == SyncOperationDisposition.AlreadyApplied ? 1 :
                operationResult.Disposition == SyncOperationDisposition.Conflict ? 2 : 3;
            using (var command = CreateCommand(connection, transaction, @"
UPDATE sync_outbox SET state=$state, result_version_id=$version_id, result_code=$code,
 result_message=$message, completed_utc=$completed_utc WHERE operation_id=$operation_id;"))
            {
                Add(command, "$state", state);
                Add(command, "$version_id", operationResult.ResultVersionId.ToString("D"));
                Add(command, "$code", (object)operationResult.Code ?? DBNull.Value);
                Add(command, "$message", (object)operationResult.Message ?? DBNull.Value);
                Add(command, "$completed_utc", committedUtc.ToString("O"));
                Add(command, "$operation_id", operationResult.OperationId.ToString("D"));
                if (await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
                    throw new InvalidOperationException("The server acknowledged an operation that is not in the local outbox.");
            }
            if (operationResult.Disposition == SyncOperationDisposition.Conflict)
            {
                using (var command = CreateCommand(connection, transaction, @"
INSERT INTO sync_conflicts(operation_id, code, message, recorded_utc)
VALUES($operation_id, $code, $message, $recorded_utc)
ON CONFLICT(operation_id) DO UPDATE SET code=excluded.code, message=excluded.message, recorded_utc=excluded.recorded_utc;"))
                {
                    Add(command, "$operation_id", operationResult.OperationId.ToString("D"));
                    Add(command, "$code", (object)operationResult.Code ?? DBNull.Value);
                    Add(command, "$message", (object)operationResult.Message ?? DBNull.Value);
                    Add(command, "$recorded_utc", committedUtc.ToString("O"));
                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static async Task<bool> WasRemoteChangeAppliedAsync(SqliteConnection connection, SqliteTransaction transaction,
            string changeId, CancellationToken cancellationToken)
        {
            using (var command = CreateCommand(connection, transaction,
                "SELECT EXISTS(SELECT 1 FROM sync_inbox WHERE change_id=$change_id);"))
            {
                Add(command, "$change_id", changeId);
                return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) == 1;
            }
        }

        private static async Task EnsureRemoteChangeCanAdvanceEntityAsync(SqliteConnection connection,
            SqliteTransaction transaction, SyncRemoteChange change, CancellationToken cancellationToken)
        {
            if (change == null || string.IsNullOrWhiteSpace(change.ChangeId) || change.EntityId == Guid.Empty ||
                change.VersionId == Guid.Empty || string.IsNullOrWhiteSpace(change.EntityType) ||
                (change.Kind != SyncOperationKind.Upsert && change.Kind != SyncOperationKind.Delete) ||
                (change.Kind == SyncOperationKind.Upsert && string.IsNullOrWhiteSpace(change.PayloadJson)))
                throw new InvalidOperationException("The hosted service returned an invalid remote entity change.");

            Guid currentVersionId = Guid.Empty;
            using (var current = CreateCommand(connection, transaction, @"SELECT version_id FROM edge_entities
WHERE entity_type=$entity_type AND entity_id=$entity_id;"))
            {
                Add(current, "$entity_type", change.EntityType);
                Add(current, "$entity_id", change.EntityId.ToString("D"));
                var value = await current.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (value != null && value != DBNull.Value)
                    currentVersionId = Guid.Parse(Convert.ToString(value));
            }

            if (currentVersionId == change.VersionId || currentVersionId == change.PreviousVersionId)
                return;

            // A conflict response explicitly authorizes the corresponding server version to be materialized
            // while preserving the local operation in sync_conflicts for user-selected resolution.
            using (var conflict = CreateCommand(connection, transaction, @"SELECT EXISTS(
SELECT 1 FROM sync_outbox
WHERE entity_type=$entity_type AND entity_id=$entity_id AND state=2 AND result_version_id=$version_id);"))
            {
                Add(conflict, "$entity_type", change.EntityType);
                Add(conflict, "$entity_id", change.EntityId.ToString("D"));
                Add(conflict, "$version_id", change.VersionId.ToString("D"));
                if (Convert.ToInt32(await conflict.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) == 1)
                    return;
            }

            throw new RemoteVersionConflictException(
                $"Remote change '{change.ChangeId}' cannot advance {change.EntityType}/{change.EntityId:D} from " +
                $"version '{currentVersionId:D}' because it requires previous version '{change.PreviousVersionId:D}'.");
        }

        private sealed class RemoteVersionConflictException : InvalidOperationException
        {
            public RemoteVersionConflictException(string message) : base(message) { }
        }

        private static SqliteCommand CreateCommand(SqliteConnection connection, SqliteTransaction transaction, string text)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = text;
            return command;
        }

        private static void Add(SqliteCommand command, string name, object value) => command.Parameters.AddWithValue(name, value);

        private static void ValidateMutation(EdgeLocalMutation mutation)
        {
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));
            if (mutation.OperationId == Guid.Empty || mutation.DeviceId == Guid.Empty || mutation.AvatarId == Guid.Empty || mutation.EntityId == Guid.Empty)
                throw new ArgumentException("Operation, device, avatar and entity identifiers are required.", nameof(mutation));
            if (mutation.LocalVersionId == Guid.Empty) throw new ArgumentException("A local version identifier is required.", nameof(mutation));
            if (string.IsNullOrWhiteSpace(mutation.EntityType)) throw new ArgumentException("An entity type is required.", nameof(mutation));
            if (mutation.Kind != SyncOperationKind.Delete && string.IsNullOrWhiteSpace(mutation.PayloadJson))
                throw new ArgumentException("A payload is required for non-delete mutations.", nameof(mutation));
        }

        private static void SetError<T>(OASISResult<T> result, string code, string message, Exception exception)
        {
            result.IsError = true;
            result.ErrorCount = 1;
            result.ErrorCode = code;
            result.Message = message;
            result.Exception = exception;
        }
    }
}
