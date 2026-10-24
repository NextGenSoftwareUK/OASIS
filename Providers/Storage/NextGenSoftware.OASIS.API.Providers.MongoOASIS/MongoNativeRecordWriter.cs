using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal static class MongoNativeRecordWriter
{
    internal static async Task<OASISResult<T>> SaveAsync<T>(IMongoCollection<T> records,T holon,bool updating,bool synchronous,bool preserveChildren=false) where T:HolonBase
    {
        var entered=false;var validatingPayload=false;
        try
        {
            if(holon==null) throw new ArgumentNullException(nameof(holon));
            if(holon.HolonId==Guid.Empty)
            {
                if(updating) throw new ArgumentException("A public record identity is required for update.");
                holon.HolonId=Guid.NewGuid();
            }
            BsonDocument observed=null;
            BsonDocument expected=null;
            var raw=records.Database.GetCollection<BsonDocument>(records.CollectionNamespace.CollectionName,records.Settings);
            if(updating)
            {
                // Compare exact persisted BSON, not a reserialized domain object. Nested
                // avatar state can normalize during mapping and cause a false CAS conflict.
                var lookup=Builders<T>.Filter.Where(row=>row.HolonId==holon.HolonId && row.DeletedDate==DateTime.MinValue);
                var rendered=lookup.Render(new RenderArgs<T>(records.DocumentSerializer,records.Settings.SerializerRegistry));
                var query=raw.Find(rendered).Limit(2);
                var originals=synchronous ? query.ToList():await query.ToListAsync().ConfigureAwait(false);
                if(originals.Count>1) throw new StorageIdentityAmbiguousException();
                var original=originals.SingleOrDefault();
                if(original==null) return new OASISResult<T> {IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Holon not found for update."};
                if(!original.TryGetValue("_id",out var nativeId) || !nativeId.IsObjectId)
                    throw new StorageLifecycleConflictException("Mongo record has an invalid physical identity.");
                var originalId=nativeId.AsObjectId.ToString();
                if(!string.IsNullOrEmpty(holon.Id) && holon.Id!=originalId)
                {
                    var conflict=new StorageLifecycleConflictException("Mongo record physical identity changed before update.");
                    return new OASISResult<T> {IsError=true,ErrorCode=StorageLifecycleConflictException.ErrorCode,Exception=conflict,Message=conflict.Message};
                }
                holon.Id=originalId;
                var creation=BsonSerializer.Deserialize<T>(original);
                holon.CreatedProviderType=creation.CreatedProviderType;
                holon.CreatedOASISType=creation.CreatedOASISType;
                observed=original;
                // Whole-document equality also detects newly added fields. $literal keeps
                // arbitrary metadata/unknown field values out of Mongo expression syntax.
                expected=new BsonDocument {{"_id",nativeId},{"$expr",new BsonDocument("$eq",new BsonArray {
                    "$$ROOT",new BsonDocument("$literal",observed)})}};
            }
            if(!updating && holon.CreatedProviderType==null) holon.CreatedProviderType=new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);
            PrepareIdentity(holon);
            if (typeof(T) == typeof(Holon)) RotateHolonRevision((Holon)(object)holon);
            // Complete serialization before dispatch for inserts AND replacements. The
            // exact prepared BSON is what the SDK writes; no second serialization pass.
            validatingPayload=true;
            var replacement=SerializePayload(records,holon);
            validatingPayload=false;
            if(updating)
            {
                RetainUnknownFields<T>(replacement,observed);
                if (preserveChildren)
                {
                    // Excluded graph membership must survive the same BSON CAS as the root write.
                    var children = BsonClassMap.LookupClassMap(typeof(T)).AllMemberMaps.Single(member => member.MemberName == nameof(HolonBase.Children)).ElementName;
                    replacement.Remove(children);
                    if (observed.TryGetValue(children, out var retained))
                        replacement.Add(children, retained.DeepClone());
                }
            }
            var writer=raw.WithWriteConcern(WriteConcern.WMajority.With(journal:true));
            if(updating && typeof(T)==typeof(Holon))MongoNativeHolonHistory.Stage(replacement,observed);
            entered=true;
            if(updating)
            {
                var replaced=synchronous ? writer.ReplaceOne(expected,replacement):await writer.ReplaceOneAsync(expected,replacement).ConfigureAwait(false);
                if(!replaced.IsAcknowledged) throw new InvalidDataException("Mongo record update was not acknowledged.");
                if(replaced.MatchedCount==0)
                {
                    var conflict=new StorageLifecycleConflictException("Mongo record changed after observation; no replacement was written.");
                    return new OASISResult<T> {IsError=true,ErrorCode=StorageLifecycleConflictException.ErrorCode,Exception=conflict,Message=conflict.Message};
                }
            }
            else if(synchronous) writer.InsertOne(replacement);
            else await writer.InsertOneAsync(replacement).ConfigureAwait(false);
            return new OASISResult<T> {Result=holon,IsSaved=true,SavedCount=1};
        }
        catch(Exception cause)
        {
            // BadValue is an authoritative server rejection before the single write executes.
            // All other post-dispatch failures remain conservative until native reconciliation.
            var rejected=cause is MongoCommandException command && command.Code==2;
            var failure=entered && !rejected ? new MongoMutationReconciliationException(holon?.HolonId ?? Guid.Empty,cause):cause;
            return new OASISResult<T> {IsError=true,Exception=failure,
                ErrorCode=cause is StorageLifecycleConflictException ? StorageLifecycleConflictException.ErrorCode
                    :cause is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity
                    :!entered && (cause is ArgumentException || validatingPayload && cause is BsonSerializationException) ? "HYPERDRIVE_PAYLOAD_INVALID":null,
                Message="Mongo record save failed."};
        }
    }
    internal static void RotateHolonRevision(Holon record)
    {
        record.ProviderRevisionId = Guid.NewGuid();
        record.ProviderMetaData ??= new Dictionary<ProviderType,Dictionary<string,string>>();
        if (!record.ProviderMetaData.TryGetValue(ProviderType.MongoDBOASIS, out var own) || own == null)
            record.ProviderMetaData[ProviderType.MongoDBOASIS] = own = new Dictionary<string,string>();
        own[Core.Holons.CosmicConcurrency.ProviderRevisionMetadataKey] = record.ProviderRevisionId.ToString("D");
    }

    internal static BsonDocument SerializePayload<T>(IMongoCollection<T> collection,T record) where T:HolonBase
    {
        var body=record.ToBsonDocument(collection.DocumentSerializer);
        if(record is Holon holon)
            body[MongoCqlMembership.Field]=MongoCqlMembership.Build(holon.HolonId,holon.ChildRoleManifestJson);
        return body;
    }

    internal static BsonDocument PrepareHolonMutation(IMongoCollection<Holon> collection,Holon record,BsonDocument observed)
    {
        PrepareIdentity(record);
        RotateHolonRevision(record);
        var body=SerializePayload(collection,record);
        if(observed!=null)
        {
            RetainUnknownFields<Holon>(body,observed);
            MongoNativeHolonHistory.Stage(body,observed);
        }
        return body;
    }

    internal static void RetainUnknownFields<T>(BsonDocument replacement,BsonDocument observed) where T:HolonBase
    {
        var mapped=new HashSet<string>(BsonClassMap.LookupClassMap(typeof(T)).AllMemberMaps.Select(member=>member.ElementName),StringComparer.Ordinal);
        mapped.Add("_t");
        // Only unknown top-level fields merge. Known fields retain replacement semantics.
        foreach(var field in observed)
            if(!mapped.Contains(field.Name) && !replacement.Contains(field.Name)) replacement.Add(field.Name,field.Value.DeepClone());
    }
    internal static void PrepareIdentity<T>(T record) where T:HolonBase
    {
        if(string.IsNullOrEmpty(record.Id)) record.Id=ObjectId.GenerateNewId().ToString();
        if(!ObjectId.TryParse(record.Id,out _)) throw new ArgumentException("Mongo native record ID must be an ObjectId.");
        record.ProviderUniqueStorageKey ??= new Dictionary<ProviderType,string>();
        record.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]=record.Id;
    }

}
