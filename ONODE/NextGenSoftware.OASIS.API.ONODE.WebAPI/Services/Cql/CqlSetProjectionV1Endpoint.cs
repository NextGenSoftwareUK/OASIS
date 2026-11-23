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
    private static async Task<IActionResult> ExecuteSetProjectionAsync(HttpContext http,CqlSetV1Request request,
        ICqlV1Providers providers,Guid actor,ProviderType selected,CosmicQueryReadAccess access,
        IOASISProvider storage,ICosmicQueryStorageProvider rowAdapter)
    {
        if(rowAdapter is not ICosmicQuerySetProjectionStorageProvider adapter || !adapter.SupportsSetProjection)
            return Failure(400,"CQL_CAPABILITY_UNSUPPORTED","The provider does not implement native late entity-set selection.");
        var intent=CqlV1Compiler.CompileSetProjection(request,actor,selected,access);
        var plan=CosmicSetProjectionPlanner.Plan(intent,selected,
            new CosmicProjectionTarget(selected,adapter.QueryCapabilities,adapter.ProjectionCapabilities),
            adapter.SetQueryCapabilities,new CosmicSetQueryOptions(request.MaximumLeftRows,
                request.MaximumRightRows,MaximumResponseBytes,request.MaximumResults));
        if(plan.IsError)return Failure(400,plan.ErrorCode,"The provider does not support this late entity-set selection.");
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        var manager=providers.CreateManager(storage);
        http.Items[CqlV1ResourceFilter.DispatchedKey]=true;
        var result=await manager.ExecuteSetProjectionQueryAsync(plan.Result,new ICosmicQueryStorageProvider[]{adapter},
            maximumResults:request.MaximumResults,maximumPayloadBytes:MaximumResponseBytes,cancellationToken:deadline.Token);
        if(result.IsError)return QueryFailure(http,result.ErrorCode);
        if(!result.IsLoaded || result.Result==null || result.Result.Providers.Count!=1)
            return Failure(503,"CQL_PROVIDER_RESULT_INVALID","Late entity-set selection did not publish one complete native result.");
        var output=result.Result.Providers.Single();
        if(output.Provider!=selected || intent.Source.Left.ReadAccess?.ActorId!=actor
            || intent.Source.Right.ReadAccess?.ActorId!=actor)
            return Failure(503,"CQL_VISIBILITY_VIOLATION","Late entity-set provenance or actor differs from its admitted scope.");
        var columns=SelectedWireColumns(intent.Selection.Columns);
        var rows=SelectedWireRows(output.Rows,deadline.Token);
        deadline.Token.ThrowIfCancellationRequested();
        var response=JsonResponse(new OASISResult<object>(new{
            version="cql-set-projection-v1",provider=selected.ToString(),kind=intent.Source.Kind.ToString(),
            count=output.Rows.Count,columns,rows}){IsLoaded=true});
        deadline.Token.ThrowIfCancellationRequested();
        return response;
    }
}
