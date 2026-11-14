using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS
{
    private const string OutboundSequenceKey = "outbound:sequence";

    private async Task StageDynamoDeliveriesAsync(ConditionalHolonPlan plan, IEnumerable<IHolon> acknowledged,
        List<TransactWriteItem> actions, List<long> sizes, CancellationToken cancellationToken)
    {
        var enrollment = plan.ReplicationEnrollment;
        var observed = await ReadBoundOutboundItemAsync(OutboundSequenceKey, enrollment.SourceStorageIdentity, cancellationToken);
        var sequence = observed?.Count > 0 ? ReadOutboundSequence(observed) : 0;
        var previous = sequence == 0 ? null : observed["sequence"].N;

        var payload = CommittedHolonReplicaPayload.Capture(new CommittedHolonReplicaRequest(
            plan.OperationId, enrollment.SourceId, plan.Authority, plan.ActorId, plan.Operation,
            acknowledged, plan.SaveChildren, plan.Recursive, plan.MaxChildDepth, enrollment.NativeRoutes));
        var digest = HolonRecoveryEvidenceJson.Digest(payload);

        foreach (var route in enrollment.NativeRoutes)
        {
            sequence = checked(sequence + 1);
            var item = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = OutboundDeliveryKey(sequence) },
                ["sequence"] = new() { N = sequence.ToString(CultureInfo.InvariantCulture) },
                ["operation_id"] = new() { S = plan.OperationId.ToString("D") },
                ["target_id"] = new() { S = route.TargetId },
                ["payload"] = new() { S = payload },
                ["digest"] = new() { S = digest },
                ["state"] = new() { S = nameof(HolonDeliveryState.Pending) },
                ["attempt_count"] = new() { N = "0" }
            };
            sizes.Add(MeasureGraphItem(item));
            actions.Add(ReplicaReceiptPut(item, null));
        }

        var counter = new Dictionary<string, AttributeValue>
        {
            ["id"] = new() { S = OutboundSequenceKey },
            ["sequence"] = new() { N = sequence.ToString(CultureInfo.InvariantCulture) }
        };
        sizes.Add(MeasureGraphItem(counter));
        actions.Add(previous == null ? ReplicaReceiptPut(counter, null) : new()
        {
            Put = new Put
            {
                TableName = ConditionalReceiptTable,
                Item = counter,
                ConditionExpression = "#sequence = :previous",
                ExpressionAttributeNames = new() { ["#sequence"] = "sequence" },
                ExpressionAttributeValues = new() { [":previous"] = new() { N = previous } }
            }
        });
    }

    private static string OutboundDeliveryKey(long sequence)
        => "outbound:delivery:" + sequence.ToString("D19", CultureInfo.InvariantCulture);

    private async Task<Dictionary<string, AttributeValue>?> ReadOutboundHeadAsync(
        string targetId, CosmicStorageIdentity expected, CancellationToken cancellationToken)
    {
        var counter = await ReadBoundOutboundItemAsync(OutboundSequenceKey, expected, cancellationToken);
        if (counter == null || counter.Count == 0)
            return null;
        var highWaterMark = ReadOutboundSequence(counter);

        // The sequence counter and its deliveries commit together. Every key
        // up to this watermark exists, including completed obligations.
        for (long sequence = 1; ; sequence++)
        {
            var key = OutboundDeliveryKey(sequence);
            var item = await ReadBoundOutboundItemAsync(key, expected, cancellationToken);
            if (item == null || item.Count == 0 || item["id"].S != key || ReadOutboundSequence(item) != sequence)
                throw new InvalidDataException("DynamoDB outbox sequence has a missing or inconsistent delivery.");
            if (item["target_id"].S == targetId && item["state"].S != nameof(HolonDeliveryState.Completed))
                return item;
            if (sequence == highWaterMark)
                return null;
        }
    }

    private async Task<HolonDeliveryClaim?> ClaimOutboundHeadAsync(string targetId, string workerId,
        TimeSpan duration, CosmicStorageIdentity expected, CancellationToken cancellationToken)
    {
        var item = await ReadOutboundHeadAsync(targetId, expected, cancellationToken);
        if (item == null)
            return null;

        var state = item["state"].S;
        var now = DateTime.UtcNow;
        if (state == nameof(HolonDeliveryState.ReconciliationRequired)
            || ReadOutboundTicks(item, "lease_until") > now.Ticks
            || ReadOutboundTicks(item, "retry_at") > now.Ticks)
            return null;
        if (state != nameof(HolonDeliveryState.Pending) && state != nameof(HolonDeliveryState.InFlight))
            throw new InvalidDataException("The DynamoDB delivery state cannot be claimed.");

        var payload = item["payload"].S;
        if (HolonRecoveryEvidenceJson.Digest(payload) != item["digest"].S)
            throw new InvalidDataException("The DynamoDB delivery payload checksum differs.");
        var request = CommittedHolonReplicaPayload.ReadForTarget(payload, targetId);
        if (request.OperationId.ToString("D") != item["operation_id"].S
            || !request.NativeRoutes.Any(route => route.TargetId == targetId
                && route.SourceStorage.BindingFingerprint == expected.BindingFingerprint))
            throw new InvalidDataException("The DynamoDB delivery differs from its enrolled source or operation.");

        var lease = Guid.NewGuid();
        var until = now.Add(duration);
        var attempts = checked((int)ReadOutboundTicks(item, "attempt_count") + 1);
        var replacement = new Dictionary<string, AttributeValue>(item)
        {
            ["state"] = new() { S = nameof(HolonDeliveryState.InFlight) },
            ["worker"] = new() { S = workerId },
            ["lease"] = new() { S = lease.ToString("D") },
            ["lease_until"] = new() { N = until.Ticks.ToString(CultureInfo.InvariantCulture) },
            ["attempt_count"] = new() { N = attempts.ToString(CultureInfo.InvariantCulture) }
        };
        replacement.Remove("retry_at");
        var write = new TransactWriteItem
        {
            Put = new Put
            {
                TableName = ConditionalReceiptTable,
                Item = replacement,
                ConditionExpression = "#state = :state AND attempt_count = :attempt AND " +
                    (state == nameof(HolonDeliveryState.Pending)
                        ? "(attribute_not_exists(retry_at) OR retry_at <= :now)"
                        : "lease_until <= :now"),
                ExpressionAttributeNames = new() { ["#state"] = "state" },
                ExpressionAttributeValues = new()
                {
                    [":state"] = item["state"],
                    [":attempt"] = item["attempt_count"],
                    [":now"] = new() { N = now.Ticks.ToString(CultureInfo.InvariantCulture) }
                }
            }
        };
        MeasureGraphItem(replacement);
        if (!await WriteOutboundLeaseAsync(expected, write, cancellationToken))
            return null;
        return new HolonDeliveryClaim(ReadOutboundSequence(item), request.OperationId, targetId, workerId,
            lease, payload, attempts, until);
    }

    private Task<bool> RenewOutboundLeaseAsync(HolonDeliveryClaim claim, TimeSpan duration,
        CosmicStorageIdentity expected, CancellationToken cancellationToken)
        => UpdateOwnedOutboundDeliveryAsync(claim, expected, (item, now) =>
        {
            var until = Math.Max(ReadOutboundTicks(item, "lease_until"), now.Add(duration).Ticks);
            item["lease_until"] = new() { N = until.ToString(CultureInfo.InvariantCulture) };
        }, cancellationToken);

    private Task<bool> FinishOutboundDeliveryAsync(HolonDeliveryClaim claim, HolonDeliveryOutcome outcome,
        HolonDeliveryRecoveryEvidence? evidence, string? errorCode, string? message, DateTime? retryAt,
        CosmicStorageIdentity expected, CancellationToken cancellationToken)
    {
        var state = outcome switch
        {
            HolonDeliveryOutcome.Completed => HolonDeliveryState.Completed,
            HolonDeliveryOutcome.Retry => HolonDeliveryState.Pending,
            HolonDeliveryOutcome.ReconciliationRequired => HolonDeliveryState.ReconciliationRequired,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome))
        };
        if ((outcome == HolonDeliveryOutcome.Completed ? errorCode != null || message != null
                : string.IsNullOrWhiteSpace(errorCode) || errorCode.Length > 128 || message?.Length > 4096)
            || (outcome == HolonDeliveryOutcome.Retry ? retryAt?.Kind != DateTimeKind.Utc : retryAt != null)
            || (evidence?.RequiresNativeReconciliation == true && state != HolonDeliveryState.ReconciliationRequired))
            throw new ArgumentException("Delivery diagnostics and retry time must match the outcome.");

        return UpdateOwnedOutboundDeliveryAsync(claim, expected, (item, now) =>
        {
            if (retryAt <= now)
                throw new ArgumentException("The next delivery attempt must be in the future.", nameof(retryAt));
            item["state"] = new() { S = state.ToString() };
            foreach (var field in new[] { "worker", "lease", "lease_until", "retry_at", "error", "message", "recovery", "recovery_digest" })
                item.Remove(field);
            if (retryAt.HasValue)
                item["retry_at"] = new() { N = retryAt.Value.Ticks.ToString(CultureInfo.InvariantCulture) };
            if (errorCode != null)
                item["error"] = new() { S = errorCode };
            if (message != null)
                item["message"] = new() { S = message };
            if (evidence != null)
            {
                var json = HolonRecoveryEvidenceJson.Write(evidence);
                item["recovery"] = new() { S = json };
                item["recovery_digest"] = new() { S = HolonRecoveryEvidenceJson.Digest(json) };
            }
        }, cancellationToken);
    }

    private async Task<bool> UpdateOwnedOutboundDeliveryAsync(HolonDeliveryClaim claim, CosmicStorageIdentity expected,
        Action<Dictionary<string, AttributeValue>, DateTime> update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claim);
        if (claim.Sequence <= 0 || claim.OperationId == Guid.Empty || claim.LeaseToken == Guid.Empty
            || string.IsNullOrWhiteSpace(claim.WorkerId) || string.IsNullOrWhiteSpace(claim.TargetId))
            throw new ArgumentException("A valid DynamoDB delivery claim is required.", nameof(claim));

        var item = await ReadBoundOutboundItemAsync(OutboundDeliveryKey(claim.Sequence), expected, cancellationToken);
        var now = DateTime.UtcNow;
        var observedUntil = item?.Count > 0 ? ReadOutboundTicks(item, "lease_until") : 0;
        if (observedUntil <= now.Ticks || item!["state"].S != nameof(HolonDeliveryState.InFlight)
            || item["target_id"].S != claim.TargetId || item["operation_id"].S != claim.OperationId.ToString("D")
            || item["worker"].S != claim.WorkerId || item["lease"].S != claim.LeaseToken.ToString("D"))
            return false;

        var previousUntil = item["lease_until"];
        update(item, now);
        MeasureGraphItem(item);
        var write = new TransactWriteItem
        {
            Put = new Put
            {
                TableName = ConditionalReceiptTable,
                Item = item,
                ConditionExpression = "#state = :inflight AND lease = :lease AND lease_until = :until AND lease_until > :now",
                ExpressionAttributeNames = new() { ["#state"] = "state" },
                ExpressionAttributeValues = new()
                {
                    [":inflight"] = new() { S = nameof(HolonDeliveryState.InFlight) },
                    [":lease"] = new() { S = claim.LeaseToken.ToString("D") },
                    [":until"] = previousUntil,
                    [":now"] = new() { N = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture) }
                }
            }
        };
        return await WriteOutboundLeaseAsync(expected, write, cancellationToken);
    }

    private async Task<bool> WriteOutboundLeaseAsync(CosmicStorageIdentity expected, TransactWriteItem write,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = new() { RequireStorageIdentityAtCommit(expected.StoreId), write }
            }, cancellationToken);
            if (response.HttpStatusCode != HttpStatusCode.OK)
                throw new InvalidDataException("DynamoDB did not acknowledge the delivery lease mutation.");
            return true;
        }
        catch (TransactionCanceledException cause) when (cause.CancellationReasons?.Count == 2
            && cause.CancellationReasons[0].Code == "None"
            && cause.CancellationReasons[1].Code == "ConditionalCheckFailed")
        {
            return false;
        }
    }

    private static long ReadOutboundTicks(Dictionary<string, AttributeValue> item, string field)
    {
        if (!item.TryGetValue(field, out var stored))
            return 0;
        if (!long.TryParse(stored.N, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 0)
            throw new InvalidDataException($"The DynamoDB delivery contains invalid {field}.");
        return value;
    }

    private static long ReadOutboundSequence(Dictionary<string, AttributeValue> item)
    {
        if (!item.TryGetValue("sequence", out var stored)
            || !long.TryParse(stored.N, NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
            || sequence <= 0)
            throw new InvalidDataException("The DynamoDB outbox sequence is invalid.");
        return sequence;
    }

    private async Task<Dictionary<string, AttributeValue>> ReadBoundOutboundItemAsync(
        string key, CosmicStorageIdentity expected, CancellationToken cancellationToken)
    {
        var fingerprint = await ReadStorageEndpointFingerprintAsync(cancellationToken);
        if (expected.Authority != StorageProviderType || fingerprint != expected.EndpointFingerprint)
            throw new DynamoSourceBindingException();

        var response = await _client.TransactGetItemsAsync(new TransactGetItemsRequest
        {
            TransactItems = new()
            {
                new() { Get = ReceiptTableGet(StorageIdentityKey) },
                new() { Get = ReceiptTableGet(key) }
            }
        }, cancellationToken);
        if (response.HttpStatusCode != HttpStatusCode.OK || response.Responses?.Count != 2)
            throw new InvalidDataException("DynamoDB returned an incomplete bound outbox snapshot.");

        var identity = response.Responses[0].Item;
        if (identity == null || !identity.TryGetValue("store_id", out var nativeStore) || nativeStore.S != expected.StoreId)
            throw new DynamoSourceBindingException();
        return response.Responses[1].Item;
    }

    private sealed class DynamoSourceBindingException : Exception
    {
        public DynamoSourceBindingException() : base("The physical DynamoDB store differs from the enrolled source.") { }
    }
}
