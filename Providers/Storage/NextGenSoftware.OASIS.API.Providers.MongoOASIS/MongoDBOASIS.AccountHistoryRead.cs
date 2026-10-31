using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NativeAvatar=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using NativeDetail=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS
{
    private enum AccountHistorySelector {Id,Username,Email,ProviderKey,All}

    private static OASISResult<T> AccountHistoryFailure<T>(Exception cause)=>new()
    {
        IsError=true,ErrorCount=1,Exception=cause,Message="Mongo account history read failed.",
        ErrorCode=cause is ArgumentException?"HYPERDRIVE_PAYLOAD_INVALID":
            cause is StorageIdentityAmbiguousException?OASISResultErrorCodes.AmbiguousIdentity:
            cause is InvalidDataException || cause is FormatException || cause is BsonSerializationException
                ?StorageLifecycleConflictException.ErrorCode:"MONGO_HISTORY_READ_FAILED"
    };

    private async Task<OASISResult<IEnumerable<T>>> ReadAccountHistoryRowsAsync<TNative,T>(int version,
        AccountHistorySelector selector,Guid id,string value,FilterDefinition<TNative> identity,
        Func<BsonDocument,IReadOnlyList<T>> decode,Func<T,bool> matches,bool synchronous)
        where T:IHolonBase
    {
        try
        {
            if(version<1)throw new ArgumentOutOfRangeException(nameof(version));
            if(selector==AccountHistorySelector.Id && id==Guid.Empty)throw new ArgumentException("A public account identity is required.");
            if(selector is AccountHistorySelector.Username or AccountHistorySelector.Email && string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("An account selector is required.");
            if(selector==AccountHistorySelector.ProviderKey && !ObjectId.TryParse(value,out _))throw new ArgumentException("A native account key is required.");
            if(!IsProviderActivated || Database?.MongoDB==null)throw new InvalidOperationException("Mongo provider is not activated.");
            var typed=Database.MongoDB.GetCollection<TNative>(typeof(TNative).Name);
            var filter=identity.Render(new RenderArgs<TNative>(typed.DocumentSerializer,typed.Settings.SerializerRegistry));
            if(selector is AccountHistorySelector.Username or AccountHistorySelector.Email)
            {
                // These filters are SDK-rendered single-field equality expressions.
                // Match renamed historical aliases too, not only the current account.
                if(filter.ElementCount!=1 || filter.GetElement(0).Name.StartsWith("$",StringComparison.Ordinal))
                    throw new InvalidDataException("The native account alias filter is inconsistent.");
                var field=filter.GetElement(0);
                filter=new BsonDocument("$or",new BsonArray {filter,
                    new BsonDocument(MongoNativeHolonHistory.Field+".records.bson."+field.Name,field.Value)});
            }
            var raw=Database.MongoDB.GetCollection<BsonDocument>(typed.CollectionNamespace.CollectionName,typed.Settings);
            using var cursor=synchronous?raw.FindSync(filter):await raw.FindAsync(filter).ConfigureAwait(false);
            var selected=new List<T>();var identities=new HashSet<Guid>();
            while(synchronous?cursor.MoveNext():await cursor.MoveNextAsync().ConfigureAwait(false))
                foreach(var document in cursor.Current)
                {
                    var history=decode(document);
                    if(history.Count==0)throw new InvalidDataException("Mongo account history is empty.");
                    if(!identities.Add(history[0].Id))throw new StorageIdentityAmbiguousException();
                    var record=history.SingleOrDefault(row=>row.Version==version);
                    if(record is not null && matches(record))selected.Add(record);
                }
            return new OASISResult<IEnumerable<T>>(selected){IsLoaded=true,LoadedCount=selected.Count};
        }
        catch(Exception cause){return AccountHistoryFailure<IEnumerable<T>>(cause);}
    }

    private Task<OASISResult<IEnumerable<IAvatar>>> ReadHistoricalAvatarRowsAsync(int version,AccountHistorySelector selector,
        Guid id=default,string value=null,bool synchronous=false)
    {
        var filters=Builders<NativeAvatar>.Filter;
        var identity=selector switch {AccountHistorySelector.Id=>filters.Eq(row=>row.HolonId,id),
            AccountHistorySelector.Username=>filters.Eq(row=>row.Username,value),
            AccountHistorySelector.Email=>filters.Eq(row=>row.Email,value),
            AccountHistorySelector.ProviderKey=>filters.Eq(row=>row.Id,value),_=>filters.Empty};
        return ReadAccountHistoryRowsAsync(version,selector,id,value,identity,MongoNativeAccountHistory.ReadAvatar,
            row=>selector==AccountHistorySelector.Username?row.Username==value:selector!=AccountHistorySelector.Email || row.Email==value,synchronous);
    }

    private Task<OASISResult<IEnumerable<IAvatarDetail>>> ReadHistoricalDetailRowsAsync(int version,AccountHistorySelector selector,
        Guid id=default,string value=null,bool synchronous=false)
    {
        var filters=Builders<NativeDetail>.Filter;
        var identity=selector switch {AccountHistorySelector.Id=>filters.Eq(row=>row.HolonId,id),
            AccountHistorySelector.Username=>filters.Eq(row=>row.Username,value),
            AccountHistorySelector.Email=>filters.Eq(row=>row.Email,value),_=>filters.Empty};
        return ReadAccountHistoryRowsAsync(version,selector,id,value,identity,MongoNativeAccountHistory.ReadDetail,
            row=>selector==AccountHistorySelector.Username?row.Username==value:selector!=AccountHistorySelector.Email || row.Email==value,synchronous);
    }

    private static OASISResult<T> SelectHistoricalAccount<T>(OASISResult<IEnumerable<T>> rows)
    {
        if(rows.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>,T>(rows);
        var selected=rows.Result.ToList();
        if(selected.Count>1)return AccountHistoryFailure<T>(new StorageIdentityAmbiguousException());
        return new OASISResult<T> {Result=selected.SingleOrDefault(),IsLoaded=selected.Count==1,LoadedCount=selected.Count,
            ErrorCode=selected.Count==0?OASISResultErrorCodes.RecordNotFound:null};
    }

    private async Task<OASISResult<IAvatar>> ReadHistoricalAvatarAsync(int version,AccountHistorySelector selector,
        Guid id=default,string value=null,bool synchronous=false)
        =>SelectHistoricalAccount(await ReadHistoricalAvatarRowsAsync(version,selector,id,value,synchronous).ConfigureAwait(false));
    private async Task<OASISResult<IAvatarDetail>> ReadHistoricalDetailAsync(int version,AccountHistorySelector selector,
        Guid id=default,string value=null,bool synchronous=false)
        =>SelectHistoricalAccount(await ReadHistoricalDetailRowsAsync(version,selector,id,value,synchronous).ConfigureAwait(false));
}
