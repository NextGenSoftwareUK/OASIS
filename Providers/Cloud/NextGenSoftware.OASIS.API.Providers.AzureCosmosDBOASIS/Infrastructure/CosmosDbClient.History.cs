using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

public partial class CosmosDbClient
{
    private enum RecordWriteMode { Create, Upsert, Replace, ConditionalReplace }

    public Task<string> UpsertVersionedDocumentAsync<T>(T proposed,Func<string,T> decode,
        PartitionKey partitionKey,CancellationToken token=default) where T:class,IHolonBase
    {
        ArgumentNullException.ThrowIfNull(proposed); ArgumentNullException.ThrowIfNull(decode);
        if(proposed.Id==Guid.Empty || !partitionKey.Equals(new PartitionKey(proposed.Id.ToString())))
            throw new ArgumentException("Versioned Cosmos storage requires matching explicit OASIS identity and /id partition.");
        return WriteCanonicalRecordAsync(proposed.Id.ToString(),proposed,partitionKey,RecordWriteMode.Upsert,
            null,true,text=>decode(text),token);
    }

    // One owner for all record writes. Persisted format determines archive retention even if
    // configuration is later disabled; it is not a retry/fallback after a failed operation.
    private async Task<string> WriteCanonicalRecordAsync(string id,object document,PartitionKey partition,
        RecordWriteMode mode,object expected,bool forceHistory,Func<string,IHolonBase> decode,CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(document);
        token.ThrowIfCancellationRequested();
        var staged=JObject.Parse(OasisJson.Serialize(document));
        if(string.IsNullOrWhiteSpace(id) || staged["Id"]?.ToString()!=id)
            throw new ArgumentException("Document payload identity differs from its native item key.");
        staged["id"]=id;
        var expectedPayload=expected==null ? null:JObject.Parse(OasisJson.Serialize(expected));
        var inputType=document.GetType(); // Trusted caller input type, never a type name taken from stored history.
        JObject previous=null; string etag=null;
        if(mode!=RecordWriteMode.Create)
        {
            var json=await ReadDocumentAsync(id,partition,token).ConfigureAwait(false);
            if(json!=null)
            {
                previous=JObject.Parse(json);
                etag=previous["_etag"]?.Type==JTokenType.String ? previous["_etag"].Value<string>() : null;
                if(string.IsNullOrWhiteSpace(etag))throw new InvalidDataException("Cosmos replacement requires an observed native ETag.");
            }
            else if(mode==RecordWriteMode.ConditionalReplace)
                throw new Core.Helpers.StorageLifecycleConflictException("Cosmos record disappeared before conditional replacement.");
            else if(mode!=RecordWriteMode.Upsert)
                throw new CosmosException("Cosmos replacement target does not exist.",HttpStatusCode.NotFound,0,null,0);
        }
        if(expectedPayload!=null && (previous==null ||
            !StoredRecordPayloadEquality.Equivalent(expectedPayload,CosmosEmbeddedHistory.CanonicalPayload(previous))))
            throw new Core.Helpers.StorageLifecycleConflictException("Cosmos record changed before conditional replacement.");

        var retainsHistory=forceHistory || _retainRecordHistory || previous?[CosmosEmbeddedHistory.PropertyName]!=null;
        if(retainsHistory)
        {
            if(document is not IHolonBase || !partition.Equals(new PartitionKey(id)))
                throw new ArgumentException("History requires a canonical OASIS record and matching /id partition.");
            decode ??= text=>(IHolonBase)OasisJson.Deserialize(text,inputType);
            var frozen=decode(CosmosEmbeddedHistory.CanonicalPayload(staged).ToString(Formatting.None));
            if(frozen==null || frozen.Id.ToString()!=id)throw new InvalidDataException("Canonical history proposal conflicts with its item identity.");
            staged=await CosmosEmbeddedHistory.StageAsync(frozen,previous,decode).ConfigureAwait(false);
        }
        using var stream=new MemoryStream(Encoding.UTF8.GetBytes(staged.ToString(Formatting.None)));
        token.ThrowIfCancellationRequested();
        using var response=await ObserveMutationAsync(()=>previous==null
            ? _container.CreateItemStreamAsync(stream,partition,null,token)
            : _container.ReplaceItemStreamAsync(stream,id,partition,new ItemRequestOptions{IfMatchEtag=etag},token),id).ConfigureAwait(false);
        var acknowledged=await ReadAcknowledgedPayloadAsync(response,id).ConfigureAwait(false);
        try
        {
            var observed=JObject.Parse(acknowledged);
            foreach(var native in new[]{"_etag","_rid","_self","_attachments","_ts"})observed.Remove(native);
            if(!StoredRecordPayloadEquality.Equivalent(staged,observed))
                throw new InvalidDataException("Acknowledged Cosmos current/history document differs from the staged atomic write.");
        }
        catch(Exception cause){throw new CosmosMutationReconciliationException(id,cause is JsonException
            ? new InvalidDataException("Acknowledged Cosmos document JSON is invalid.",cause):cause,acknowledged:true);}
        return acknowledged;
    }
}
