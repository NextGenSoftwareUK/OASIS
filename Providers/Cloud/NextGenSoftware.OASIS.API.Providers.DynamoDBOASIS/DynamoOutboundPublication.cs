using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        var response = await _client.GetItemAsync(new GetItemRequest
        {
            TableName = ConditionalReceiptTable,
            Key = ReceiptTableGet(OutboundSequenceKey).Key,
            ConsistentRead = true
        }, cancellationToken);
        RequireItemRead(response);

        var hasCounter = response.Item.Count != 0;
        var previous = hasCounter && response.Item.TryGetValue("sequence", out var stored) ? stored.N : null;
        long sequence = 0;
        if (hasCounter && (!long.TryParse(previous, NumberStyles.None, CultureInfo.InvariantCulture, out sequence) || sequence <= 0))
            throw new InvalidDataException("The DynamoDB outbox sequence is invalid.");

        var enrollment = plan.ReplicationEnrollment;
        var payload = CommittedHolonReplicaPayload.Capture(new CommittedHolonReplicaRequest(
            plan.OperationId, enrollment.SourceId, plan.Authority, plan.ActorId, plan.Operation,
            acknowledged, plan.SaveChildren, plan.Recursive, plan.MaxChildDepth, enrollment.NativeRoutes));
        var digest = HolonRecoveryEvidenceJson.Digest(payload);

        foreach (var route in enrollment.NativeRoutes)
        {
            sequence = checked(sequence + 1);
            var item = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = "outbound:delivery:" + sequence.ToString("D19", CultureInfo.InvariantCulture) },
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

    private sealed class DynamoSourceBindingException : Exception
    {
        public DynamoSourceBindingException() : base("The physical DynamoDB store differs from the enrolled source.") { }
    }
}
