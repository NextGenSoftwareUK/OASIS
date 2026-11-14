using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS : ICosmicStorageIdentityProvider, ICommittedHolonReplicaReceiptStorageProvider
{
    private const string StorageIdentityKey = "oasis-replication-storage-identity";

    public async Task<OASISResult<CosmicStorageIdentity>> CaptureStorageIdentityAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsProviderActivated)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_NOT_READY",
                Message = "Activate DynamoDB before enrolling its storage identity." };

        try
        {
            var fingerprint = await ReadStorageEndpointFingerprintAsync(cancellationToken).ConfigureAwait(false);

            var key = new Dictionary<string, AttributeValue> { ["id"] = new() { S = StorageIdentityKey } };
            async Task<Guid?> ReadAsync()
            {
                var result = await _client.GetItemAsync(new GetItemRequest
                {
                    TableName = ConditionalReceiptTable, Key = key, ConsistentRead = true
                }, cancellationToken).ConfigureAwait(false);
                RequireItemRead(result);
                if (result.Item.Count == 0)
                    return null;
                if (!result.Item.TryGetValue("store_id", out var value) || !Guid.TryParseExact(value.S, "D", out var id)
                    || id == Guid.Empty)
                    throw new InvalidDataException("DynamoDB returned an invalid persisted storage identity.");
                return id;
            }

            var existing = await ReadAsync().ConfigureAwait(false);
            if (existing.HasValue)
                return new(new CosmicStorageIdentity(StorageProviderType, fingerprint, existing.Value));

            var storeId = Guid.NewGuid();
            var item = new Dictionary<string, AttributeValue>(key) { ["store_id"] = new() { S = storeId.ToString("D") } };
            try
            {
                var saved = await _client.PutItemAsync(new PutItemRequest
                {
                    TableName = ConditionalReceiptTable, Item = item,
                    ConditionExpression = "attribute_not_exists(#id)",
                    ExpressionAttributeNames = new() { ["#id"] = "id" }
                }, cancellationToken).ConfigureAwait(false);
                if (saved.HttpStatusCode != HttpStatusCode.OK)
                    throw new InvalidDataException("DynamoDB did not acknowledge storage identity enrollment.");
            }
            catch (ConditionalCheckFailedException)
            {
                storeId = await ReadAsync().ConfigureAwait(false)
                    ?? throw new InvalidDataException("The winning DynamoDB identity enrollment is absent.");
            }
            return new(new CosmicStorageIdentity(StorageProviderType, fingerprint, storeId));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_CAPTURE_FAILED",
                Message = "The DynamoDB store identity could not be captured.", Exception = cause };
        }
    }

    private async Task<string> ReadStorageEndpointFingerprintAsync(CancellationToken token)
    {
        var table = RequireTableObservation(await _client.DescribeTableAsync(
            new DescribeTableRequest { TableName = "OasisHolons" }, token).ConfigureAwait(false));
        if (string.IsNullOrWhiteSpace(table.TableArn))
            throw new InvalidDataException("DynamoDB did not identify the native holon table.");
        var endpoint = _client.Config.ServiceURL;
        var resource = string.IsNullOrEmpty(endpoint) ? table.TableArn
            : new Uri(endpoint).GetLeftPart(UriPartial.Path).TrimEnd('/') + "\n" + table.TableArn;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resource)));
    }

    private static string ReplicaReceiptKey(Guid operationId) => $"replica:{operationId:D}";

    public async Task<OASISResult<CommittedHolonReplicaReceiptEvidence>> ReadCommittedHolonReplicaReceiptAsync(
        CommittedHolonReplicaReceiptReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null || request.ExpectedStorage.Authority != StorageProviderType)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_RECEIPT_OPTIONS_INVALID",
                Message = "An exact DynamoDB replica receipt intent and binding are required." };

        try
        {
            if (!IsProviderActivated)
                throw new InvalidOperationException("Activate DynamoDB before reading replica receipts.");
            var plan = CommittedHolonReplicaReceiptProjection.Prepare(request);
            var fingerprint = await ReadStorageEndpointFingerprintAsync(cancellationToken).ConfigureAwait(false);
            var response = await _client.TransactGetItemsAsync(new TransactGetItemsRequest
            {
                TransactItems = new()
                {
                    new() { Get = ReceiptTableGet(StorageIdentityKey) },
                    new() { Get = ReceiptTableGet(ReplicaReceiptKey(plan.OperationId)) }
                }
            }, cancellationToken).ConfigureAwait(false);
            if (response.HttpStatusCode != HttpStatusCode.OK || response.Responses?.Count != 2)
                throw new InvalidDataException("DynamoDB did not return both transactional receipt observations.");
            var identity = response.Responses[0].Item;
            if (identity == null || !identity.TryGetValue("store_id", out var nativeId)
                || !Guid.TryParseExact(nativeId.S, "D", out var storeId) || storeId == Guid.Empty
                || new CosmicStorageIdentity(StorageProviderType, fingerprint, storeId).BindingFingerprint
                    != request.ExpectedStorage.BindingFingerprint)
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_RECEIPT_STORAGE_MISMATCH",
                    Message = "The DynamoDB store differs from the reviewed replica store." };

            var receipt = response.Responses[1].Item;
            var found = receipt?.Count > 0;
            if (found && (receipt!["id"].S != ReplicaReceiptKey(plan.OperationId)
                || string.IsNullOrEmpty(receipt["request_hash"].S) || string.IsNullOrEmpty(receipt["result_json"].S)))
                throw new InvalidDataException("DynamoDB returned an invalid native replica receipt.");
            var evidence = CommittedHolonReplicaReceiptProjection.Project(request, plan,
                found ? receipt!["request_hash"].S : null, found ? receipt!["result_json"].S : null,
                HolonReplicaHeadReadConsistency.NativeSnapshot);
            return new(evidence) { IsLoaded = true };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_RECEIPT_UNVERIFIED",
                Message = "The DynamoDB replica receipt could not be verified.", Exception = cause };
        }
    }

    private static TransactWriteItem RequireStorageIdentityAtCommit(string storeId) => new()
    {
        ConditionCheck = new ConditionCheck
        {
            TableName = ConditionalReceiptTable,
            Key = ReceiptTableGet(StorageIdentityKey).Key,
            ConditionExpression = "#value = :expected",
            ExpressionAttributeNames = new() { ["#value"] = "store_id" },
            ExpressionAttributeValues = new() { [":expected"] = new() { S = storeId } }
        }
    };

    private static Get ReceiptTableGet(string key) => new()
    {
        TableName = ConditionalReceiptTable,
        Key = new() { ["id"] = new() { S = key } }
    };
}
