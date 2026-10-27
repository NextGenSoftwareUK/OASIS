using System;
using System.Linq;
using System.Globalization;
using NextGenSoftware.OASIS.API.Core.Queries;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Endpoint
{
    public static async Task<IActionResult> ExecuteIndexedAsync(HttpContext http,CqlIndexedV1Request request)
    {
        try
        {
            var rejected=CaptureAdmission(http,out var actor,out var admission);
            if(rejected!=null)return rejected;
            if(!CqlIndexedV1RequestContext.TryConsume(http,admission,out var prepared))
                return Failure(503,"CQL_INDEX_PREPARATION_REQUIRED","Bind this request's frozen indexed preparation, reservation and exact tariff policy before dispatch.");
            if(prepared.Actor!=actor||!prepared.Matches(request))
                return Failure(409,"CQL_INDEX_INTENT_MISMATCH","The dispatched intent differs from the reserved indexed request.");
            if(prepared.Binding.Manager.OASISDNA?.OASIS?.HyperDriveMode!=HyperDriveModes.V2)
                return Failure(503,"CQL_V2_REQUIRED","Indexed queries require HyperDrive V2.");
            http.RequestAborted.ThrowIfCancellationRequested();
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            http.Items[CqlV1ResourceFilter.DispatchedKey]=true;
            var result=await prepared.Binding.Manager.ExecuteIndexedQueryAsync(prepared.Plan,prepared.Binding.Reader,
                prepared.Binding.Observer,prepared.MaximumResults,deadline.Token);
            if(result.IsError)
            {
                if(!admission.ProviderExecutionPossible)http.Items.Remove(CqlV1ResourceFilter.DispatchedKey);
                return QueryFailure(http,result.ErrorCode);
            }
            var page=result.Result;
            var rows=CqlV1Projection.Create(page.Rows,prepared.Plan.Query.ReadAccess,prepared.IncludeMetadata,null,deadline.Token);
            var response=new OASISResult<object>(new{
                mode="indexed",assurance="trusted-source-observation",
                provider=page.Checkpoint.Source.SourceProvider.ToString(),
                executionProvider=page.Checkpoint.Source.ExecutionProvider.ToString(),
                count=page.Rows.Count,rows,
                checkpoint=new{source=new{
                    sourceProvider=page.Checkpoint.Source.SourceProvider.ToString(),page.Checkpoint.Source.NetworkIdentity,
                    executionProvider=page.Checkpoint.Source.ExecutionProvider.ToString(),page.Checkpoint.Source.IndexId,
                    page.Checkpoint.Source.SchemaVersion,page.Checkpoint.Source.CatalogueHash,
                    coverageStartSlot=page.Checkpoint.Source.CoverageStartSlot.ToString(CultureInfo.InvariantCulture)},position=Position(page.Checkpoint.Position),
                    completeFromSlot=page.Checkpoint.CompleteFromSlot.ToString(CultureInfo.InvariantCulture)},
                finalizedHead=new{sourceProvider=page.Head.SourceProvider.ToString(),page.Head.NetworkIdentity,
                    position=Position(page.Head.Position),page.Head.ObservedAt},
                receipts=page.Receipts.Select(receipt=>new{position=Position(receipt.Position),receipt.CommitReference,record=receipt.Record})
            }){IsLoaded=true,LoadedCount=page.Rows.Count};
            return JsonResponse(response);
        }
        catch(UsageProtocolException cause){return Failure(cause.StatusCode,cause.Code,"Indexed admission failed; retain the operation ID for reconciliation.");}
        catch(ArgumentException) when(!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        {return Failure(400,"CQL_REQUEST_INVALID","Invalid bounded indexed wire intent.");}
        catch(CqlV1Projection.Rejected cause){return Failure(cause.Code=="CQL_RESPONSE_LIMIT"?413:503,cause.Code,"Indexed rows did not satisfy the public output contract.");}
        catch(ResponseLimitException){return Failure(413,"CQL_RESPONSE_LIMIT","Indexed response exceeds the one MiB limit; narrow the query.");}
        catch(OperationCanceledException){return Failure(http.RequestAborted.IsCancellationRequested?499:504,"CQL_QUERY_CANCELLED","Indexed query cancelled.");}
        catch(Exception){return Failure(503,"CQL_INDEX_READ_FAILED","Indexed query failed; no partial page returned.");}
    }

    private static object Position(CosmicChainQueryPosition position)
        =>new{slot=position.Slot.ToString(CultureInfo.InvariantCulture),position.BlockIdentity};
}
