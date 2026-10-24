using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

// The enclosing record's BSON CAS/transaction commits this archive with its replacement.
// Versions remain caller-owned; provider revision IDs are not logical version numbers.
internal static class MongoNativeHolonHistory
{
    internal const string Field="OasisHolonRevisionArchive";

    private static IHolon Current(BsonDocument document)=>Helpers.DataHelper.ConvertMongoEntityToOASISHolon(BsonSerializer.Deserialize<NativeHolon>(document),true);

    internal static IReadOnlyList<IHolon> Read(BsonDocument document,bool requireUniqueLogicalVersions=true)
    {
        var current=Current(document);
        var records=new List<IHolon>();
        if(document.TryGetValue(Field,out var value))
        {
            if(!value.IsBsonDocument || value.AsBsonDocument.ElementCount!=2
                || !value.AsBsonDocument.TryGetValue("schema",out var schema) || !schema.IsInt32 || schema.AsInt32!=1
                || !value.AsBsonDocument.TryGetValue("records",out var entries) || !entries.IsBsonArray)
                throw new InvalidDataException("Malformed Mongo holon revision archive.");
            foreach(var entry in entries.AsBsonArray)
            {
                if(!entry.IsBsonDocument || entry.AsBsonDocument.ElementCount!=2
                    || !entry.AsBsonDocument.TryGetValue("json",out var json) || !json.IsString
                    || !entry.AsBsonDocument.TryGetValue("sha256",out var digest) || !digest.IsString
                    || Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json.AsString)))!=digest.AsString)
                    throw new InvalidDataException("Mongo archived holon checksum or envelope is invalid.");
                var record=StoredRecordJson.DeserializeStoredHolon(json.AsString);
                if(record==null || record.Id!=current.Id || record.Version<0)
                    throw new InvalidDataException("Mongo archived holon identity/version differs from its record.");
                records.Add(record);
            }
        }
        records.Add(current);
        if(!requireUniqueLogicalVersions)return records;
        var distinct=new List<IHolon>();
        foreach(var group in records.GroupBy(record=>record.Version))
        {
            var first=group.First();
            foreach(var candidate in group.Skip(1))
                if(!StoredRecordPayloadEquality.Equivalent(StoredRecordJson.CaptureRuntimeToken(first),StoredRecordJson.CaptureRuntimeToken(candidate)))
                    throw new InvalidDataException("The logical Mongo version identifies multiple native revisions.");
            distinct.Add(first);
        }
        return distinct.OrderBy(record=>record.Version).ToArray();
    }

    internal static void Stage(BsonDocument replacement,BsonDocument observed)
    {
        // Validate existing evidence before allowing any replacement to overwrite it.
        var records=Read(observed,requireUniqueLogicalVersions:false);
        if(Current(replacement).Id!=records[0].Id)
            throw new InvalidDataException("Mongo replacement changed the archived public identity.");
        replacement[Field]=new BsonDocument{{"schema",1},{"records",new BsonArray(records.Select(record=>{
            var json=StoredRecordJson.Serialize<IHolon>(record);
            return new BsonDocument{{"json",json},{"sha256",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))}};
        }))}};
    }
}
