using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;
using Holon=NextGenSoftware.OASIS.API.Core.Holons.Holon;
using DataHelper=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS
{
    private async Task<OASISResult<IHolon>> ReadHolonGraphAsync(Guid? id,string key,bool children,bool recursive,int depth,bool fromProvider,int version,bool synchronous)
    {
        var result=new OASISResult<IHolon>();
        try
        {
            if(id.HasValue ? id.Value==Guid.Empty : !ObjectId.TryParse(key,out _)) throw new ArgumentException("Valid Mongo holon identity is required.");
            if(depth<0 || version<0) throw new ArgumentOutOfRangeException(nameof(depth),"Depth and version cannot be negative.");
            IHolon root;List<IHolon> records;
            if(version>0)
            {
                using var session=children && fromProvider
                    ? synchronous ? Database.MongoClient.StartSession(new ClientSessionOptions{Snapshot=true})
                        :await Database.MongoClient.StartSessionAsync(new ClientSessionOptions{Snapshot=true}).ConfigureAwait(false)
                    :null;
                var nativeFilter=id.HasValue?Builders<NativeHolon>.Filter.Eq(row=>row.HolonId,id.Value):Builders<NativeHolon>.Filter.Eq(row=>row.Id,key);
                var filter=children && fromProvider?FilterDefinition<NativeHolon>.Empty:nativeFilter;
                var rendered=filter.Render(new RenderArgs<NativeHolon>(Database.Holon.DocumentSerializer,Database.Holon.Settings.SerializerRegistry));
                var raw=Database.MongoDB.GetCollection<BsonDocument>(Database.Holon.CollectionNamespace.CollectionName,Database.Holon.Settings);
                var query=session==null?raw.Find(rendered):raw.Find(session,rendered);
                if(!(children && fromProvider))query=query.Limit(2);
                var documents=synchronous?query.ToList():await query.ToListAsync().ConfigureAwait(false);
                var observations=documents.Select(document=>(Document:document,Native:BsonSerializer.Deserialize<NativeHolon>(document))).ToList();
                if(observations.GroupBy(row=>row.Native.HolonId).Any(group=>group.Count()>1))throw new StorageIdentityAmbiguousException();
                var selected=observations.SingleOrDefault(row=>id.HasValue?row.Native.HolonId==id.Value:row.Native.Id==key);
                root=selected.Document==null?null:MongoNativeHolonHistory.Read(selected.Document).SingleOrDefault(row=>row.Version==version);
                records=children && fromProvider?observations.Select(row=>DataHelper.ConvertMongoEntityToOASISHolon(row.Native,false)).ToList():new List<IHolon>();
                if(root!=null){records.RemoveAll(row=>row.Id==root.Id);records.Add(root);}
            }
            else if(children && fromProvider)
            {
                // One native snapshot cursor supplies BOTH root and provider-linked descendants.
                using var session=synchronous ? Database.MongoClient.StartSession(new ClientSessionOptions {Snapshot=true})
                    :await Database.MongoClient.StartSessionAsync(new ClientSessionOptions {Snapshot=true}).ConfigureAwait(false);
                var query=Database.Holon.Find(session,FilterDefinition<NativeHolon>.Empty);
                var rows=synchronous ? query.ToList():await query.ToListAsync().ConfigureAwait(false);
                records=rows.Select(row=>DataHelper.ConvertMongoEntityToOASISHolon(row,false)).ToList();
                if(records.GroupBy(row=>row.Id).Any(group=>group.Count()>1)) throw new StorageIdentityAmbiguousException();
                root=records.SingleOrDefault(row=>(id.HasValue ? row.Id==id.Value:row.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS]==key)
                    && (version==0 || row.Version==version));
            }
            else
            {
                var row=id.HasValue
                    ? synchronous ? _holonRepository.GetHolon(id.Value):await _holonRepository.GetHolonAsync(id.Value).ConfigureAwait(false)
                    : synchronous ? _holonRepository.GetHolon(key):await _holonRepository.GetHolonAsync(key).ConfigureAwait(false);
                root=row==null || version!=0 && row.Version!=version ? null:DataHelper.ConvertMongoEntityToOASISHolon(row,children);
                records=root==null ? new List<IHolon>():new List<IHolon> {root};
            }
            if(root==null) return new OASISResult<IHolon> {ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Holon not found."};
            var project=HolonGraphProjection.Create(records,children,recursive,depth,0,fromProvider,
                row=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(row)));
            result.Result=project(root);result.IsLoaded=true;result.LoadedCount=1;
        }
        catch(Exception cause)
        {
            result.IsError=true;result.Exception=cause;result.Message="Mongo holon graph read failed.";
            if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            else if(cause is InvalidDataException or FormatException) result.ErrorCode=Core.Helpers.StorageLifecycleConflictException.ErrorCode;
            else if(cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }
}
