using MongoDB.Bson;
using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal static partial class MongoNativeRecordWriter
{
    internal static void PrepareAccountHistory<T>(BsonDocument replacement,BsonDocument observed,bool retain) where T:HolonBase
    {
        if(typeof(T)==typeof(Avatar))
            PrepareProjectedAccountHistory(replacement,observed,retain,MongoNativeAccountHistory.ReadAvatar,MongoNativeAccountHistory.StageAvatar);
        else if(typeof(T)==typeof(AvatarDetail))
            PrepareProjectedAccountHistory(replacement,observed,retain,MongoNativeAccountHistory.ReadDetail,MongoNativeAccountHistory.StageDetail);
    }
    private static void PrepareProjectedAccountHistory<T>(BsonDocument replacement,BsonDocument observed,bool retain,
        Func<BsonDocument,IReadOnlyList<T>> read,Action<BsonDocument,BsonDocument> stage) where T:IHolonBase
    {
        if(!retain && !replacement.Contains(MongoNativeHolonHistory.Field))return;
        var before=read(observed);var after=read(replacement);
        var previous=before[before.Count-1];var current=after[after.Count-1];
        if(current.Version<previous.Version)throw new StorageLifecycleConflictException("The account replacement would move history backwards.");
        if(retain)
        {
            // An acknowledged identical save must not grow the history on every retry.
            if(previous.Version==current.Version && StoredRecordPayloadEquality.Equivalent(
                StoredRecordJson.CaptureRuntimeToken(previous),StoredRecordJson.CaptureRuntimeToken(current)))return;
            stage(replacement,observed);
        }
        if(retain)_ = read(replacement);
    }
}
