using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS
{
    public sealed partial class EdgeSQLiteSyncStateStore
    {
        public async Task<OASISResult<IReadOnlyList<SyncOperation>>> ReadPendingLocalReplicationsAsync(
            string targetId, int maximumCount, CancellationToken cancellationToken)
        {
            var result = new OASISResult<IReadOnlyList<SyncOperation>>();
            try
            {
                ValidateTarget(targetId);
                if (maximumCount <= 0) throw new ArgumentOutOfRangeException(nameof(maximumCount));
                var operations = new List<SyncOperation>();
                using (var connection = await OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT o.operation_id,o.device_id,o.avatar_id,o.device_sequence,o.entity_id,o.entity_type,
 o.kind,o.base_version_id,o.local_version_id,o.payload_json,o.created_utc
FROM local_replication_outbox r
JOIN sync_outbox o ON o.operation_id=r.operation_id
WHERE r.target_id=$target_id AND r.state=0
ORDER BY o.device_sequence LIMIT $maximum_count;";
                    Add(command, "$target_id", targetId);
                    Add(command, "$maximum_count", maximumCount);
                    using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            operations.Add(ReadSyncOperation(reader));
                }
                result.Result = operations;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetError(result, "EDGE_LOCAL_REPLICATION_READ_FAILED", "Pending local replications could not be read.", ex); }
            return result;
        }

        public async Task<OASISResult<bool>> CompleteLocalReplicationAsync(string targetId,
            Guid operationId, CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            try
            {
                ValidateTarget(targetId);
                if (operationId == Guid.Empty) throw new ArgumentException("An operation id is required.", nameof(operationId));
                using (var connection = await OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
UPDATE local_replication_outbox SET state=1, completed_utc=$completed_utc,
 last_error_code=NULL,last_error_message=NULL
WHERE target_id=$target_id AND operation_id=$operation_id AND state=0;";
                    Add(command, "$completed_utc", DateTime.UtcNow.ToString("O"));
                    Add(command, "$target_id", targetId);
                    Add(command, "$operation_id", operationId.ToString("D"));
                    if (await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
                        throw new InvalidOperationException("The pending local replication operation does not exist or is already complete.");
                }
                result.Result = true;
                result.IsSaved = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetError(result, "EDGE_LOCAL_REPLICATION_COMPLETE_FAILED", "The local replication acknowledgement was not committed.", ex); }
            return result;
        }

        public async Task<OASISResult<bool>> RecordLocalReplicationFailureAsync(string targetId,
            Guid operationId, string errorCode, string errorMessage, CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            try
            {
                ValidateTarget(targetId);
                if (operationId == Guid.Empty) throw new ArgumentException("An operation id is required.", nameof(operationId));
                using (var connection = await OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
UPDATE local_replication_outbox SET attempt_count=attempt_count+1,
 last_error_code=$error_code,last_error_message=$error_message,last_attempt_utc=$attempted_utc
WHERE target_id=$target_id AND operation_id=$operation_id AND state=0;";
                    Add(command, "$error_code", (object)errorCode ?? DBNull.Value);
                    Add(command, "$error_message", (object)errorMessage ?? DBNull.Value);
                    Add(command, "$attempted_utc", DateTime.UtcNow.ToString("O"));
                    Add(command, "$target_id", targetId);
                    Add(command, "$operation_id", operationId.ToString("D"));
                    if (await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) != 1)
                        throw new InvalidOperationException("The pending local replication operation does not exist or is already complete.");
                }
                result.Result = true;
                result.IsSaved = true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetError(result, "EDGE_LOCAL_REPLICATION_FAILURE_RECORD_FAILED", "The local replication failure was not recorded.", ex); }
            return result;
        }

        public async Task<OASISResult<long>> GetPendingLocalReplicationCountAsync(string targetId,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<long>();
            try
            {
                ValidateTarget(targetId);
                using (var connection = await OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM local_replication_outbox WHERE target_id=$target_id AND state=0;";
                    Add(command, "$target_id", targetId);
                    result.Result = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { SetError(result, "EDGE_LOCAL_REPLICATION_COUNT_FAILED", "Pending local replications could not be counted.", ex); }
            return result;
        }

        private static SyncOperation ReadSyncOperation(SqliteDataReader reader) => new SyncOperation
        {
            OperationId = Guid.Parse(reader.GetString(0)), DeviceId = Guid.Parse(reader.GetString(1)),
            AvatarId = Guid.Parse(reader.GetString(2)), DeviceSequence = reader.GetInt64(3),
            EntityId = Guid.Parse(reader.GetString(4)), EntityType = reader.GetString(5),
            Kind = (SyncOperationKind)reader.GetInt32(6), BaseVersionId = Guid.Parse(reader.GetString(7)),
            VersionId = Guid.Parse(reader.GetString(8)), PayloadJson = reader.IsDBNull(9) ? null : reader.GetString(9),
            CreatedUtc = DateTime.Parse(reader.GetString(10), null, System.Globalization.DateTimeStyles.RoundtripKind)
        };

        private static void ValidateTarget(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) throw new ArgumentException("A local replication target id is required.", nameof(targetId));
        }
    }
}
