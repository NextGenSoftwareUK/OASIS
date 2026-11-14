using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS
{
    private sealed partial class OracleOutboundOutbox(OracleDBOASIS provider, CosmicStorageIdentity expectedSource)
    {
        private const string DeliveryColumns =
            "DELIVERY_SEQUENCE, OPERATION_ID, TARGET_ID, PAYLOAD_JSON, PAYLOAD_DIGEST, DELIVERY_STATE, ATTEMPT_COUNT, "
            + "WORKER_ID, LEASE_TOKEN, LEASE_UNTIL, RETRY_AT, ERROR_CODE, ERROR_MESSAGE, RECOVERY_JSON, RECOVERY_DIGEST";

        private sealed record DeliveryRow(HolonDeliveryInspection Inspection, string Payload, string? Worker,
            Guid? Lease, long LeaseUntil, long RetryAt);

        private async Task<T> ReadBoundAsync<T>(
            Func<OracleConnection, OracleTransaction, Task<T>> read, CancellationToken cancellationToken)
        {
            if (!provider.IsProviderActivated || expectedSource.Authority != ReplicaAuthority)
                throw new InvalidOperationException("Activate the enrolled Oracle schema before opening the outbox.");

            await using var connection = new OracleConnection(provider._connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
            var identity = await ReadOracleStorageIdentityAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
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
                // Compare UTF-8 bytes to match the shared route cursor, independently of Oracle NLS settings.
                var continuation = afterTargetId == null
                    ? string.Empty
                    : "AND UTL_I18N.STRING_TO_RAW(TARGET_ID, 'AL32UTF8') > UTL_I18N.STRING_TO_RAW(:AfterTarget, 'AL32UTF8')";
                await using var command = CreateCommand($@"
SELECT {DeliveryColumns}
FROM (
    SELECT {DeliveryColumns}, ROW_NUMBER() OVER (
        PARTITION BY UTL_I18N.STRING_TO_RAW(TARGET_ID, 'AL32UTF8') ORDER BY DELIVERY_SEQUENCE) AS ROUTE_POSITION
    FROM OASIS_OUTBOUND_DELIVERIES
    WHERE DELIVERY_STATE <> 'Completed' {continuation}
)
WHERE ROUTE_POSITION = 1
ORDER BY UTL_I18N.STRING_TO_RAW(TARGET_ID, 'AL32UTF8')
FETCH FIRST :PageLimit ROWS ONLY", connection);
                command.Transaction = transaction;
                if (afterTargetId != null)
                    command.Parameters.Add("AfterTarget", OracleDbType.Varchar2).Value = afterTargetId;
                command.Parameters.Add("PageLimit", OracleDbType.Int32).Value = pageSize + 1;

                var heads = new List<HolonDeliveryInspection>(pageSize + 1);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var head = ReadDelivery(reader).Inspection;
                    if (afterTargetId != null && HolonReplicationRoutePage.CompareTargetIds(head.TargetId, afterTargetId) <= 0)
                        throw new InvalidDataException("Oracle returned outbound routes outside binary target order.");
                    heads.Add(head);
                    afterTargetId = head.TargetId;
                }
                return new HolonReplicationRoutePage(heads.Take(pageSize), heads.Count > pageSize);
            }, cancellationToken);
        }

        private async Task<DeliveryRow?> ReadRowAsync(OracleConnection connection, OracleTransaction transaction,
            string? targetId, long? sequence, CancellationToken token)
        {
            var predicate = targetId == null
                ? "DELIVERY_SEQUENCE=:Sequence"
                : "UTL_I18N.STRING_TO_RAW(TARGET_ID, 'AL32UTF8')=UTL_I18N.STRING_TO_RAW(:Target, 'AL32UTF8') AND DELIVERY_STATE<>'Completed'";
            await using var command = CreateCommand($@"SELECT {DeliveryColumns}
FROM OASIS_OUTBOUND_DELIVERIES
WHERE {predicate}
ORDER BY DELIVERY_SEQUENCE FETCH FIRST 1 ROW ONLY", connection);
            command.Transaction = transaction;
            if (targetId == null)
                command.Parameters.Add("Sequence", OracleDbType.Int64).Value = sequence!.Value;
            else
                command.Parameters.Add("Target", OracleDbType.Varchar2).Value = targetId;
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            return await reader.ReadAsync(token).ConfigureAwait(false) ? ReadDelivery(reader) : null;
        }

        private DeliveryRow ReadDelivery(OracleDataReader reader)
        {
            var sequence = reader.GetInt64(0);
            var operation = reader.GetValue(1) as byte[];
            var targetId = reader.GetString(2);
            var payload = reader.GetString(3);
            var stateText = reader.GetString(5);
            var attempts = reader.GetInt32(6);
            if (sequence <= 0 || operation?.Length != 16 || new Guid(operation) == Guid.Empty || attempts < 0
                || !Enum.TryParse<HolonDeliveryState>(stateText, out var state) || !Enum.IsDefined(state)
                || state.ToString() != stateText)
                throw new InvalidDataException("Oracle returned invalid outbound delivery metadata.");
            if (HolonRecoveryEvidenceJson.Digest(payload) != reader.GetString(4))
                throw new InvalidDataException("The Oracle outbound payload checksum differs.");

            var operationId = new Guid(operation);
            var request = CommittedHolonReplicaPayload.ReadForTarget(payload, targetId);
            var route = request.NativeRoutes.Single(binding => binding.TargetId == targetId);
            if (request.OperationId != operationId || request.SourceAuthority != ReplicaAuthority
                || route.SourceStorage.BindingFingerprint != expectedSource.BindingFingerprint)
                throw new InvalidDataException("The Oracle outbound payload differs from the enrolled route.");

            string? ReadOptional(int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
            var worker = ReadOptional(7);
            var rawLease = reader.IsDBNull(8) ? null : reader.GetValue(8) as byte[];
            var leaseUntil = reader.IsDBNull(9) ? 0 : reader.GetInt64(9);
            var retryAt = reader.IsDBNull(10) ? 0 : reader.GetInt64(10);
            Guid? lease = rawLease?.Length == 16 ? new Guid(rawLease) : null;
            if (state == HolonDeliveryState.InFlight
                ? string.IsNullOrWhiteSpace(worker) || lease == null || lease == Guid.Empty || leaseUntil <= 0 || retryAt != 0
                : worker != null || rawLease != null || leaseUntil != 0 || retryAt < 0
                    || (state != HolonDeliveryState.Pending && retryAt != 0))
                throw new InvalidDataException("Oracle returned invalid outbound lease metadata.");

            var inspection = new HolonDeliveryInspection(sequence, operationId, targetId, state, attempts,
                ReadOptional(11), ReadOptional(12), HolonRecoveryEvidenceJson.ReadStored(
                    ReadOptional(13), ReadOptional(14), state), route);
            return new DeliveryRow(inspection, payload, worker, lease, leaseUntil, retryAt);
        }
    }
}
