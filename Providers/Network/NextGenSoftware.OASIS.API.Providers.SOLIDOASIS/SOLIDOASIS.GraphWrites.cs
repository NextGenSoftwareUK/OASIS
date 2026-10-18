using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

public partial class SOLIDOASIS
{
    private async Task<OASISResult<IHolon>> SaveOnePrivateGraphAsync(IHolon source,bool children,bool recursive,int depth)
    {
        var batch=await SavePrivateGraphAsync(new[]{source},children,recursive,depth,0);
        var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(batch);
        if(!batch.IsError)result.Result=batch.Result.Single();
        return result;
    }
    private async Task<OASISResult<IEnumerable<IHolon>>> SavePrivateGraphAsync(IEnumerable<IHolon> sources,bool children,bool recursive,int depth,int initialDepth)
    {
        var result=new OASISResult<IEnumerable<IHolon>>();var acknowledged=new List<string>();
        try{
            RequirePrivateStorage();
            var plan=HolonGraphWritePlan.Create(sources,children,recursive,depth,initialDepth);
            foreach(var node in plan.Nodes){
                node.Snapshot.ProviderUniqueStorageKey??=new Dictionary<NextGenSoftware.OASIS.API.Core.Enums.ProviderType,string>();
                node.Snapshot.ProviderUniqueStorageKey[Core.Enums.ProviderType.SOLIDOASIS]=node.Snapshot.Id.ToString();
                node.Snapshot.IsNewHolon=false;
            }
            foreach(var node in plan.Nodes){
                var saved=await SavePrivateRecordAsync<IHolon>(node.Snapshot);
                if(saved.IsError){
                    if(acknowledged.Count==0)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,IEnumerable<IHolon>>(saved);
                    throw saved.Exception??new IOException("SOLID graph member write failed without an exception.");
                }
                if(saved.Result==null)throw new InvalidDataException("SOLID graph member write returned no acknowledged record.");
                acknowledged.Add(_privateRecords.Resource<IHolon>(node.Snapshot.Id).AbsoluteUri);
                // Publishing is per verified native record, not an atomic multi-resource commit.
                node.Source.Id=node.Snapshot.Id;node.Source.ParentHolonId=node.Snapshot.ParentHolonId;
                node.Source.ProviderUniqueStorageKey??=new Dictionary<NextGenSoftware.OASIS.API.Core.Enums.ProviderType,string>();
                node.Source.ProviderUniqueStorageKey[Core.Enums.ProviderType.SOLIDOASIS]=node.Snapshot.Id.ToString();
                node.Source.IsNewHolon=false;
            }
            result.Result=plan.Roots;result.IsSaved=true;
        }catch(Exception error){
            if(acknowledged.Count>0)error=new KeyValuePartialMutationException(Core.Enums.ProviderType.SOLIDOASIS,acknowledged,error);
            OASISErrorHandling.HandleError(ref result,"SOLID graph save failed.",error);
        }
        return result;
    }
}
