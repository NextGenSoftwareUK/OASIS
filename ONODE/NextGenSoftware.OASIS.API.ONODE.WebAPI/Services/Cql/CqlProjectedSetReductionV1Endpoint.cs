using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
public static partial class CqlV1Endpoint
{
    private static async Task<IActionResult> ExecuteProjectedSetReductionAsync(HttpContext http,CqlSetV1Request request,
        ICqlV1Providers providers,Guid actor,ProviderType selected,CosmicQueryReadAccess access,
        IOASISProvider storage,ICosmicQueryStorageProvider rowAdapter)
    {
        if(rowAdapter is not ICosmicQueryProjectedSetCountStorageProvider {SupportsProjectedSetCount:true} adapter)
            return Failure(400,"CQL_CAPABILITY_UNSUPPORTED","The provider has no native selected-value count contract.");
        var intent=CqlV1Compiler.CompileProjectedSetReduction(request,actor,selected,access);
        var planned=CosmicProjectedSetPlanner.Plan(intent,selected,
            new CosmicProjectionCompositionTarget(selected,adapter.QueryCapabilities,adapter.ProjectionCapabilities,adapter.CompositionCapabilities),
            adapter.ProjectedSetCapabilities,new CosmicProjectedSetOptions(new CosmicSetQueryOptions(
                request.MaximumLeftRows,request.MaximumRightRows,MaximumResponseBytes,request.MaximumResults)));
        if(planned.IsError)return Failure(400,planned.ErrorCode,"The provider does not support this selected-value reduction.");
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);deadline.CancelAfter(TimeSpan.FromSeconds(5));
        var manager=providers.CreateManager(storage);http.Items[CqlV1ResourceFilter.DispatchedKey]=true;
        var read=await manager.ExecuteProjectedSetCountQueryAsync(planned.Result,new ICosmicQueryStorageProvider[]{adapter},deadline.Token);
        if(read.IsError)return QueryFailure(http,read.ErrorCode);
        var result=read.Result.Providers.Single();
        if(!read.IsLoaded||result.Provider!=selected||intent.Output.Source.ReadAccess?.ActorId!=actor)
            return Failure(503,"CQL_PROVIDER_RESULT_INVALID","Selected reduction provenance differs from its admitted caller/provider.");
        deadline.Token.ThrowIfCancellationRequested();return SetReductionResponse(http,request,selected,result.Value,deadline.Token);
    }
}
