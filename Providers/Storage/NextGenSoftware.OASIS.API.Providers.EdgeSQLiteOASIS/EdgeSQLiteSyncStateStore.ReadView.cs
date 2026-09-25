using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS
{
    public sealed class EdgeReadView
    {
        public IReadOnlyList<EdgeStoredEntity> Entities { get; set; }
        public IReadOnlyList<SyncOperation> Commands { get; set; }
    }

    public sealed partial class EdgeSQLiteSyncStateStore
    {
        /// <summary>Includes accepted commands until their durable hosted outcome has arrived.</summary>
        public async Task<OASISResult<long>> GetUnsettledOperationCountAsync(CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<long>();
            try
            {
                using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
                using var command = connection.CreateCommand();
                command.CommandText = @"SELECT COUNT(*) FROM sync_outbox o WHERE o.state=0 OR
                    (o.state=1 AND o.kind=$kind AND NOT EXISTS
                    (SELECT 1 FROM edge_entities e WHERE e.entity_type=$type AND e.entity_id=o.operation_id AND e.is_deleted=0));";
                Add(command, "$kind", (int)SyncOperationKind.Command);
                Add(command, "$type", HyperDriveEntityTypes.CommandResult);
                result.Result = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
                result.IsLoaded = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetError(result, "EDGE_SQLITE_UNSETTLED_COUNT_FAILED", "Pending hosted outcomes could not be counted.", ex); }
            return result;
        }

        /// <summary>
        /// One read transaction for projections and durable intents. Reading these independently can
        /// lose or double-display an action when synchronization acknowledges it between the reads.
        /// Accepted commands remain visible until the corresponding domain projection carries its receipt.
        /// </summary>
        public async Task<OASISResult<EdgeReadView>> ReadViewAsync(CancellationToken cancellationToken = default)
        {
            var result = new OASISResult<EdgeReadView>();
            try
            {
                using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
                using var transaction = connection.BeginTransaction(deferred: true);
                var entities = new List<EdgeStoredEntity>();
                using (var command = CreateCommand(connection, transaction,
                    "SELECT entity_id, entity_type, version_id, payload_json, is_deleted, changed_utc FROM edge_entities;"))
                using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        entities.Add(new EdgeStoredEntity
                        {
                            EntityId = Guid.Parse(reader.GetString(0)), EntityType = reader.GetString(1),
                            VersionId = Guid.Parse(reader.GetString(2)), PayloadJson = reader.IsDBNull(3) ? null : reader.GetString(3),
                            IsDeleted = reader.GetInt32(4) != 0,
                            ChangedUtc = DateTime.Parse(reader.GetString(5), null, DateTimeStyles.RoundtripKind)
                        });
                }
                var commands = new List<SyncOperation>();
                using (var command = CreateCommand(connection, transaction, @"
SELECT operation_id, device_id, avatar_id, device_sequence, entity_id, entity_type,
base_version_id, local_version_id, payload_json, created_utc
FROM sync_outbox WHERE state IN (0,1) AND kind=$kind ORDER BY device_sequence;"))
                {
                    Add(command, "$kind", (int)SyncOperationKind.Command);
                    using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        commands.Add(new SyncOperation
                        {
                            OperationId = Guid.Parse(reader.GetString(0)), DeviceId = Guid.Parse(reader.GetString(1)),
                            AvatarId = Guid.Parse(reader.GetString(2)), DeviceSequence = reader.GetInt64(3),
                            EntityId = Guid.Parse(reader.GetString(4)), EntityType = reader.GetString(5),
                            Kind = SyncOperationKind.Command, BaseVersionId = Guid.Parse(reader.GetString(6)),
                            VersionId = Guid.Parse(reader.GetString(7)), PayloadJson = reader.GetString(8),
                            CreatedUtc = DateTime.Parse(reader.GetString(9), null, DateTimeStyles.RoundtripKind)
                        });
                }
                transaction.Commit();
                result.Result = new EdgeReadView { Entities = entities, Commands = commands };
                result.IsLoaded = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetError(result, "EDGE_SQLITE_READ_VIEW_FAILED", "The consistent Edge read view could not be loaded.", ex); }
            return result;
        }
    }
}
