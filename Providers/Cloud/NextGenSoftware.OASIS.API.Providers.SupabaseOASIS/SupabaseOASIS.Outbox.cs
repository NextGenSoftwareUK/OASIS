using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS;

public partial class SupabaseOASIS : IBoundHolonReplicationOutboxSource, IBoundAtomicHolonReplicationStorageProvider
{
    public OASISResult<IEnumerable<IHolon>> ExecuteReplicatingHolonWrite(
        ConditionalHolonWriteRequest request, HolonReplicationEnrollment enrollment)
        => ExecuteReplicatingHolonWriteAsync(request, enrollment).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ExecuteReplicatingHolonWriteAsync(
        ConditionalHolonWriteRequest request, HolonReplicationEnrollment enrollment, CancellationToken cancellationToken = default)
        => ExecuteSupabaseConditionalWriteAsync(request, enrollment ?? throw new ArgumentNullException(nameof(enrollment)), cancellationToken);

    public OASISResult<IHolonReplicationRouteInventoryOutbox> GetBoundHolonReplicationOutbox(CosmicStorageIdentity expectedSource)
    {
        ArgumentNullException.ThrowIfNull(expectedSource);
        return IsProviderActivated && Guid.TryParse(expectedSource.StoreId, out var storeId) && storeId != Guid.Empty
            && CreateSupabaseStorageIdentity(storeId).BindingFingerprint == expectedSource.BindingFingerprint
            ? new(new SupabaseOutboundOutbox(this, expectedSource, storeId))
            : new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_OUTBOX_SOURCE_UNAVAILABLE",
                Message = "An active Supabase provider and its bound endpoint are required." };
    }

    public OASISResult<IHolonReplicationReadinessOutbox> GetHolonReplicationOutbox()
    {
        var identity = CaptureStorageIdentityAsync().GetAwaiter().GetResult();
        if (identity.IsError)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = identity.ErrorCode,
                Message = identity.Message, Exception = identity.Exception };
        return new(new SupabaseOutboundOutbox(this, identity.Result, Guid.Parse(identity.Result.StoreId)));
    }

    private sealed class SupabaseOutboundOutbox(SupabaseOASIS provider, CosmicStorageIdentity source, Guid storeId)
        : IHolonReplicationRouteInventoryOutbox
    {
        public Task<OASISResult<HolonDeliveryInspection>> InspectAsync(long sequence, CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequence);
            return ReadOneAsync(sequence, null, cancellationToken);
        }

        public Task<OASISResult<HolonDeliveryInspection>> PeekNextAsync(string targetId, CancellationToken cancellationToken = default)
        {
            ValidateTarget(targetId);
            return ReadOneAsync(null, targetId, cancellationToken);
        }

        private Task<OASISResult<HolonDeliveryInspection>> ReadOneAsync(long? sequence, string? target, CancellationToken token)
            => CallAsync<HolonDeliveryInspection>("oasis_read_outbound",
                ReadArguments(sequence, target, null, 1), root =>
                {
                    var row = OptionalRow(root);
                    if (!row.HasValue)
                        return null!;
                    var inspection = InspectRow(row.Value);
                    if ((sequence.HasValue && inspection.Sequence != sequence) || (target != null && inspection.TargetId != target))
                        throw new InvalidDataException("Supabase returned a different outbound delivery.");
                    return inspection;
                }, token);

        public Task<OASISResult<HolonReplicationRoutePage>> ReadRouteHeadsAsync(int pageSize = 64,
            string? afterTargetId = null, CancellationToken cancellationToken = default)
        {
            HolonReplicationRoutePage.ValidateRequest(pageSize, afterTargetId);
            return CallAsync("oasis_read_outbound", ReadArguments(null, null, afterTargetId, pageSize + 1), root =>
            {
                var heads = root.EnumerateArray().Select(InspectRow).ToArray();
                if (heads.Length > pageSize + 1)
                    throw new InvalidDataException("Supabase returned an oversized route page.");
                var cursor = afterTargetId;
                foreach (var head in heads)
                {
                    if (cursor != null && HolonReplicationRoutePage.CompareTargetIds(head.TargetId, cursor) <= 0)
                        throw new InvalidDataException("Supabase returned routes outside binary target order.");
                    cursor = head.TargetId;
                }
                return new HolonReplicationRoutePage(heads.Take(pageSize), heads.Length > pageSize);
            }, cancellationToken);
        }

        public Task<OASISResult<HolonDeliveryClaim>> ClaimNextAsync(string targetId, string workerId,
            TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            ValidateTarget(targetId);
            if (string.IsNullOrWhiteSpace(workerId) || workerId.Length > 256)
                throw new ArgumentException("A bounded worker identity is required.", nameof(workerId));
            return CallAsync<HolonDeliveryClaim>("oasis_claim_outbound", new { expected_store = storeId,
                requested_target = targetId, requested_worker = workerId, lease_milliseconds = LeaseMilliseconds(leaseDuration) }, root =>
            {
                var optional = OptionalRow(root);
                if (!optional.HasValue)
                    return null!;
                var row = optional.Value;
                var inspection = InspectRow(row);
                var lease = row.GetProperty("lease_token").GetGuid();
                if (lease == Guid.Empty || inspection.TargetId != targetId || inspection.State != HolonDeliveryState.InFlight
                    || row.GetProperty("worker_id").GetString() != workerId || inspection.AttemptCount < 1)
                    throw new InvalidDataException("Supabase returned invalid outbound claim ownership.");
                return new HolonDeliveryClaim(inspection.Sequence, inspection.OperationId, targetId, workerId, lease,
                    row.GetProperty("payload_json").GetString(), inspection.AttemptCount,
                    row.GetProperty("lease_until").GetDateTimeOffset().UtcDateTime);
            }, cancellationToken);
        }

        public Task<OASISResult<bool>> RenewAsync(HolonDeliveryClaim claim, TimeSpan leaseDuration,
            CancellationToken cancellationToken = default)
        {
            var args = OwnedArguments(claim);
            args.Add("lease_milliseconds", LeaseMilliseconds(leaseDuration));
            return CallAsync("oasis_renew_outbound", args, root => root.GetBoolean(), cancellationToken);
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
            CancellationToken token)
        {
            var state = outcome switch
            {
                HolonDeliveryOutcome.Completed => HolonDeliveryState.Completed,
                HolonDeliveryOutcome.Retry => HolonDeliveryState.Pending,
                HolonDeliveryOutcome.ReconciliationRequired => HolonDeliveryState.ReconciliationRequired,
                _ => throw new ArgumentOutOfRangeException(nameof(outcome))
            };
            if ((outcome == HolonDeliveryOutcome.Retry ? nextAttemptUtc?.Kind != DateTimeKind.Utc : nextAttemptUtc != null)
                || (evidence?.RequiresNativeReconciliation == true && state != HolonDeliveryState.ReconciliationRequired))
                throw new ArgumentException("Recovery evidence and retry time must match the delivery outcome.");
            var args = OwnedArguments(claim);
            var recovery = evidence == null ? null : HolonRecoveryEvidenceJson.Write(evidence);
            args.Add("next_state", state.ToString());
            args.Add("next_attempt", nextAttemptUtc);
            args.Add("failure_code", errorCode);
            args.Add("failure_message", message);
            args.Add("recovery", recovery);
            args.Add("recovery_hash", recovery == null ? null : HolonRecoveryEvidenceJson.Digest(recovery));
            return CallAsync("oasis_finish_outbound", args, root => root.GetBoolean(), token);
        }

        private object ReadArguments(long? sequence, string? target, string? cursor, int limit)
            => new { expected_store = storeId, requested_sequence = sequence, requested_target = target,
                after_target = cursor, page_limit = limit };

        private Dictionary<string, object?> OwnedArguments(HolonDeliveryClaim claim)
        {
            ArgumentNullException.ThrowIfNull(claim);
            ValidateTarget(claim.TargetId);
            return new() { ["expected_store"] = storeId, ["requested_sequence"] = claim.Sequence,
                ["requested_operation"] = claim.OperationId, ["requested_target"] = claim.TargetId,
                ["requested_worker"] = claim.WorkerId, ["requested_token"] = claim.LeaseToken };
        }

        private static void ValidateTarget(string target)
        {
            ArgumentNullException.ThrowIfNull(target);
            HolonReplicationRoutePage.ValidateRequest(1, target);
        }

        private static long LeaseMilliseconds(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero || duration > TimeSpan.FromHours(1))
                throw new ArgumentOutOfRangeException(nameof(duration));
            return (long)Math.Ceiling(duration.TotalMilliseconds);
        }

        private static JsonElement? OptionalRow(JsonElement root)
            => root.GetArrayLength() switch
            {
                0 => null,
                1 => root[0],
                _ => throw new InvalidDataException("Supabase returned multiple outbound rows for a single selector.")
            };

        private HolonDeliveryInspection InspectRow(JsonElement row)
        {
            var sequence = row.GetProperty("delivery_sequence").GetInt64();
            var operation = row.GetProperty("operation_id").GetGuid();
            var target = row.GetProperty("target_id").GetString()!;
            var payload = row.GetProperty("payload_json").GetString()!;
            var stateText = row.GetProperty("delivery_state").GetString();
            var attempts = row.GetProperty("attempt_count").GetInt32();
            if (sequence <= 0 || operation == Guid.Empty || attempts < 0
                || !Enum.TryParse<HolonDeliveryState>(stateText, out var state) || state.ToString() != stateText || !Enum.IsDefined(state)
                || HolonRecoveryEvidenceJson.Digest(payload) != row.GetProperty("payload_digest").GetString())
                throw new InvalidDataException("Supabase outbound delivery metadata or payload is invalid.");
            var request = CommittedHolonReplicaPayload.ReadForTarget(payload, target);
            var route = request.NativeRoutes.Single(binding => binding.TargetId == target);
            if (request.OperationId != operation || request.SourceAuthority != ReplicaAuthority
                || route.SourceStorage.BindingFingerprint != source.BindingFingerprint)
                throw new InvalidDataException("Supabase outbound payload differs from the enrolled route.");
            return new(sequence, operation, target, state, attempts, row.GetProperty("error_code").GetString(),
                row.GetProperty("error_message").GetString(), HolonRecoveryEvidenceJson.ReadStored(
                    row.GetProperty("recovery_json").GetString(), row.GetProperty("recovery_digest").GetString(), state), route);
        }

        private async Task<OASISResult<T>> CallAsync<T>(string rpc, object args, Func<JsonElement, T> project, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (!provider.IsProviderActivated)
                    throw new InvalidOperationException("Activate the enrolled Supabase provider before opening its outbox.");
                using var document = await provider.ReadReplicaRpcAsync(rpc, args, token).ConfigureAwait(false);
                return new(project(document.RootElement));
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception cause)
            {
                return new() { IsError = true, ErrorCount = 1, Exception = cause, Message = cause.Message,
                    ErrorCode = cause is HttpRequestException { StatusCode: HttpStatusCode.Conflict }
                        ? "COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH" : "COSMIC_SUPABASE_OUTBOX_FAILED" };
            }
        }
    }
}
