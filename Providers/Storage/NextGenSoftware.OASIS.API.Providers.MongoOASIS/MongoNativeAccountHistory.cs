using System.Collections.Generic;
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

// Account-specific projections use the same BSON archive envelope and integrity
// owner as holons. No JSON account archive format has previously been defined.
internal static class MongoNativeAccountHistory
{
    private static BsonValue ValidatePhysicalArchive(BsonDocument document)
    {
        if(!document.TryGetValue("_id",out var key) || !key.IsObjectId)
            throw new InvalidDataException("Mongo account native identity is invalid.");
        if(document.TryGetValue(MongoNativeHolonHistory.Field,out var archive))
        {
            if(!archive.IsBsonDocument || !archive.AsBsonDocument.TryGetValue("records",out var records) || !records.IsBsonArray)
                throw new InvalidDataException("Mongo account archive is malformed.");
            foreach(var entry in records.AsBsonArray)
                if(!entry.IsBsonDocument || !entry.AsBsonDocument.TryGetValue("bson",out var snapshot) || !snapshot.IsBsonDocument
                    || !snapshot.AsBsonDocument.TryGetValue("_id",out var archivedKey) || archivedKey!=key)
                    throw new InvalidDataException("Mongo account archive native identity differs from its record.");
        }
        return key;
    }
    private static void ValidateReplacement(BsonDocument replacement,BsonDocument observed)
    {
        if(ValidatePhysicalArchive(replacement)!=ValidatePhysicalArchive(observed))
            throw new InvalidDataException("Mongo account replacement changed its native identity.");
    }
    private static T Require<T>(OASISResult<T> result) where T:IHolonBase
    {
        if(result==null || result.IsError || result.Result is null)
            throw new InvalidDataException("Mongo account history projection failed.",result?.Exception);
        return result.Result;
    }
    private static IAvatar Avatar(BsonDocument document)
        =>Require(DataHelper.ConvertMongoEntityToOASISAvatar(new OASISResult<Entities.Avatar>(BsonSerializer.Deserialize<Entities.Avatar>(document))));
    private static IAvatarDetail Detail(BsonDocument document)
        =>Require(DataHelper.ConvertMongoEntityToOASISAvatarDetail(new OASISResult<Entities.AvatarDetail>(BsonSerializer.Deserialize<Entities.AvatarDetail>(document))));
    private static T UndefinedLegacy<T>(string json)
        =>throw new InvalidDataException("Mongo account history does not define a legacy JSON archive format.");

    internal static IReadOnlyList<IAvatar> ReadAvatar(BsonDocument document)
    {
        ValidatePhysicalArchive(document);
        return MongoNativeHolonHistory.Read(document,Avatar,UndefinedLegacy<IAvatar>);
    }
    internal static IReadOnlyList<IAvatarDetail> ReadDetail(BsonDocument document)
    {
        ValidatePhysicalArchive(document);
        return MongoNativeHolonHistory.Read(document,Detail,UndefinedLegacy<IAvatarDetail>);
    }
    internal static void StageAvatar(BsonDocument replacement,BsonDocument observed)
    {
        ValidateReplacement(replacement,observed);
        MongoNativeHolonHistory.Stage(replacement,observed,Avatar,UndefinedLegacy<IAvatar>);
    }
    internal static void StageDetail(BsonDocument replacement,BsonDocument observed)
    {
        ValidateReplacement(replacement,observed);
        MongoNativeHolonHistory.Stage(replacement,observed,Detail,UndefinedLegacy<IAvatarDetail>);
    }
}
