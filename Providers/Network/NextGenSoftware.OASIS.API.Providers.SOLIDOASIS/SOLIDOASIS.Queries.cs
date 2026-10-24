using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

public partial class SOLIDOASIS
{
    private async Task<OASISResult<IHolon>> LoadPrivateHolonGraphAsync(Guid id,bool children,bool recursive,int maxDepth,bool childrenFromProvider,int version)
    {
        var result=new OASISResult<IHolon>();
        try{
            if(maxDepth<0)throw new ArgumentOutOfRangeException(nameof(maxDepth));
            var root=await LoadPrivateRecordAsync<IHolon>(id,version);
            if(root.IsError||!children||!childrenFromProvider)return root;
            // Root selection owns its requested version; children retain independent current versions.
            var loaded=await LoadPrivateRecordsAsync<IHolon>(0);
            if(loaded.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(loaded);
            var project=HolonGraphProjection.Create(loaded.Result.Where(record=>record.DeletedDate==DateTime.MinValue),true,recursive,maxDepth,0,true,
                record=>StoredRecordJson.DeserializeStored<IHolon>(StoredRecordJson.Serialize(record)));
            root.Result=project(root.Result);return root;
        }catch(Exception error){OASISErrorHandling.HandleError(ref result,"SOLID single holon graph read failed.",error);}
        return result;
    }
    private async Task<OASISResult<IHolon>> LoadPrivateHolonGraphByKeyAsync(string key,bool children,bool recursive,int maxDepth,bool childrenFromProvider,int version)
    {
        var result=new OASISResult<IHolon>();
        try{return await LoadPrivateHolonGraphAsync(ParsePrivateRecordKey(key),children,recursive,maxDepth,childrenFromProvider,version);}
        catch(Exception error){OASISErrorHandling.HandleError(ref result,"SOLID single holon key read failed.",error);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> QueryPrivateHolonsAsync(HolonType type,Func<IHolon,bool> filter,
        bool loadChildren,bool recursive,int maxDepth,int initialDepth,bool childrenFromProvider,int version,bool rootsAreChildren=false)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try{
            if(!Enum.IsDefined(typeof(HolonType),type))throw new ArgumentOutOfRangeException(nameof(type));
            if(maxDepth<0||initialDepth<0)throw new ArgumentOutOfRangeException(nameof(maxDepth));
            var loaded=await LoadPrivateRecordsAsync<IHolon>(version);
            if(loaded.IsError)return loaded;
            var rows=loaded.Result.ToArray();
            var pool=loadChildren&&childrenFromProvider&&version>0?await LoadPrivateRecordsAsync<IHolon>(0):loaded;
            if(pool.IsError)return pool;
            var project=HolonGraphProjection.Create(pool.Result.Where(record=>record.DeletedDate==DateTime.MinValue),loadChildren,recursive,maxDepth,initialDepth,childrenFromProvider,
                record=>StoredRecordJson.DeserializeStored<IHolon>(StoredRecordJson.Serialize(record)),rootsAreChildren:rootsAreChildren);
            result.Result=rows.Where(record=>(type==HolonType.All||record.HolonType==type)&&(filter==null||filter(record))).Select(project).ToArray();
            result.IsLoaded=true;
            result.LoadedCount=result.Result.Count();
        }catch(Exception error){OASISErrorHandling.HandleError(ref result,"SOLID private holon query failed.",error);}
        return result;
    }

    private Task<OASISResult<IEnumerable<IHolon>>> QueryPrivateMetadataAsync(string key,string value,HolonType type,
        bool loadChildren,bool recursive,int maxDepth,int initialDepth,bool childrenFromProvider,int version)
        =>QueryPrivateMetadataAsync(string.IsNullOrWhiteSpace(key)?null:new Dictionary<string,string>{{key,value}},MetaKeyValuePairMatchMode.All,
            type,loadChildren,recursive,maxDepth,initialDepth,childrenFromProvider,version);

    private async Task<OASISResult<IEnumerable<IHolon>>> QueryPrivateMetadataAsync(Dictionary<string,string> filter,MetaKeyValuePairMatchMode mode,HolonType type,
        bool loadChildren,bool recursive,int maxDepth,int initialDepth,bool childrenFromProvider,int version)
    {
        try{
            if(filter==null||filter.Count==0||filter.Keys.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Nonblank metadata key/value filters are required.",nameof(filter));
            if(mode!=MetaKeyValuePairMatchMode.All&&mode!=MetaKeyValuePairMatchMode.Any)throw new ArgumentOutOfRangeException(nameof(mode));
            var snapshot=new Dictionary<string,string>(filter,StringComparer.Ordinal);
            return await QueryPrivateHolonsAsync(type,record=>StoredRecordSearch.MatchesMetadata(record.MetaData,snapshot,mode),
                loadChildren,recursive,maxDepth,initialDepth,childrenFromProvider,version);
        }catch(Exception error){
            var result=new OASISResult<IEnumerable<IHolon>>();OASISErrorHandling.HandleError(ref result,"SOLID private metadata query failed.",error);return result;
        }
    }

    private Task<OASISResult<IEnumerable<IHolon>>> QueryPrivateChildrenByKeyAsync(string key,HolonType type,
        bool loadChildren,bool recursive,int maxDepth,int initialDepth,bool childrenFromProvider,int version)
    {
        if(Guid.TryParse(key,out var id))return QueryPrivateHolonsAsync(type,record=>record.ParentHolonId==id,loadChildren,recursive,maxDepth,initialDepth,childrenFromProvider,version,rootsAreChildren:true);
        var result=new OASISResult<IEnumerable<IHolon>>();
        OASISErrorHandling.HandleError(ref result,"SOLID parent provider key must be a GUID.",new ArgumentException("Invalid parent key.",nameof(key)));
        return Task.FromResult(result);
    }
}
