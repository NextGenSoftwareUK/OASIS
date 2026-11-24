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
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
public static partial class CqlV1Endpoint
{
    private static async Task<IActionResult> ExecuteSetReductionAsync(HttpContext http,CqlSetV1Request request,
        ICqlV1Providers providers,Guid actor,ProviderType selected,CosmicQueryReadAccess access,
        IOASISProvider storage,ICosmicQueryStorageProvider rowAdapter)
    {
        if(rowAdapter is not ICosmicSetCountQueryStorageProvider {SupportsSetCount:true} adapter)
            return Failure(400,"CQL_CAPABILITY_UNSUPPORTED","The provider has no native set reduction contract.");
        var intent=CqlV1Compiler.CompileSetReduction(request,actor,selected,access);
        var planned=CosmicSetQueryPlanner.Plan(intent,selected,
            new CosmicQueryTarget(selected,adapter.QueryCapabilities),adapter.SetQueryCapabilities,
            new CosmicSetQueryOptions(request.MaximumLeftRows,request.MaximumRightRows,MaximumResponseBytes,request.MaximumResults));
        if(planned.IsError)return Failure(400,planned.ErrorCode,"The provider does not support this native set reduction.");
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        var manager=providers.CreateManager(storage);
        http.Items[CqlV1ResourceFilter.DispatchedKey]=true;
        var result=await manager.ExecuteSetCountQueryAsync(planned.Result,new ICosmicQueryStorageProvider[]{adapter},deadline.Token);
        if(result.IsError)return QueryFailure(http,result.ErrorCode);
        var output=result.Result.Providers.Single();
        if(!result.IsLoaded || output.Provider!=selected || intent.Left.ReadAccess?.ActorId!=actor)
            return Failure(503,"CQL_PROVIDER_RESULT_INVALID","Native reduction provenance differs from its admitted caller/provider.");
        deadline.Token.ThrowIfCancellationRequested();
        object value=request.Reduction switch{
            "Count"=>ProjectionWireValue(checked((int)output.Value)),
            "LongCount"=>ProjectionWireValue(output.Value),
            "Any"=>output.Value!=0,
            "All"=>output.Value==0,
            _=>throw new ArgumentException("Unknown admitted set reduction.")};
        var response=JsonResponse(new OASISResult<object>(new{
            version="cql-scalar-v1",provider=selected.ToString(),operation=request.Reduction,
            valueType=request.Reduction=="Count"?"Int32":request.Reduction=="LongCount"?"Int64":"Boolean",value
        }){IsLoaded=true});
        deadline.Token.ThrowIfCancellationRequested();return response;
    }
}
