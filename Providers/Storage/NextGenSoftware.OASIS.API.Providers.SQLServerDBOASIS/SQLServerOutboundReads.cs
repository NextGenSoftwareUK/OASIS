using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Microsoft.Data.SqlClient;

namespace NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS;

public partial class SQLServerDBOASIS
{
    private sealed partial class SqlServerOutboundOutbox(SQLServerDBOASIS provider, CosmicStorageIdentity expectedSource)
    {
        private const string DeliveryColumns =
            "DeliverySequence, OperationId, TargetId, PayloadJson, PayloadDigest, DeliveryState, AttemptCount, "
            + "WorkerId, LeaseToken, LeaseUntil, RetryAt, ErrorCode, ErrorMessage, RecoveryJson, RecoveryDigest";

        private sealed record DeliveryRow(HolonDeliveryInspection Inspection, string Payload, string? Worker,
            Guid? Lease, long LeaseUntil, long RetryAt);

        private async Task<T> ReadBoundAsync<T>(
            Func<SqlConnection, SqlTransaction, Task<T>> read, CancellationToken cancellationToken)
        {
            if (!provider.IsProviderActivated || expectedSource.Authority != ReplicaAuthority)
                throw new InvalidOperationException("Activate the enrolled SQL Server database before opening the outbox.");

            await using var connection = new SqlConnection(provider._connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
            var identity = await ReadSqlServerStorageIdentityAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            if (identity == null || identity.BindingFingerprint != expectedSource.BindingFingerprint)
                throw new SourceBindingException();

            return await read(connection, transaction).ConfigureAwait(false);
        }

        private Task<HolonReplicationRoutePage> ReadRoutePageAsync(int pageSize, string? afterTargetId,
            CancellationToken cancellationToken)
        {
            HolonReplicationRoutePage.ValidateRequest(pageSize, afterTargetId);
            return ReadBoundAsync(async (connection, transaction) =>
            {
                // Compare UTF-8 bytes to match the shared route cursor, independently of database collation.
                var continuation = afterTargetId == null
                    ? string.Empty
                    : "AND TargetKey > @AfterTarget";
                await using var command = new SqlCommand($@"
SELECT TOP (@PageLimit) {DeliveryColumns}
FROM (
    SELECT {DeliveryColumns}, TargetKey, ROW_NUMBER() OVER (
        PARTITION BY TargetKey ORDER BY DeliverySequence) AS ROUTE_POSITION
    FROM dbo.OASISOutboundDeliveries
    WHERE DeliveryState <> 'Completed' {continuation}
) AS ranked
WHERE ROUTE_POSITION = 1
ORDER BY TargetKey
", connection, transaction);
                if (afterTargetId != null)
                    command.Parameters.Add("@AfterTarget", System.Data.SqlDbType.VarBinary, 1024).Value = OutboundTargetKey(afterTargetId);
                command.Parameters.Add("@PageLimit", System.Data.SqlDbType.Int).Value = pageSize + 1;

                var heads = new List<HolonDeliveryInspection>(pageSize + 1);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var head = ReadDelivery(reader).Inspection;
                    if (afterTargetId != null && HolonReplicationRoutePage.CompareTargetIds(head.TargetId, afterTargetId) <= 0)
                        throw new InvalidDataException("SQL Server returned outbound routes outside binary target order.");
                    heads.Add(head);
                    afterTargetId = head.TargetId;
                }
                return new HolonReplicationRoutePage(heads.Take(pageSize), heads.Count > pageSize);
            }, cancellationToken);
        }

        private async Task<DeliveryRow?> ReadRowAsync(SqlConnection connection, SqlTransaction transaction,
            string? targetId, long? sequence, CancellationToken token)
        {
            var predicate = targetId == null
                ? "DeliverySequence=@Sequence"
                : "TargetKey=@Target AND DeliveryState<>'Completed'";
            await using var command = new SqlCommand($@"SELECT TOP (1) {DeliveryColumns}
FROM dbo.OASISOutboundDeliveries
WHERE {predicate}
ORDER BY DeliverySequence", connection, transaction);
            if (targetId == null)
                command.Parameters.Add("@Sequence", System.Data.SqlDbType.BigInt).Value = sequence!.Value;
            else
                command.Parameters.Add("@Target", System.Data.SqlDbType.VarBinary, 1024).Value = OutboundTargetKey(targetId);
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            return await reader.ReadAsync(token).ConfigureAwait(false) ? ReadDelivery(reader) : null;
        }

        private DeliveryRow ReadDelivery(DbDataReader reader)
        {
            var sequence = reader.GetInt64(0);
            var operation = reader.GetGuid(1);
            var targetId = reader.GetString(2);
            var payload = reader.GetString(3);
            var stateText = reader.GetString(5);
            var attempts = reader.GetInt32(6);
            if (sequence <= 0 || operation == Guid.Empty || attempts < 0
                || !Enum.TryParse<HolonDeliveryState>(stateText, out var state) || !Enum.IsDefined(state)
                || state.ToString() != stateText)
                throw new InvalidDataException("SQL Server returned invalid outbound delivery metadata.");
            if (HolonRecoveryEvidenceJson.Digest(payload) != reader.GetString(4))
                throw new InvalidDataException("The SQL Server outbound payload checksum differs.");

            var operationId = operation;
            var request = CommittedHolonReplicaPayload.ReadForTarget(payload, targetId);
            var route = request.NativeRoutes.Single(binding => binding.TargetId == targetId);
            if (request.OperationId != operationId || request.SourceAuthority != ReplicaAuthority
                || route.SourceStorage.BindingFingerprint != expectedSource.BindingFingerprint)
                throw new InvalidDataException("The SQL Server outbound payload differs from the enrolled route.");

            string? ReadOptional(int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
            var worker = ReadOptional(7);
            Guid? lease = reader.IsDBNull(8) ? null : reader.GetGuid(8);
            var leaseUntil = reader.IsDBNull(9) ? 0 : reader.GetInt64(9);
            var retryAt = reader.IsDBNull(10) ? 0 : reader.GetInt64(10);
            if (state == HolonDeliveryState.InFlight
                ? string.IsNullOrWhiteSpace(worker) || lease == null || lease == Guid.Empty || leaseUntil <= 0 || retryAt != 0
                : worker != null || lease != null || leaseUntil != 0 || retryAt < 0
                    || (state != HolonDeliveryState.Pending && retryAt != 0))
                throw new InvalidDataException("SQL Server returned invalid outbound lease metadata.");

            var inspection = new HolonDeliveryInspection(sequence, operationId, targetId, state, attempts,
                ReadOptional(11), ReadOptional(12), HolonRecoveryEvidenceJson.ReadStored(
                    ReadOptional(13), ReadOptional(14), state), route);
            return new DeliveryRow(inspection, payload, worker, lease, leaseUntil, retryAt);
        }
    }

    private static byte[] OutboundTargetKey(string targetId) => new UTF8Encoding(false, true).GetBytes(targetId);
}
