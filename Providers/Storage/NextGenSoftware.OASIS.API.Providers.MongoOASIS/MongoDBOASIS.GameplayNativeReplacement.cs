using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using AvatarDetail=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS
{
    private async Task ReplaceGameplayAvatarDetailAsync(IClientSessionHandle session,
        IMongoCollection<AvatarDetail> collection,AvatarDetail detail,CancellationToken token)
    {
        // The raw read uses the same transaction snapshot as the original typed read.
        // Typed serialization alone drops all unmapped native fields.
        var raw=collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
        var identity=new BsonDocument("_id",ObjectId.Parse(detail.Id));
        var observed=await raw.Find(session,identity).SingleOrDefaultAsync(token).ConfigureAwait(false);
        if(observed==null || BsonSerializer.Deserialize<AvatarDetail>(observed).HolonId!=detail.HolonId)
            throw new InvalidDataException("The gameplay account's native identity changed.");
        var body=MongoNativeRecordWriter.SerializePayload(collection,detail);
        MongoNativeRecordWriter.RetainUnknownFields<AvatarDetail>(body,observed);
        var replaced=await raw.ReplaceOneAsync(session,identity,body,cancellationToken:token).ConfigureAwait(false);
        if(!replaced.IsAcknowledged || replaced.MatchedCount!=1)
            throw new InvalidDataException("The gameplay account replacement was not acknowledged.");
    }
}
