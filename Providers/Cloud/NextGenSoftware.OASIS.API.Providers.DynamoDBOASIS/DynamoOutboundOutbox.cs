using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS : IBoundHolonReplicationOutboxSource
{
    public OASISResult<IHolonReplicationReadinessOutbox> GetHolonReplicationOutbox()
    {
        var captured = CaptureStorageIdentityAsync().GetAwaiter().GetResult();
        return captured.IsError
            ? new() { IsError = true, ErrorCount = 1, ErrorCode = captured.ErrorCode,
                Message = captured.Message, Exception = captured.Exception }
            : new(new DynamoOutboundOutbox(this, captured.Result));
    }

    public OASISResult<IHolonReplicationRouteInventoryOutbox> GetBoundHolonReplicationOutbox(
        CosmicStorageIdentity expectedSource)
    {
        ArgumentNullException.ThrowIfNull(expectedSource);
        return IsProviderActivated && expectedSource.Authority == StorageProviderType
            ? new(new DynamoOutboundOutbox(this, expectedSource))
            : new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_OUTBOX_SOURCE_UNAVAILABLE",
                Message = "An active DynamoDB provider and an exact source binding are required." };
    }

    private static HolonDeliveryInspection InspectOutboundDelivery(
        Dictionary<string, AttributeValue> item, CosmicStorageIdentity expected)
    {
        var targetId = item["target_id"].S;
        var payload = item["payload"].S;
        if (HolonRecoveryEvidenceJson.Digest(payload) != item["digest"].S)
            throw new InvalidDataException("The DynamoDB delivery payload checksum differs.");
        var request = CommittedHolonReplicaPayload.ReadForTarget(payload, targetId);
        var route = request.NativeRoutes.SingleOrDefault(candidate => candidate.TargetId == targetId);
        if (request.OperationId.ToString("D") != item["operation_id"].S
            || route == null || route.SourceStorage.BindingFingerprint != expected.BindingFingerprint)
            throw new InvalidDataException("The DynamoDB delivery differs from its enrolled source or operation.");
        if (!Enum.TryParse<HolonDeliveryState>(item["state"].S, out var state) || state.ToString() != item["state"].S)
            throw new InvalidDataException("The DynamoDB delivery state is invalid.");
        var evidence = HolonRecoveryEvidenceJson.ReadStored(
            item.GetValueOrDefault("recovery")?.S, item.GetValueOrDefault("recovery_digest")?.S, state);
        return new(ReadOutboundSequence(item), request.OperationId, targetId, state,
            checked((int)ReadOutboundTicks(item, "attempt_count")), item.GetValueOrDefault("error")?.S,
            item.GetValueOrDefault("message")?.S, evidence, route);
    }

    private sealed class DynamoOutboundOutbox(DynamoDBOASIS provider, CosmicStorageIdentity expected)
        : IHolonReplicationRouteInventoryOutbox
    {
        public Task<OASISResult<HolonDeliveryClaim>> ClaimNextAsync(string targetId, string workerId,
            TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            ValidateTarget(targetId);
            ValidateLease(leaseDuration);
            if (string.IsNullOrWhiteSpace(workerId) || workerId.Length > 256)
                throw new ArgumentException("A bounded worker identity is required.", nameof(workerId));
            return ExecuteAsync<HolonDeliveryClaim>(() => provider.ClaimOutboundHeadAsync(
                targetId, workerId, leaseDuration, expected, cancellationToken), cancellationToken);
        }

        public Task<OASISResult<bool>> RenewAsync(HolonDeliveryClaim claim, TimeSpan leaseDuration,
            CancellationToken cancellationToken = default)
        {
            ValidateLease(leaseDuration);
            return ExecuteAsync(() => provider.RenewOutboundLeaseAsync(claim, leaseDuration, expected, cancellationToken),
                cancellationToken);
        }

        public Task<OASISResult<bool>> FinishAsync(HolonDeliveryClaim claim, HolonDeliveryOutcome outcome,
            string errorCode = null, string message = null, DateTime? nextAttemptUtc = null,
            CancellationToken cancellationToken = default)
            => ExecuteAsync(() => provider.FinishOutboundDeliveryAsync(claim, outcome, null, errorCode, message,
                nextAttemptUtc, expected, cancellationToken), cancellationToken);

        public Task<OASISResult<bool>> FinishWithRecoveryAsync(HolonDeliveryClaim claim, HolonDeliveryOutcome outcome,
            HolonDeliveryRecoveryEvidence recoveryEvidence, string errorCode = null, string message = null,
            DateTime? nextAttemptUtc = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(recoveryEvidence);
            return ExecuteAsync(() => provider.FinishOutboundDeliveryAsync(claim, outcome, recoveryEvidence,
                errorCode, message, nextAttemptUtc, expected, cancellationToken), cancellationToken);
        }

        public Task<OASISResult<HolonDeliveryInspection>> PeekNextAsync(string targetId,
            CancellationToken cancellationToken = default)
        {
            ValidateTarget(targetId);
            return ExecuteAsync<HolonDeliveryInspection>(async () =>
            {
                var item = await provider.ReadOutboundHeadAsync(targetId, expected, cancellationToken);
                return item == null ? null : InspectOutboundDelivery(item, expected);
            }, cancellationToken);
        }

        public Task<OASISResult<HolonDeliveryInspection>> InspectAsync(long sequence,
            CancellationToken cancellationToken = default)
        {
            if (sequence <= 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            return ExecuteAsync<HolonDeliveryInspection>(async () =>
            {
                var item = await provider.ReadOutboundDeliveryAsync(sequence, expected, cancellationToken);
                return item == null ? null : InspectOutboundDelivery(item, expected);
            }, cancellationToken);
        }

        public Task<OASISResult<HolonReplicationRoutePage>> ReadRouteHeadsAsync(int pageSize = 64,
            string afterTargetId = null, CancellationToken cancellationToken = default)
        {
            HolonReplicationRoutePage.ValidateRequest(pageSize, afterTargetId);
            return ExecuteAsync(async () =>
            {
                var heads = new SortedDictionary<string, HolonDeliveryInspection>(
                    Comparer<string>.Create(HolonReplicationRoutePage.CompareTargetIds));
                await foreach (var item in provider.ReadOutboundDeliveriesAsync(expected, cancellationToken))
                {
                    var target = item["target_id"].S;
                    if (item["state"].S == nameof(HolonDeliveryState.Completed) || heads.ContainsKey(target)
                        || (afterTargetId != null && HolonReplicationRoutePage.CompareTargetIds(target, afterTargetId) <= 0))
                        continue;
                    heads.Add(target, InspectOutboundDelivery(item, expected));
                    if (heads.Count > pageSize + 1)
                        heads.Remove(heads.Last().Key);
                }
                return new HolonReplicationRoutePage(heads.Values.Take(pageSize), heads.Count > pageSize);
            }, cancellationToken);
        }

        private async Task<OASISResult<T>> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!provider.IsProviderActivated)
                    throw new InvalidOperationException("Activate DynamoDB before accessing its outbox.");
                return new(await operation());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception cause)
            {
                return new() { IsError = true, ErrorCount = 1, Exception = cause, Message = cause.Message,
                    ErrorCode = cause is DynamoSourceBindingException
                        ? "COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH" : "COSMIC_DYNAMODB_OUTBOX_FAILED" };
            }
        }

        private static void ValidateTarget(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId) || targetId.Length > 256 || targetId.Trim() != targetId)
                throw new ArgumentException("An exact bounded target identity is required.", nameof(targetId));
        }

        private static void ValidateLease(TimeSpan leaseDuration)
        {
            if (leaseDuration <= TimeSpan.Zero || leaseDuration > TimeSpan.FromHours(1))
                throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }
    }
}
