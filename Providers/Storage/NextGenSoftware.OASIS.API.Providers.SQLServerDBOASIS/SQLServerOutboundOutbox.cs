using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Microsoft.Data.SqlClient;

namespace NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS;

public partial class SQLServerDBOASIS : IBoundHolonReplicationOutboxSource, IBoundAtomicHolonReplicationStorageProvider
{
    public OASISResult<IEnumerable<IHolon>> ExecuteReplicatingHolonWrite(
        ConditionalHolonWriteRequest request, HolonReplicationEnrollment enrollment)
        => ExecuteReplicatingHolonWriteAsync(request, enrollment).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ExecuteReplicatingHolonWriteAsync(
        ConditionalHolonWriteRequest request, HolonReplicationEnrollment enrollment,
        CancellationToken cancellationToken = default)
        => ExecuteSqlServerConditionalWriteAsync(request, enrollment ?? throw new ArgumentNullException(nameof(enrollment)), cancellationToken);

    public OASISResult<IHolonReplicationRouteInventoryOutbox> GetBoundHolonReplicationOutbox(CosmicStorageIdentity expectedSource)
    {
        ArgumentNullException.ThrowIfNull(expectedSource);
        return IsProviderActivated && expectedSource.Authority == ReplicaAuthority
            ? new(new SqlServerOutboundOutbox(this, expectedSource))
            : new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_OUTBOX_SOURCE_UNAVAILABLE",
                Message = "An active SQL Server provider and an exact SQL Server source binding are required." };
    }

    public OASISResult<IHolonReplicationReadinessOutbox> GetHolonReplicationOutbox()
    {
        var identity = CaptureStorageIdentityAsync().GetAwaiter().GetResult();
        return identity.IsError
            ? new() { IsError = true, ErrorCount = 1, ErrorCode = identity.ErrorCode,
                Message = identity.Message, Exception = identity.Exception }
            : new(new SqlServerOutboundOutbox(this, identity.Result));
    }

    private sealed partial class SqlServerOutboundOutbox : IHolonReplicationRouteInventoryOutbox
    {
        private static async Task<OASISResult<T>> ResultAsync<T>(Func<Task<T>> operation, CancellationToken token)
        {
            try { return new(await operation().ConfigureAwait(false)); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception cause)
            {
                return new() { IsError = true, ErrorCount = 1, Exception = cause, Message = cause.Message,
                    ErrorCode = cause is SourceBindingException ? "COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH"
                        : "COSMIC_SQLSERVER_OUTBOX_FAILED" };
            }
        }

        private sealed class SourceBindingException() : InvalidOperationException(
            "The physical SQL Server database differs from the enrolled outbox source.") { }

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
                    "DeliveryState='InFlight', WorkerId=@Worker, LeaseToken=@Token, LeaseUntil=@Until, RetryAt=NULL, AttemptCount=@Attempt");
                command.Parameters.Add("@Worker", System.Data.SqlDbType.NVarChar).Value = workerId;
                command.Parameters.Add("@Token", System.Data.SqlDbType.UniqueIdentifier).Value = lease;
                command.Parameters.Add("@Until", System.Data.SqlDbType.BigInt).Value = until.Ticks;
                command.Parameters.Add("@Attempt", System.Data.SqlDbType.Int).Value = attempt;
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
                await using var command = DeliveryUpdate(connection, transaction, claim.Sequence, "LeaseUntil=@Until");
                command.Parameters.Add("@Until", System.Data.SqlDbType.BigInt).Value = Math.Max(row.LeaseUntil, now.Add(leaseDuration).Ticks);
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
                    "DeliveryState=@State, WorkerId=NULL, LeaseToken=NULL, LeaseUntil=NULL, RetryAt=@RetryAt, "
                    + "ErrorCode=@ErrorCode, ErrorMessage=@ErrorMessage, RecoveryJson=@Recovery, RecoveryDigest=@Digest");
                command.Parameters.Add("@State", System.Data.SqlDbType.NVarChar).Value = state.ToString();
                command.Parameters.Add("@RetryAt", System.Data.SqlDbType.BigInt).Value = (object?)nextAttemptUtc?.Ticks ?? DBNull.Value;
                command.Parameters.Add("@ErrorCode", System.Data.SqlDbType.NVarChar).Value = (object?)errorCode ?? DBNull.Value;
                command.Parameters.Add("@ErrorMessage", System.Data.SqlDbType.NVarChar, -1).Value = (object?)message ?? DBNull.Value;
                command.Parameters.Add("@Recovery", System.Data.SqlDbType.NVarChar, -1).Value = (object?)recovery ?? DBNull.Value;
                command.Parameters.Add("@Digest", System.Data.SqlDbType.NVarChar).Value = recovery == null ? DBNull.Value : HolonRecoveryEvidenceJson.Digest(recovery);
                await UpdateOneAsync(command, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        private Task<OASISResult<bool>> UpdateOwnedAsync(HolonDeliveryClaim claim,
            Func<SqlConnection, SqlTransaction, DeliveryRow, DateTime, Task> update, CancellationToken token)
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
            Func<SqlConnection, SqlTransaction, DateTime, Task<T>> mutate, CancellationToken token)
            => ResultAsync(() => ReadBoundAsync(async (connection, transaction) =>
            {
                // Source publication and outbox state transitions share this native serialization point.
                await using var guard = new SqlCommand("SELECT Slot FROM dbo.OASISOutboundSequence WITH (UPDLOCK,HOLDLOCK) WHERE Slot=1;", connection, transaction);
                if (Convert.ToInt32(await guard.ExecuteScalarAsync(token).ConfigureAwait(false)) != 1)
                    throw new SourceBindingException();
                await using var clock = new SqlCommand("SELECT SYSUTCDATETIME();", connection, transaction);
                DateTime now;
                await using (var reader = await clock.ExecuteReaderAsync(token).ConfigureAwait(false))
                {
                    if (!await reader.ReadAsync(token).ConfigureAwait(false))
                        throw new InvalidDataException("SQL Server did not return its UTC clock.");
                    now = DateTime.SpecifyKind(reader.GetDateTime(0), DateTimeKind.Utc);
                }
                var result = await mutate(connection, transaction, now).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
                return result;
            }, token), token);

        private static SqlCommand DeliveryUpdate(SqlConnection connection, SqlTransaction transaction, long sequence, string assignments)
        {
            var command = new SqlCommand($"UPDATE dbo.OASISOutboundDeliveries SET {assignments} WHERE DeliverySequence=@Sequence;", connection, transaction);
            command.Parameters.Add("@Sequence", System.Data.SqlDbType.BigInt).Value = sequence;
            return command;
        }

        private static async Task UpdateOneAsync(SqlCommand command, CancellationToken token)
        {
            if (await command.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1)
                throw new InvalidDataException("SQL Server did not update exactly one outbound delivery.");
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
