using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

public partial class AzureCosmosDBOASIS
{
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
