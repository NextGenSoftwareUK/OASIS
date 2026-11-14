using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS : IBoundHolonReplicationOutboxSource, IBoundAtomicHolonReplicationStorageProvider
{
    public OASISResult<IEnumerable<IHolon>> ExecuteReplicatingHolonWrite(
        ConditionalHolonWriteRequest request, HolonReplicationEnrollment enrollment)
        => ExecuteReplicatingHolonWriteAsync(request, enrollment).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ExecuteReplicatingHolonWriteAsync(
        ConditionalHolonWriteRequest request, HolonReplicationEnrollment enrollment,
        CancellationToken cancellationToken = default)
        => ExecuteOracleConditionalWriteAsync(request, enrollment ?? throw new ArgumentNullException(nameof(enrollment)), cancellationToken);

    public OASISResult<IHolonReplicationRouteInventoryOutbox> GetBoundHolonReplicationOutbox(CosmicStorageIdentity expectedSource)
    {
        ArgumentNullException.ThrowIfNull(expectedSource);
        return IsProviderActivated && expectedSource.Authority == ReplicaAuthority
            ? new(new OracleOutboundOutbox(this, expectedSource))
            : new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_OUTBOX_SOURCE_UNAVAILABLE",
                Message = "An active Oracle provider and an exact Oracle source binding are required." };
    }

    public OASISResult<IHolonReplicationReadinessOutbox> GetHolonReplicationOutbox()
    {
        var identity = CaptureStorageIdentityAsync().GetAwaiter().GetResult();
        return identity.IsError
            ? new() { IsError = true, ErrorCount = 1, ErrorCode = identity.ErrorCode,
                Message = identity.Message, Exception = identity.Exception }
            : new(new OracleOutboundOutbox(this, identity.Result));
    }

    private sealed partial class OracleOutboundOutbox : IHolonReplicationRouteInventoryOutbox
    {
        private static async Task<OASISResult<T>> ResultAsync<T>(Func<Task<T>> operation, CancellationToken token)
        {
            try { return new(await operation().ConfigureAwait(false)); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (OracleException cause) when (cause.Number == 1013 && token.IsCancellationRequested)
            {
                throw new OperationCanceledException("Oracle cancelled the outbox operation.", cause, token);
            }
            catch (Exception cause)
            {
                return new() { IsError = true, ErrorCount = 1, Exception = cause, Message = cause.Message,
                    ErrorCode = cause is SourceBindingException ? "COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH"
                        : "COSMIC_ORACLE_OUTBOX_FAILED" };
            }
        }

        private sealed class SourceBindingException() : InvalidOperationException(
            "The physical Oracle schema differs from the enrolled outbox source.") { }

        public Task<OASISResult<HolonReplicationRoutePage>> ReadRouteHeadsAsync(int pageSize = 64,
            string? afterTargetId = null, CancellationToken cancellationToken = default)
            => ResultAsync(() => ReadRoutePageAsync(pageSize, afterTargetId, cancellationToken), cancellationToken);

        public Task<OASISResult<HolonDeliveryInspection>> PeekNextAsync(string targetId, CancellationToken cancellationToken = default)
        {
            ValidateTarget(targetId);
            return ResultAsync(() => ReadBoundAsync(async (connection, transaction) =>
                (await ReadRowAsync(connection, transaction, targetId, null, cancellationToken).ConfigureAwait(false))?.Inspection,
                cancellationToken), cancellationToken);
        }

        public Task<OASISResult<HolonDeliveryInspection>> InspectAsync(long sequence, CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequence);
            return ResultAsync(() => ReadBoundAsync(async (connection, transaction) =>
                (await ReadRowAsync(connection, transaction, null, sequence, cancellationToken).ConfigureAwait(false))?.Inspection,
                cancellationToken), cancellationToken);
        }

        public Task<OASISResult<HolonDeliveryClaim>> ClaimNextAsync(string targetId, string workerId,
            TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            ValidateTarget(targetId);
            ValidateLease(leaseDuration);
            if (string.IsNullOrWhiteSpace(workerId) || workerId.Length > 256)
                throw new ArgumentException("A bounded worker identity is required.", nameof(workerId));

            return MutateAsync<HolonDeliveryClaim>(async (connection, transaction, now) =>
            {
                var row = await ReadRowAsync(connection, transaction, targetId, null, cancellationToken).ConfigureAwait(false);
                if (row == null || row.Inspection.State == HolonDeliveryState.ReconciliationRequired
                    || row.LeaseUntil > now.Ticks || row.RetryAt > now.Ticks)
                    return null;

                var lease = Guid.NewGuid();
                var until = now.Add(leaseDuration);
                var attempt = checked(row.Inspection.AttemptCount + 1);
                await using var command = DeliveryUpdate(connection, transaction, row.Inspection.Sequence,
                    "DELIVERY_STATE='InFlight', WORKER_ID=:Worker, LEASE_TOKEN=:Token, LEASE_UNTIL=:Until, RETRY_AT=NULL, ATTEMPT_COUNT=:Attempt");
                command.Parameters.Add("Worker", OracleDbType.Varchar2).Value = workerId;
                command.Parameters.Add("Token", OracleDbType.Raw).Value = lease.ToByteArray();
                command.Parameters.Add("Until", OracleDbType.Int64).Value = until.Ticks;
                command.Parameters.Add("Attempt", OracleDbType.Int32).Value = attempt;
                await UpdateOneAsync(command, cancellationToken).ConfigureAwait(false);
                return new HolonDeliveryClaim(row.Inspection.Sequence, row.Inspection.OperationId, targetId,
                    workerId, lease, row.Payload, attempt, until);
            }, cancellationToken);
        }

        public Task<OASISResult<bool>> RenewAsync(HolonDeliveryClaim claim, TimeSpan leaseDuration,
            CancellationToken cancellationToken = default)
        {
            ValidateLease(leaseDuration);
            return UpdateOwnedAsync(claim, async (connection, transaction, row, now) =>
            {
                await using var command = DeliveryUpdate(connection, transaction, claim.Sequence, "LEASE_UNTIL=:Until");
                command.Parameters.Add("Until", OracleDbType.Int64).Value = Math.Max(row.LeaseUntil, now.Add(leaseDuration).Ticks);
                await UpdateOneAsync(command, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public Task<OASISResult<bool>> FinishAsync(HolonDeliveryClaim claim, HolonDeliveryOutcome outcome,
            string? errorCode = null, string? message = null, DateTime? nextAttemptUtc = null,
            CancellationToken cancellationToken = default)
            => FinishCoreAsync(claim, outcome, null, errorCode, message, nextAttemptUtc, cancellationToken);

        public Task<OASISResult<bool>> FinishWithRecoveryAsync(HolonDeliveryClaim claim, HolonDeliveryOutcome outcome,
            HolonDeliveryRecoveryEvidence recoveryEvidence, string? errorCode = null, string? message = null,
            DateTime? nextAttemptUtc = null, CancellationToken cancellationToken = default)
            => FinishCoreAsync(claim, outcome, recoveryEvidence ?? throw new ArgumentNullException(nameof(recoveryEvidence)),
                errorCode, message, nextAttemptUtc, cancellationToken);

        private Task<OASISResult<bool>> FinishCoreAsync(HolonDeliveryClaim claim, HolonDeliveryOutcome outcome,
            HolonDeliveryRecoveryEvidence? evidence, string? errorCode, string? message, DateTime? nextAttemptUtc,
            CancellationToken cancellationToken)
        {
            var state = outcome switch
            {
                HolonDeliveryOutcome.Completed => HolonDeliveryState.Completed,
                HolonDeliveryOutcome.Retry => HolonDeliveryState.Pending,
                HolonDeliveryOutcome.ReconciliationRequired => HolonDeliveryState.ReconciliationRequired,
                _ => throw new ArgumentOutOfRangeException(nameof(outcome))
            };
            if ((outcome == HolonDeliveryOutcome.Completed && (errorCode != null || message != null))
                || (outcome != HolonDeliveryOutcome.Completed && (string.IsNullOrWhiteSpace(errorCode)
                    || errorCode.Length > 128 || message?.Length > 4096))
                || (outcome == HolonDeliveryOutcome.Retry ? nextAttemptUtc?.Kind != DateTimeKind.Utc : nextAttemptUtc != null)
                || (evidence?.RequiresNativeReconciliation == true && state != HolonDeliveryState.ReconciliationRequired))
                throw new ArgumentException("Delivery diagnostics and retry time must match the outcome.");

            var recovery = evidence == null ? null : HolonRecoveryEvidenceJson.Write(evidence);
            return UpdateOwnedAsync(claim, async (connection, transaction, _, now) =>
            {
                if (nextAttemptUtc <= now)
                    throw new ArgumentException("The next delivery attempt must be in the future.", nameof(nextAttemptUtc));
                await using var command = DeliveryUpdate(connection, transaction, claim.Sequence,
                    "DELIVERY_STATE=:State, WORKER_ID=NULL, LEASE_TOKEN=NULL, LEASE_UNTIL=NULL, RETRY_AT=:RetryAt, "
                    + "ERROR_CODE=:ErrorCode, ERROR_MESSAGE=:ErrorMessage, RECOVERY_JSON=:Recovery, RECOVERY_DIGEST=:Digest");
                command.Parameters.Add("State", OracleDbType.Varchar2).Value = state.ToString();
                command.Parameters.Add("RetryAt", OracleDbType.Int64).Value = (object?)nextAttemptUtc?.Ticks ?? DBNull.Value;
                command.Parameters.Add("ErrorCode", OracleDbType.Varchar2).Value = (object?)errorCode ?? DBNull.Value;
                command.Parameters.Add("ErrorMessage", OracleDbType.Clob).Value = (object?)message ?? DBNull.Value;
                command.Parameters.Add("Recovery", OracleDbType.Clob).Value = (object?)recovery ?? DBNull.Value;
                command.Parameters.Add("Digest", OracleDbType.Varchar2).Value = recovery == null ? DBNull.Value : HolonRecoveryEvidenceJson.Digest(recovery);
                await UpdateOneAsync(command, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        private Task<OASISResult<bool>> UpdateOwnedAsync(HolonDeliveryClaim claim,
            Func<OracleConnection, OracleTransaction, DeliveryRow, DateTime, Task> update, CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(claim);
            ValidateTarget(claim.TargetId);
            return MutateAsync(async (connection, transaction, now) =>
            {
                var row = await ReadRowAsync(connection, transaction, null, claim.Sequence, token).ConfigureAwait(false);
                if (row == null || row.Inspection.State != HolonDeliveryState.InFlight
                    || row.Inspection.OperationId != claim.OperationId || row.Inspection.TargetId != claim.TargetId
                    || row.Worker != claim.WorkerId || row.Lease != claim.LeaseToken || row.LeaseUntil <= now.Ticks)
                    return false;
                await update(connection, transaction, row, now).ConfigureAwait(false);
                return true;
            }, token);
        }

        private Task<OASISResult<T>> MutateAsync<T>(
            Func<OracleConnection, OracleTransaction, DateTime, Task<T>> mutate, CancellationToken token)
            => ResultAsync(() => ReadBoundAsync(async (connection, transaction) =>
            {
                // Source publication and outbox state transitions share this native serialization point.
                await using var guard = CreateCommand("SELECT SLOT FROM OASIS_STORAGE_IDENTITY WHERE SLOT=1 FOR UPDATE", connection);
                guard.Transaction = transaction;
                if (Convert.ToInt32(await guard.ExecuteScalarAsync(token).ConfigureAwait(false)) != 1)
                    throw new SourceBindingException();
                await using var clock = CreateCommand("SELECT CAST(SYS_EXTRACT_UTC(SYSTIMESTAMP) AS TIMESTAMP) FROM DUAL", connection);
                clock.Transaction = transaction;
                DateTime now;
                await using (var reader = await clock.ExecuteReaderAsync(token).ConfigureAwait(false))
                {
                    if (!await reader.ReadAsync(token).ConfigureAwait(false))
                        throw new InvalidDataException("Oracle did not return its UTC clock.");
                    now = DateTime.SpecifyKind(reader.GetDateTime(0), DateTimeKind.Utc);
                }
                var result = await mutate(connection, transaction, now).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
                return result;
            }, token), token);

        private static OracleCommand DeliveryUpdate(OracleConnection connection, OracleTransaction transaction, long sequence, string assignments)
        {
            var command = CreateCommand($"UPDATE OASIS_OUTBOUND_DELIVERIES SET {assignments} WHERE DELIVERY_SEQUENCE=:Sequence", connection);
            command.Transaction = transaction;
            command.Parameters.Add("Sequence", OracleDbType.Int64).Value = sequence;
            return command;
        }

        private static async Task UpdateOneAsync(OracleCommand command, CancellationToken token)
        {
            if (await command.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1)
                throw new InvalidDataException("Oracle did not update exactly one outbound delivery.");
        }

        private static void ValidateTarget(string targetId)
        {
            ArgumentNullException.ThrowIfNull(targetId);
            HolonReplicationRoutePage.ValidateRequest(1, targetId);
        }

        private static void ValidateLease(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero || duration > TimeSpan.FromHours(1))
                throw new ArgumentOutOfRangeException(nameof(duration));
        }
    }
}
