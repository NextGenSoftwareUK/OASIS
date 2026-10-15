using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Holon=NextGenSoftware.OASIS.API.Core.Holons.Holon;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;
using Repository=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories.HolonRepository;
using DataHelper=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS
{
    private async Task<OASISResult<IEnumerable<IHolon>>> ReadHolonCollectionGraphAsync(HolonType type,Guid? parent,string parentKey,
        Dictionary<string,string> metadata,MetaKeyValuePairMatchMode match,bool children,bool recursive,int depth,int initialDepth,bool fromProvider,int version,bool synchronous)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();IClientSessionHandle session=null;
        try
        {
            if(depth<0 || initialDepth<0 || version<0) throw new ArgumentOutOfRangeException(nameof(depth));
            if(!Enum.IsDefined(typeof(HolonType),type) || !Enum.IsDefined(typeof(MetaKeyValuePairMatchMode),match)) throw new ArgumentException("Unknown Mongo query type or match mode.");
            if(parentKey!=null && !ObjectId.TryParse(parentKey,out _)) throw new ArgumentException("A native Mongo parent key is required.");
            if(metadata!=null && (metadata.Count==0 || metadata.Any(pair=>string.IsNullOrWhiteSpace(pair.Key) || pair.Value==null)))
                throw new ArgumentException("Metadata query requires nonempty keys and nonnull values.");
            if(children && fromProvider)
                session=synchronous ? Database.MongoClient.StartSession(new ClientSessionOptions {Snapshot=true})
                    :await Database.MongoClient.StartSessionAsync(new ClientSessionOptions {Snapshot=true}).ConfigureAwait(false);
            async Task<List<NativeHolon>> Read(FilterDefinition<NativeHolon> filter,int limit=0)
            {
                var query=session==null ? Database.Holon.Find(filter):Database.Holon.Find(session,filter);
                if(limit!=0) query=query.Limit(limit);
                return synchronous ? query.ToList():await query.ToListAsync().ConfigureAwait(false);
            }
            if(parentKey!=null)
            {
                var parents=await Read(Builders<NativeHolon>.Filter.Where(row=>row.Id==parentKey && row.DeletedDate==DateTime.MinValue),2).ConfigureAwait(false);
                if(parents.Count>1) throw new StorageIdentityAmbiguousException();
                if(parents.Count==0) {result.Result=Array.Empty<IHolon>();result.IsLoaded=true;return result;}
                parent=parents[0].HolonId;
            }
            var rootsFilter=Repository.BuildHolonTypeFilter(type);
            if(parent.HasValue) rootsFilter &= Builders<NativeHolon>.Filter.Eq(row=>row.ParentHolonId,parent.Value);
            if(version!=0) rootsFilter &= Builders<NativeHolon>.Filter.Eq(row=>row.Version,version);
            if(metadata!=null) rootsFilter &= Repository.BuildMetaDataMultiFilter(metadata,match);
            var roots=await Read(rootsFilter).ConfigureAwait(false);
            if(roots.Count==0) {result.Result=Array.Empty<IHolon>();result.IsLoaded=true;return result;}
            var rows=children && fromProvider ? await Read(Repository.BuildHolonTypeFilter(HolonType.All)).ConfigureAwait(false):roots;
            var records=rows.Select(row=>DataHelper.ConvertMongoEntityToOASISHolon(row,children && !fromProvider)).ToList();
            if(records.GroupBy(row=>row.Id).Any(group=>group.Count()>1)) throw new StorageIdentityAmbiguousException();
            var byId=records.ToDictionary(row=>row.Id);
            var project=HolonGraphProjection.Create(records,children,recursive,depth,initialDepth,fromProvider,
                row=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(row)));
            result.Result=roots.Select(root=>project(byId[root.HolonId])).ToList();result.IsLoaded=true;
        }
        catch(Exception cause)
        {
            result.IsError=true;result.Exception=cause;result.Message="Mongo holon collection read failed.";
            if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            else if(cause is InvalidDataException) result.ErrorCode=Core.Helpers.StorageLifecycleConflictException.ErrorCode;
            else if(cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
        }
        finally
        {
            try {session?.Dispose();}
            catch(Exception cleanup)
            {
                result.Exception=result.Exception==null ? cleanup:new AggregateException("Mongo graph read and snapshot cleanup failed.",result.Exception,cleanup);
                if(result.IsLoaded) {result.IsWarning=true;result.WarningCount++;result.Message="Mongo graph loaded but snapshot cleanup failed.";}
                else {result.IsError=true;result.Message="Mongo graph read/snapshot cleanup failed.";}
            }
        }
        return result;
    }
}
