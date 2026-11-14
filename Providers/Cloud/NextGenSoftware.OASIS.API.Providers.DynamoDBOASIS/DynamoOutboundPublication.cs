using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
