using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

public partial class CosmosDbClient
{
    public async Task DeleteUnchangedDocumentAsync(string documentId,object expected,PartitionKey? partitionKey,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(expected);
        var proposal=JObject.Parse(OasisJson.Serialize(expected));
        if(!Guid.TryParse(documentId,out var id)||id==Guid.Empty||proposal["Id"]?.ToString()!=documentId)
            throw new ArgumentException("Conditional delete requires matching nonempty Cosmos identity.");
        cancellationToken.ThrowIfCancellationRequested();
        var json=await ReadDocumentAsync(documentId,partitionKey,cancellationToken);
        if(json==null)throw new StorageLifecycleConflictException("Cosmos record disappeared before conditional deletion.");
        var observed=JObject.Parse(json);
        if(observed["id"]?.ToString()!=documentId||observed["Id"]?.ToString()!=documentId)
            throw new InvalidDataException("Cosmos observed identity differs from deletion target.");
        var etag=observed["_etag"]?.Type==JTokenType.String?observed["_etag"].Value<string>():null;
        if(string.IsNullOrWhiteSpace(etag))throw new InvalidDataException("Cosmos conditional deletion requires an observed ETag.");
        var payload=CosmosEmbeddedHistory.CanonicalPayload(observed);
        if(payload.Property("$type")==null)proposal.Remove("$type");
        if(!StoredRecordPayloadEquality.Equivalent(proposal,payload))throw new StorageLifecycleConflictException("Cosmos record changed before conditional deletion.");
        cancellationToken.ThrowIfCancellationRequested();
        using var response=await ObserveMutationAsync(()=>_container.DeleteItemStreamAsync(documentId,partitionKey??PartitionKey.None,new ItemRequestOptions{IfMatchEtag=etag},cancellationToken),documentId);
        if(response.StatusCode!=HttpStatusCode.NoContent)
            throw new CosmosMutationReconciliationException(documentId,new InvalidDataException("Cosmos conditional deletion lacks the native no-content acknowledgement."));
    }
}
