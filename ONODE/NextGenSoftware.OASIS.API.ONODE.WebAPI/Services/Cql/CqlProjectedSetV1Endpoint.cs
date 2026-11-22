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
    private static async Task<IActionResult> ExecuteProjectedSetAsync(HttpContext http,CqlSetV1Request request,
        ICqlV1Providers providers,Guid actor,ProviderType selected,CosmicQueryReadAccess access,
        IOASISProvider storage,ICosmicQueryStorageProvider rowAdapter)
    {
        if(rowAdapter is not ICosmicQueryProjectedSetStorageProvider adapter)
            return Failure(400,"CQL_CAPABILITY_UNSUPPORTED","The provider does not implement native selected-value sets.");
        var intent=CqlV1Compiler.CompileProjectedSet(request,actor,selected,access);
        var options=new CosmicProjectedSetOptions(new CosmicSetQueryOptions(
            request.MaximumLeftRows,request.MaximumRightRows,MaximumResponseBytes,request.MaximumResults),
            maximumStageRows:32000,maximumProofPayloadBytes:MaximumResponseBytes);
        var plan=CosmicProjectedSetPlanner.Plan(intent,selected,
            new CosmicProjectionCompositionTarget(selected,adapter.QueryCapabilities,
                adapter.ProjectionCapabilities,adapter.CompositionCapabilities),adapter.ProjectedSetCapabilities,options);
        if(plan.IsError)return Failure(400,plan.ErrorCode,"The provider does not support this selected-value set.");
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        var manager=providers.CreateManager(storage);
        http.Items[CqlV1ResourceFilter.DispatchedKey]=true;
        var result=await manager.ExecuteProjectedSetQueryAsync(plan.Result,new ICosmicQueryStorageProvider[]{adapter},
            maximumResults:request.MaximumResults,maximumPayloadBytes:MaximumResponseBytes,cancellationToken:deadline.Token);
        if(result.IsError)return QueryFailure(http,result.ErrorCode);
        if(!result.IsLoaded || result.Result==null || result.Result.Providers.Count!=1)
            return Failure(503,"CQL_PROVIDER_RESULT_INVALID","Selected-value set execution did not publish one acknowledged native result.");
        var output=result.Result.Providers.Single();
        if(output.Provider!=selected || intent.Left.Source.ReadAccess?.ActorId!=actor ||
            intent.Right.Source.ReadAccess?.ActorId!=actor)
            return Failure(503,"CQL_VISIBILITY_VIOLATION","Selected-value set provenance or actor differs from its admitted scope.");
        var columns=SelectedWireColumns(intent.Output.Columns);
        var rows=SelectedWireRows(output.Rows,deadline.Token);
        deadline.Token.ThrowIfCancellationRequested();
        return JsonResponse(new OASISResult<object>(new{
            version="cql-projected-set-v1",provider=selected.ToString(),kind=intent.Kind.ToString(),
            count=output.Rows.Count,columns,rows}){IsLoaded=true});
    }
}
