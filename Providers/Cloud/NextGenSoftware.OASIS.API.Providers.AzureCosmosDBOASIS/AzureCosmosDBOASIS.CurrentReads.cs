using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

public partial class AzureCosmosDBOASIS
{
    private async Task<OASISResult<IEnumerable<IHolon>>> ReadCurrentParentAsync(string key,HolonType type,bool children,bool recursive,int depth,int initialDepth,bool fromProvider,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(!Guid.TryParse(key,out var id) || id==Guid.Empty) throw new ArgumentException("A nonempty Cosmos parent GUID key is required.",nameof(key));
            if(depth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(depth));
            var loaded=await ReadCurrentListAsync<IHolon>(()=>holonRepository,version);
            if(loaded.IsError) return loaded;
            var records=loaded.Result.ToList();
            var project=HolonGraphProjection.Create(records,children,recursive,depth,initialDepth,fromProvider,
                source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
            result.Result=records.Where(row=>row.ParentHolonId==id && (type==HolonType.All || row.HolonType==type)).Select(project).ToArray();
            result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ReadCurrentHolonsAsync(HolonType type,bool children,bool recursive,int depth,int initialDepth,bool fromProvider,int version)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if(depth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(depth));
            var loaded=await ReadCurrentListAsync<IHolon>(()=>holonRepository,version);
            if(loaded.IsError) return loaded;
            var records=loaded.Result.ToList();
            var project=HolonGraphProjection.Create(records,children,recursive,depth,initialDepth,fromProvider,
                source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
            result.Result=records.Where(row=>type==HolonType.All || row.HolonType==type).Select(project).ToArray();
            result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IHolon>> ReadCurrentHolonAsync(Guid id,bool children,bool recursive,int depth,bool fromProvider,int version)
    {
        var result=await ReadCurrentIdAsync<IHolon>(()=>holonRepository,id,version,depth);
        if(result.IsError) return result;
        try
        {
            IEnumerable<IHolon> records=new[] {result.Result};
            if(children && fromProvider)
            {
                var loaded=await ReadCurrentListAsync<IHolon>(()=>holonRepository,version);
                if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(loaded);
                records=loaded.Result;
            }
            var project=HolonGraphProjection.Create(records,children,recursive,depth,0,fromProvider,
                source=>OasisJson.Deserialize<Holon>(OasisJson.Serialize(source)));
            result.Result=project(result.Result);
        }
        catch(Exception cause) {result.Result=null;result.IsLoaded=false;OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private Task<OASISResult<IHolon>> ReadCurrentHolonKeyAsync(string key,bool children,bool recursive,int depth,bool fromProvider,int version)
    {
        if(Guid.TryParse(key,out var id) && id!=Guid.Empty) return ReadCurrentHolonAsync(id,children,recursive,depth,fromProvider,version);
        var result=new OASISResult<IHolon>();var cause=new ArgumentException("A nonempty Cosmos OASIS GUID key is required.",nameof(key));
        OASISErrorHandling.HandleError(ref result,cause.Message,cause);return Task.FromResult(result);
    }

    private async Task<OASISResult<IEnumerable<T>>> ReadCurrentListAsync<T>(Func<IRepository<T>> repository,int version) where T:IHolonBase
    {
        var result=new OASISResult<IEnumerable<T>>();
        try
        {
            if(version!=0) throw new NotSupportedException("Cosmos current-record storage does not implement historical reads.");
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IEnumerable<T>>(activation);
                if(!activation.Result) throw new InvalidOperationException("Cosmos activation did not acknowledge readiness.");
            }
            var store=repository() ?? throw new InvalidOperationException("Cosmos repository was not initialized.");
            var rows=store.GetList() ?? throw new System.IO.InvalidDataException("Cosmos collection read returned no collection.");
            result.Result=rows.Where(row=>row.IsActive).ToArray();
            result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<T>> SaveCurrentRecordAsync<T>(T record,Func<IRepository<T>> repository) where T:IHolonBase
    {
        var result=new OASISResult<T>();
        try
        {
            ArgumentNullException.ThrowIfNull(record);
            NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(record);
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,T>(activation);
                if(!activation.Result) throw new InvalidOperationException("Cosmos activation did not acknowledge readiness.");
            }
            var store=repository() ?? throw new InvalidOperationException("Cosmos repository was not initialized.");
            var saved=await store.UpsertAsync(record);
            if(saved==null) throw new CosmosMutationReconciliationException(record.Id.ToString(),new System.IO.InvalidDataException("Cosmos save returned no acknowledged record."));
            result.Result=saved;result.IsSaved=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private Task<OASISResult<T>> ReadCurrentKeyAsync<T>(Func<IRepository<T>> repository,string key,int version,int depth=0) where T:IHolonBase
    {
        if(Guid.TryParse(key,out var id) && id!=Guid.Empty) return ReadCurrentIdAsync(repository,id,version,depth);
        var result=new OASISResult<T>();var cause=new ArgumentException("A nonempty Cosmos OASIS GUID key is required.",nameof(key));
        OASISErrorHandling.HandleError(ref result,cause.Message,cause);return Task.FromResult(result);
    }

    private async Task<OASISResult<T>> ReadCurrentIdAsync<T>(Func<IRepository<T>> repository,Guid id,int version,int depth=0) where T:IHolonBase
    {
        var result=new OASISResult<T>();
        try
        {
            if(id==Guid.Empty) throw new ArgumentException("A nonempty Cosmos OASIS GUID identity is required.",nameof(id));
            if(version!=0) throw new NotSupportedException("Cosmos current-record storage does not implement historical reads.");
            if(depth<0) throw new ArgumentOutOfRangeException(nameof(depth));
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,T>(activation);
                if(!activation.Result) throw new InvalidOperationException("Cosmos activation did not acknowledge readiness.");
            }
            var row=await repository().GetByIdAsync(id.ToString());
            if(row==null || !row.IsActive)
            {
                result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Cosmos OASIS record not found.";
            }
            else {result.Result=row;result.IsLoaded=true;}
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }
}
