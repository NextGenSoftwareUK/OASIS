using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS : IBoundCommittedHolonReplicaStorageProvider
{
    public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
        => ApplyCommittedHolonReplicaAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CancellationToken cancellationToken = default)
    {
        var identity = await CaptureStorageIdentityAsync(cancellationToken).ConfigureAwait(false);
        return identity.IsError ? GraphFailure(identity.ErrorCode, identity.Message, identity.Exception)
            : await ApplyBoundCommittedHolonReplicaAsync(request, identity.Result, cancellationToken).ConfigureAwait(false);
    }

    public OASISResult<IEnumerable<IHolon>> ApplyBoundCommittedHolonReplica(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage)
        => ApplyBoundCommittedHolonReplicaAsync(request, expectedStorage).GetAwaiter().GetResult();

    public async Task<OASISResult<IEnumerable<IHolon>>> ApplyBoundCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedStorage);
        var token = cancellationToken;
        string? submission = null;
        try
        {
            token.ThrowIfCancellationRequested();
            CommittedHolonReplicaPlan.ValidateNativeDestination(request, expectedStorage);
            var plan = CommittedHolonReplicaPlan.Create(request, StorageProviderType);
            if (!IsProviderActivated || _client.Config.MaxErrorRetry != 0)
                throw new InvalidOperationException("DynamoDB replica apply requires an active single-attempt client.");
            if (plan.Nodes.Count > 49)
                throw new ArgumentException("DynamoDB replica transactions allow 49 graph nodes plus identity and receipt.");

            var fingerprint = await ReadStorageEndpointFingerprintAsync(token).ConfigureAwait(false);
            var receiptKey = ReplicaReceiptKey(plan.OperationId);
            var reads = new List<TransactGetItem>
            {
                new() { Get = ReceiptTableGet(StorageIdentityKey) },
                new() { Get = ReceiptTableGet(receiptKey) }
            };
            foreach (var node in plan.Nodes)
            {
                reads.Add(new() { Get = new Get { TableName = "OasisHolons", Key = LifecycleKey(node.Snapshot.Id) } });
                reads.Add(new() { Get = ReceiptTableGet(ReplicaHeadKey(node.Snapshot.Id)) });
            }
            var state = await _client.TransactGetItemsAsync(new TransactGetItemsRequest { TransactItems = reads }, token)
                .ConfigureAwait(false);
            if (state.HttpStatusCode != HttpStatusCode.OK || state.Responses?.Count != reads.Count)
                throw new InvalidDataException("DynamoDB returned an incomplete replica snapshot.");
            var identity = state.Responses[0].Item;
            if (identity == null || !identity.TryGetValue("store_id", out var nativeStore)
                || !Guid.TryParseExact(nativeStore.S, "D", out var storeId) || storeId == Guid.Empty
                || new CosmicStorageIdentity(StorageProviderType, fingerprint, storeId).BindingFingerprint != expectedStorage.BindingFingerprint)
                return GraphFailure("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", "DynamoDB differs from the enrolled replica store.");

            var receipt = state.Responses[1].Item;
            if (receipt?.Count > 0)
                return receipt["request_hash"].S == plan.Fingerprint
                    ? GraphAcknowledgement(ConditionalHolonReceipt.Read(receipt["result_json"].S, plan), plan.Operation)
                    : GraphFailure("COSMIC_REPLICA_ACKNOWLEDGEMENT_INVALID", "DynamoDB receipt belongs to another request.");

            var writes = new List<TransactWriteItem>
            {
                new() { ConditionCheck = new ConditionCheck { TableName = ConditionalReceiptTable,
                    Key = ReceiptTableGet(StorageIdentityKey).Key, ConditionExpression = "#value = :expected",
                    ExpressionAttributeNames = new() { ["#value"] = "store_id" },
                    ExpressionAttributeValues = new() { [":expected"] = new() { S = nativeStore.S } } } }
            };
            var records = new Dictionary<Guid, IHolon>();
            long bytes = 0;
            var deleting = plan.Operation == ConditionalHolonOperation.HardDelete;
            for (var index = 0; index < plan.Nodes.Count; index++)
            {
                var record = plan.Nodes[index].Snapshot;
                var currentItem = state.Responses[2 + index * 2].Item;
                var headItem = state.Responses[3 + index * 2].Item;
                var current = currentItem?.Count > 0 ? ReadStoredIdentity<Holon>(currentItem, record.Id) : null;
                var hasHead = headItem?.Count > 0;
                if (hasHead)
                {
                    var head = HolonReplicaHeadRecord.Read(record.Id, StorageProviderType,
                        headItem!["source_id"].S, headItem["source_authority"].S, headItem["snapshot_json"].S,
                        headItem["snapshot_digest"].S, headItem["revision_id"].S, headItem["hard_deleted"].BOOL).Evidence;
                    HolonReplicaHeadRecord.ValidateCurrent(record.Id, StorageProviderType, current, head);
                    if (head.SourceId != plan.SourceId || head.SourceAuthority != plan.SourceAuthority
                        || head.VersionId != record.PreviousVersionId || head.Version != record.Version - 1)
                        return GraphFailure("COSMIC_REPLICA_LINEAGE_CONFLICT", "DynamoDB replica predecessor differs from the committed source.");
                }
                else if (current != null || record.Version != 1)
                    return GraphFailure("COSMIC_REPLICA_LINEAGE_CONFLICT", "DynamoDB replica requires an initial revision.");

                record.ProviderUniqueStorageKey ??= new();
                record.ProviderUniqueStorageKey[StorageProviderType] = record.Id.ToString("D");
                record.PreviousVersionProviderUniqueStorageKey?.Remove(StorageProviderType);
                record.ProviderMetaData?.Remove(StorageProviderType);
                record.InstanceSavedOnProviderType = new EnumValue<Core.Enums.ProviderType>(StorageProviderType);
                var prior = current == null ? null : OasisJson.ParseStoredObject(currentItem!["data_json"].S);
                var json = (await StageRecordAsync(record, prior).ConfigureAwait(false)).ToString(Newtonsoft.Json.Formatting.None);
                var stored = OasisJson.DeserializeStored<Holon>(json);
                plan.ValidateAcknowledgedRecord(stored);
                var currentCondition = current == null ? "attribute_not_exists(#value)" : "#value = :expected";
                var currentNames = new Dictionary<string, string> { ["#value"] = current == null ? "id" : "data_json" };
                var currentValues = current == null ? null : new Dictionary<string, AttributeValue>
                    { [":expected"] = new() { S = currentItem!["data_json"].S } };
                if (deleting)
                {
                    if (current == null)
                        return GraphFailure("COSMIC_REPLICA_LINEAGE_CONFLICT", "DynamoDB hard delete requires a current replica.");
                    writes.Add(new() { Delete = new Delete { TableName = "OasisHolons", Key = LifecycleKey(record.Id),
                        ConditionExpression = currentCondition, ExpressionAttributeNames = currentNames, ExpressionAttributeValues = currentValues } });
                    bytes += MeasureGraphItem(currentItem!);
                }
                else
                {
                    var item = LifecycleKey(record.Id);
                    item["data_json"] = new() { S = json };
                    item["parentHolonId"] = new() { S = stored.ParentHolonId.ToString("D") };
                    item["holon_type"] = new() { N = ((int)stored.HolonType).ToString(System.Globalization.CultureInfo.InvariantCulture) };
                    item["is_deleted"] = new() { BOOL = stored.IsDeleted };
                    bytes += Math.Max(MeasureGraphItem(item), current == null ? 0 : MeasureGraphItem(currentItem!));
                    writes.Add(new() { Put = new Put { TableName = "OasisHolons", Item = item,
                        ConditionExpression = currentCondition, ExpressionAttributeNames = currentNames, ExpressionAttributeValues = currentValues } });
                }

                var snapshot = StoredRecordJson.Serialize<IHolon>(stored);
                var lineage = new Dictionary<string, AttributeValue>
                {
                    ["id"] = new() { S = ReplicaHeadKey(stored.Id) },
                    ["source_id"] = new() { S = plan.SourceId },
                    ["source_authority"] = new() { S = plan.SourceAuthority.ToString() },
                    ["snapshot_json"] = new() { S = snapshot },
                    ["snapshot_digest"] = new() { S = HolonRecoveryEvidenceJson.Digest(snapshot) },
                    ["revision_id"] = new() { S = CosmicConcurrency.GetProviderRevisionId(stored, StorageProviderType)!.Value.ToString("D") },
                    ["hard_deleted"] = new() { BOOL = deleting }
                };
                bytes += Math.Max(MeasureGraphItem(lineage), hasHead ? MeasureGraphItem(headItem!) : 0);
                writes.Add(ReplicaReceiptPut(lineage, hasHead ? headItem!["revision_id"].S : null));
                records.Add(stored.Id, stored);
            }

            foreach (var edge in plan.ChildIds)
                records[edge.Key].Children = edge.Value.Select(id => records[id]).ToList();
            var receiptJson = ConditionalHolonReceipt.Write(plan.RootIds.Select(id => records[id]).ToArray(), plan);
            var acknowledged = ConditionalHolonReceipt.Read(receiptJson, plan);
            var receiptItem = new Dictionary<string, AttributeValue>
            {
                ["id"] = new() { S = receiptKey },
                ["request_hash"] = new() { S = plan.Fingerprint },
                ["result_json"] = new() { S = receiptJson }
            };
            if (bytes + MeasureGraphItem(receiptItem) > 4L * 1024 * 1024)
                throw new ArgumentException("DynamoDB replica graph exceeds the 4 MiB transaction limit.");
            writes.Add(ReplicaReceiptPut(receiptItem, null));
            token.ThrowIfCancellationRequested();
            submission = Guid.NewGuid().ToString("N");
            var response = await _client.TransactWriteItemsAsync(new TransactWriteItemsRequest
                { ClientRequestToken = submission, TransactItems = writes }, CancellationToken.None).ConfigureAwait(false);
            if (response.HttpStatusCode != HttpStatusCode.OK)
                throw new InvalidDataException("DynamoDB did not acknowledge the replica transaction.");
            return GraphAcknowledgement(acknowledged, plan.Operation);
        }
        catch (TransactionCanceledException cause) when (cause.CancellationReasons?.Any(reason => reason.Code == "ConditionalCheckFailed") == true
            && cause.CancellationReasons.All(reason => reason.Code is "None" or "ConditionalCheckFailed"))
        {
            return GraphFailure("COSMIC_REPLICA_STATE_CONFLICT", "DynamoDB replica conditions changed before commit.", cause);
        }
        catch (OperationCanceledException) when (submission == null && token.IsCancellationRequested) { throw; }
        catch (Exception cause)
        {
            return GraphFailure(submission == null ? "COSMIC_REPLICA_WRITE_FAILED" : "COSMIC_CONDITIONAL_RECONCILIATION_REQUIRED",
                "DynamoDB replica apply failed.", submission == null ? cause
                    : new DynamoDBMutationReconciliationException(ConditionalReceiptTable, ReplicaReceiptKey(request.OperationId), cause, submission));
        }
    }

    private static string ReplicaHeadKey(Guid id) => $"replica-head:{id:D}";

    private static TransactWriteItem ReplicaReceiptPut(Dictionary<string, AttributeValue> item, string? previousRevision)
        => new() { Put = new Put { TableName = ConditionalReceiptTable, Item = item,
            ConditionExpression = previousRevision == null ? "attribute_not_exists(#value)" : "#value = :expected",
            ExpressionAttributeNames = new() { ["#value"] = previousRevision == null ? "id" : "revision_id" },
            ExpressionAttributeValues = previousRevision == null ? null : new() { [":expected"] = new() { S = previousRevision } } } };
}
