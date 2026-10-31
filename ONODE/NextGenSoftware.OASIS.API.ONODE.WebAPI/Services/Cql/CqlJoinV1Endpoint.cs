using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Endpoint
{
    public static async Task<IActionResult> ExecuteJoinAsync(HttpContext http,CqlJoinV1Request request,ICqlV1Providers providers)
    {
        var rejected=CaptureAdmission(http,out var actor,out var admission);
        if(rejected!=null)return rejected;
        try
        {
            http.RequestAborted.ThrowIfCancellationRequested();
            var requested=CqlV1Compiler.RequestedProvider(request?.Provider);
            var policy=CaptureReadPolicy(http,actor,requested,providers,admission);
            var intent=CqlV1Compiler.CompileJoin(request,actor,policy?.Selected??requested,policy?.Access);
            if(!TryResolveEligible(providers,requested,admission,out var storage,out var rowAdapter,out var selected,policy))
                return Failure(403,"CQL_PROVIDER_NOT_ELIGIBLE","The provider is not eligible for this admitted join.");
            if(rowAdapter is not ICosmicQueryJoinStorageProvider adapter)
                return Failure(400,"CQL_CAPABILITY_UNSUPPORTED","The provider does not implement native joins.");
            var planned=CosmicJoinPlanner.Plan(intent,selected,new[]{
                new CosmicJoinTarget(selected,adapter.QueryCapabilities,adapter.JoinCapabilities)});
            if(planned.IsError)return Failure(400,planned.ErrorCode,"The provider does not support this join.");
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            var manager=providers.CreateManager(storage);
            http.Items[CqlV1ResourceFilter.DispatchedKey]=true;
            var result=await manager.ExecuteJoinQueryAsync(planned.Result,new ICosmicQueryStorageProvider[]{adapter},
                maximumResults:100,maximumPayloadBytes:MaximumResponseBytes,cancellationToken:deadline.Token);
            if(result.IsError)return QueryFailure(http,result.ErrorCode);
            var output=result.Result.Providers.Single();
            if(output.Provider!=selected)return Failure(503,"CQL_PROVIDER_RESULT_INVALID","Join provenance differs from its admitted provider.");
            var columns=intent.Columns.Select(column=>new
            {
                alias=column.Alias,source=column.Source.ToString(),
                valueType=ProjectionWireType(Nullable.GetUnderlyingType(column.ValueType)??column.ValueType),
                nullable=!column.ValueType.IsValueType || Nullable.GetUnderlyingType(column.ValueType)!=null
            }).ToArray();
            var rows=new List<object>();
            foreach(var row in output.Rows)
            {
                deadline.Token.ThrowIfCancellationRequested();
                var values=new object[row.Value.Values.Count];
                for(var index=0;index<values.Length;index++)
                {
                    deadline.Token.ThrowIfCancellationRequested();values[index]=ProjectionWireValue(row.Value.Values[index]);
                }
                rows.Add(new{hasRight=row.HasRight,values});
            }
            deadline.Token.ThrowIfCancellationRequested();
            var response=JsonResponse(new OASISResult<object>(new
            {version="cql-join-v1",provider=selected.ToString(),kind=intent.Join.Kind.ToString(),columns,rows}){IsLoaded=true});
            deadline.Token.ThrowIfCancellationRequested();return response;
        }
        catch(ReadPolicyRejected cause) { return Failure(cause.Status,cause.Code,"The trusted read-access policy did not authorize this request."); }
        catch(ArgumentException) when(!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        {return Failure(400,"CQL_REQUEST_INVALID","Invalid bounded CQL join description.");}
        catch(InvalidOperationException) when(!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        {return Failure(503,"CQL_PROVIDER_UNAVAILABLE","The requested provider is unavailable.");}
        catch(ProjectionWireException){return Failure(503,"CQL_PROVIDER_RESULT_INVALID","A joined scalar has an invalid public wire domain.");}
        catch(ResponseLimitException){return Failure(413,"CQL_RESPONSE_LIMIT","Join response exceeds the public output limit.");}
        catch(OperationCanceledException)
        {return Failure(http.RequestAborted.IsCancellationRequested?499:504,"CQL_QUERY_CANCELLED","The join query was cancelled.");}
        catch(Exception){return Failure(503,"CQL_EXECUTION_FAILED","The join did not complete. Retain the usage operation ID for reconciliation.");}
    }
}
