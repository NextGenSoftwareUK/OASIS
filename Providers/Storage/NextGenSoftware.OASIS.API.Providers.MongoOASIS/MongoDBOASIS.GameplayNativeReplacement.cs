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
    private sealed class GameplayAccountVersionOverflowException(System.OverflowException cause)
        :System.OverflowException("The account version cannot be advanced.",cause);

    private async Task ReplaceGameplayAvatarDetailAsync(IClientSessionHandle session,
        IMongoCollection<AvatarDetail> collection,AvatarDetail detail,bool retainHistory,CancellationToken token)
    {
        // The raw read uses the same transaction snapshot as the original typed read.
        // Typed serialization alone drops all unmapped native fields.
        var raw=collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
        var identity=new BsonDocument("_id",ObjectId.Parse(detail.Id));
        var observed=await raw.Find(session,identity).SingleOrDefaultAsync(token).ConfigureAwait(false);
        if(observed==null)
            throw new InvalidDataException("The gameplay account's native identity changed.");
        var prior=BsonSerializer.Deserialize<AvatarDetail>(observed);
        if(prior.HolonId!=detail.HolonId || prior.Version!=detail.Version || prior.VersionId!=detail.VersionId)
            throw new InvalidDataException("The gameplay account's observed version identity changed.");
        // This boundary owns a logical gameplay mutation; ordinary native saves
        // and replica writes continue to preserve their caller-supplied versions.
        int next;
        try{next=checked(prior.Version+1);}
        catch(System.OverflowException cause){throw new GameplayAccountVersionOverflowException(cause);}
        detail.PreviousVersionId=prior.VersionId;
        detail.Version=next;
        detail.VersionId=System.Guid.NewGuid();
        var body=MongoNativeRecordWriter.SerializePayload(collection,detail);
        MongoNativeRecordWriter.RetainUnknownFields<AvatarDetail>(body,observed);
        MongoNativeRecordWriter.PrepareAccountHistory<AvatarDetail>(body,observed,retainHistory);
        var replaced=await raw.ReplaceOneAsync(session,identity,body,cancellationToken:token).ConfigureAwait(false);
        if(!replaced.IsAcknowledged || replaced.MatchedCount!=1)
            throw new InvalidDataException("The gameplay account replacement was not acknowledged.");
    }
}
