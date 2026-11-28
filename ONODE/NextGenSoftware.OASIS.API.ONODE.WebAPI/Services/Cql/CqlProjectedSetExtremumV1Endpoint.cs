using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
public static partial class CqlV1Endpoint
{
    private static Task<IActionResult> ExecuteProjectedSetExtremumReductionAsync(HttpContext http,CqlSetV1Request request,
        ICqlV1Providers providers,Guid actor,ProviderType selected,CosmicQueryReadAccess access,
        IOASISProvider storage,ICosmicQueryStorageProvider rowAdapter)
    {
        if(rowAdapter is not ICosmicQueryProjectedSetStorageProvider native)
            return Task.FromResult<IActionResult>(Failure(400,"CQL_CAPABILITY_UNSUPPORTED","The provider has no native selected-value set contract."));
        var query=CqlV1Compiler.CompileProjectedSetReduction(request,actor,selected,access);
        var position=query.Output.OutputAliases.IndexOf(request.ReductionColumn);
        if(position<0)throw new ArgumentException("Extrema must name a currently visible selected alias.");
        var column=query.Output.OutputColumnIndices[position];
        return (Task<IActionResult>)typeof(CqlV1Endpoint).GetMethod(nameof(ExecuteProjectedSetExtremumTypedAsync),BindingFlags.NonPublic|BindingFlags.Static)
            .MakeGenericMethod(query.Output.Columns[column].ModelValueType).Invoke(null,new object[]{http,request,providers,actor,selected,storage,native,query,column});
    }
    private static async Task<IActionResult> ExecuteProjectedSetExtremumTypedAsync<TValue>(HttpContext http,CqlSetV1Request request,
        ICqlV1Providers providers,Guid actor,ProviderType selected,IOASISProvider storage,
        ICosmicQueryProjectedSetStorageProvider native,CosmicProjectedSetQuery<Holon,CosmicProjectionRecord> query,int column)
    {
        var operation=request.Reduction=="Min"?CosmicScalarOperation.Min:CosmicScalarOperation.Max;
        var plan=CosmicProjectedSetExtremumPlanner.Plan<Holon,CosmicProjectionRecord,TValue>(query,column,operation,selected,
            new CosmicProjectionCompositionTarget(selected,native.QueryCapabilities,native.ProjectionCapabilities,native.CompositionCapabilities),
            native.ProjectedSetCapabilities,new CosmicProjectedSetOptions(new CosmicSetQueryOptions(
                request.MaximumLeftRows,request.MaximumRightRows,MaximumResponseBytes,request.MaximumResults)));
        if(plan.IsError)return Failure(400,plan.ErrorCode,"The provider does not support this selected native extremum.");
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);deadline.CancelAfter(TimeSpan.FromSeconds(5));
        var manager=providers.CreateManager(storage);http.Items[CqlV1ResourceFilter.DispatchedKey]=true;
        var read=await manager.ExecuteProjectedSetExtremumQueryAsync(plan.Result,new ICosmicQueryStorageProvider[]{native},deadline.Token);
        if(read.IsError)return QueryFailure(http,read.ErrorCode);
        var result=read.Result.Providers.Single();
        if(!read.IsLoaded||result.Provider!=selected||query.Output.Source.ReadAccess?.ActorId!=actor)
            return Failure(503,"CQL_PROVIDER_RESULT_INVALID","Extremum provenance differs from its admitted caller/provider.");
        deadline.Token.ThrowIfCancellationRequested();
        return JsonResponse(new OASISResult<object>(new{version="cql-scalar-v1",provider=selected.ToString(),operation=request.Reduction,
            valueType=ProjectionWireType(Nullable.GetUnderlyingType(typeof(TValue))??typeof(TValue)),value=ProjectionWireValue(result.Value)}){IsLoaded=true});
    }
}
