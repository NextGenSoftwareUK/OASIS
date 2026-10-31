using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class AvatarRepository
{
    private static void ApplyNativeSet<T>(IMongoCollection<T> collection,BsonDocument body,UpdateDefinition<T> patch)
    {
        var rendered=patch.Render(new RenderArgs<T>(collection.DocumentSerializer,collection.Settings.SerializerRegistry));
        if(!rendered.IsBsonDocument || rendered.AsBsonDocument.ElementCount!=1
            || !rendered.AsBsonDocument.TryGetValue("$set",out var fields) || !fields.IsBsonDocument)
            throw new InvalidDataException("The account lifecycle patch is not a native set operation.");
        foreach(var field in fields.AsBsonDocument)body[field.Name]=field.Value.DeepClone();
    }

    // Snapshot the retention setting once per enclosing transaction. The disabled
    // mode keeps the existing exact-field native patch; neither path is a retry.
    private async Task PatchAccountLifecycleAsync<T>(IClientSessionHandle session,IMongoCollection<T> collection,
        string nativeId,UpdateDefinition<T> patch,bool retainHistory,bool synchronous) where T:HolonBase
    {
        if(!retainHistory)
        {
            var filter=Builders<T>.Filter.Eq(row=>row.Id,nativeId);
            var written=synchronous?collection.UpdateOne(session,filter,patch):await collection.UpdateOneAsync(session,filter,patch).ConfigureAwait(false);
            if(!written.IsAcknowledged || written.MatchedCount!=1)throw new InvalidOperationException("The account lifecycle patch was not acknowledged.");
            return;
        }
        var raw=collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName,collection.Settings);
        var identity=new BsonDocument("_id",ObjectId.Parse(nativeId));
        var query=raw.Find(session,identity);
        var observed=synchronous?query.SingleOrDefault():await query.SingleOrDefaultAsync().ConfigureAwait(false);
        if(observed==null)throw new InvalidDataException("The account lifecycle native identity is absent.");
        var body=observed.DeepClone().AsBsonDocument;ApplyNativeSet(collection,body,patch);
        if(body.Equals(observed))return;
        var prior=BsonSerializer.Deserialize<T>(observed);
        var version=Builders<T>.Update.Set(row=>row.Version,checked(prior.Version+1))
            .Set(row=>row.PreviousVersionId,prior.VersionId).Set(row=>row.VersionId,Guid.NewGuid());
        ApplyNativeSet(collection,body,version);
        MongoNativeRecordWriter.PrepareAccountHistory<T>(body,observed,true);
        var changed=synchronous?raw.ReplaceOne(session,identity,body):await raw.ReplaceOneAsync(session,identity,body).ConfigureAwait(false);
        if(!changed.IsAcknowledged || changed.MatchedCount!=1)throw new InvalidOperationException("The versioned account lifecycle replacement was not acknowledged.");
    }
}
