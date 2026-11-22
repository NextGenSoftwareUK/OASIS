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
    public static async Task<IActionResult> ExecuteSetAsync(HttpContext http,CqlSetV1Request request,ICqlV1Providers providers)
    {
        var rejected=CaptureAdmission(http,out var actor,out var admission);
        if(rejected!=null)return rejected;
        try
        {
            http.RequestAborted.ThrowIfCancellationRequested();
            request=CqlRequestCapture.Capture(request);
            var requested=CqlV1Compiler.RequestedProvider(request?.Provider);
            var policy=CaptureReadPolicy(http,actor,requested,providers,admission);
            var intent=CqlV1Compiler.CompileSet(request,actor,policy?.Selected??requested,policy?.Access);
            if(!TryResolveEligible(providers,requested,admission,out var storage,out var rowAdapter,out var selected,policy))
                return Failure(403,"CQL_PROVIDER_NOT_ELIGIBLE","The provider is not eligible for this admitted set.");
            if(rowAdapter is not ICosmicSetQueryStorageProvider adapter)
                return Failure(400,"CQL_CAPABILITY_UNSUPPORTED","The provider does not implement native sets.");
            var planned=CosmicSetQueryPlanner.Plan(intent,selected,
                new CosmicQueryTarget(selected,adapter.QueryCapabilities),adapter.SetQueryCapabilities,
                new CosmicSetQueryOptions(request.MaximumLeftRows,request.MaximumRightRows,
                    MaximumResponseBytes,request.MaximumResults));
            if(planned.IsError)return Failure(400,planned.ErrorCode,"The provider does not support this set operation.");
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            var manager=providers.CreateManager(storage);
            http.Items[CqlV1ResourceFilter.DispatchedKey]=true;
            var result=await manager.ExecuteSetQueryAsync(planned.Result,new ICosmicQueryStorageProvider[]{adapter},
                maximumResults:request.MaximumResults,cancellationToken:deadline.Token);
            if(result.IsError)return QueryFailure(http,result.ErrorCode);
            var output=result.Result.Providers.Single();
            if(output.Provider!=selected)return Failure(503,"CQL_PROVIDER_RESULT_INVALID","Set provenance differs from its admitted provider.");
            if(!result.IsLoaded || intent.Left.ReadAccess==null || intent.Left.ReadAccess.ActorId!=actor ||
                output.Rows.Any(row=>!intent.Left.ReadAccess.CanRead(row)))
                return Failure(503,"CQL_VISIBILITY_VIOLATION","Native set results did not satisfy the mandatory read policy.");
            deadline.Token.ThrowIfCancellationRequested();
            var response=JsonResponse(new OASISResult<object>(new
            {
                version="cql-set-v1",provider=selected.ToString(),kind=intent.Kind.ToString(),count=output.Rows.Count,
                rows=CqlV1Projection.Create(output.Rows,intent.Left.ReadAccess,request.IncludeMetadata,null,deadline.Token,
                    preserveRootOccurrences:intent.Kind==CosmicSetQueryKind.Concat)
            }){IsLoaded=true});
            deadline.Token.ThrowIfCancellationRequested();return response;
        }
        catch(ReadPolicyRejected cause) { return Failure(cause.Status,cause.Code,"The trusted read-access policy did not authorize this request."); }
        catch(ArgumentException) when(!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        {return Failure(400,"CQL_REQUEST_INVALID","Invalid bounded CQL set description.");}
        catch(InvalidOperationException) when(!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        {return Failure(503,"CQL_PROVIDER_UNAVAILABLE","The requested provider is unavailable.");}
        catch(CqlV1Projection.Rejected cause){return Failure(cause.Code=="CQL_RESPONSE_LIMIT"?413:503,cause.Code,"Native set rows violated the public output contract.");}
        catch(ResponseLimitException){return Failure(413,"CQL_RESPONSE_LIMIT","Set response exceeds the public output limit.");}
        catch(OperationCanceledException)
        {return Failure(http.RequestAborted.IsCancellationRequested?499:504,"CQL_QUERY_CANCELLED","The set query was cancelled.");}
        catch(Exception){return Failure(503,"CQL_EXECUTION_FAILED","The set query did not complete. Retain the usage operation ID for reconciliation.");}
    }
}
