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
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS : ICosmicStorageIdentityProvider
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
            var table = RequireTableObservation(await _client.DescribeTableAsync(
                new DescribeTableRequest { TableName = "OasisHolons" }, cancellationToken).ConfigureAwait(false));
            if (string.IsNullOrWhiteSpace(table.TableArn))
                throw new InvalidDataException("DynamoDB did not identify the native holon table.");
            var endpoint = _client.Config.ServiceURL;
            var resource = string.IsNullOrEmpty(endpoint) ? table.TableArn
                : new Uri(endpoint).GetLeftPart(UriPartial.Path).TrimEnd('/') + "\n" + table.TableArn;
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resource)));

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
}
