using System;
using System.Linq;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
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
            if(prepared.ProjectionPlan!=null){
                var selected=await prepared.Binding.Manager.ExecuteIndexedProjectionQueryAsync(prepared.ProjectionPlan,
                    (ICosmicChainQueryIndexProjectionReader)prepared.Binding.Reader,prepared.Binding.Observer,prepared.MaximumResults,
                    MaximumResponseBytes,deadline.Token);
                if(selected.IsError){
                    if(!admission.ProviderExecutionPossible)http.Items.Remove(CqlV1ResourceFilter.DispatchedKey);
                    return QueryFailure(http,selected.ErrorCode);
                }
                var projection=selected.Result;
                return IndexedResponse("indexed-projection",projection.Checkpoint,projection.Head,projection.Receipts,
                    SelectedWireRows(projection.Rows,deadline.Token),projection.Rows.Count,SelectedWireColumns(prepared.ProjectionPlan.Query.Columns));
            }
            var result=await prepared.Binding.Manager.ExecuteIndexedQueryAsync(prepared.Plan,prepared.Binding.Reader,
                prepared.Binding.Observer,prepared.MaximumResults,deadline.Token);
            if(result.IsError)
            {
                if(!admission.ProviderExecutionPossible)http.Items.Remove(CqlV1ResourceFilter.DispatchedKey);
                return QueryFailure(http,result.ErrorCode);
            }
            var page=result.Result;
            var rows=CqlV1Projection.Create(page.Rows,prepared.Plan.Query.ReadAccess,prepared.IncludeMetadata,null,deadline.Token);
            return IndexedResponse("indexed",page.Checkpoint,page.Head,page.Receipts,rows,page.Rows.Count);
        }
        catch(UsageProtocolException cause){return Failure(cause.StatusCode,cause.Code,"Indexed admission failed; retain the operation ID for reconciliation.");}
        catch(ArgumentException) when(!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        {return Failure(400,"CQL_REQUEST_INVALID","Invalid bounded indexed wire intent.");}
        catch(CqlV1Projection.Rejected cause){return Failure(cause.Code=="CQL_RESPONSE_LIMIT"?413:503,cause.Code,"Indexed rows did not satisfy the public output contract.");}
        catch(ProjectionWireException){return Failure(503,"CQL_PROVIDER_RESULT_INVALID","An indexed selected scalar has an invalid wire domain.");}
        catch(ResponseLimitException){return Failure(413,"CQL_RESPONSE_LIMIT","Indexed response exceeds the one MiB limit; narrow the query.");}
        catch(OperationCanceledException){return Failure(http.RequestAborted.IsCancellationRequested?499:504,"CQL_QUERY_CANCELLED","Indexed query cancelled.");}
        catch(Exception){return Failure(503,"CQL_INDEX_READ_FAILED","Indexed query failed; no partial page returned.");}
    }

    public static async Task<IActionResult> ExecuteIndexedScalarAsync(HttpContext http,CqlIndexedScalarV1Request request)
    {
        try
        {
            var rejected=CaptureAdmission(http,out var actor,out var admission);
            if(rejected!=null)return rejected;
            if(!CqlIndexedV1RequestContext.TryConsume(http,admission,out var prepared))
                return Failure(503,"CQL_INDEX_PREPARATION_REQUIRED","Bind the frozen scalar preparation and exact reservation before dispatch.");
            if(prepared.Actor!=actor||!prepared.Matches(request))
                return Failure(409,"CQL_INDEX_INTENT_MISMATCH","The scalar intent differs from the reserved request.");
            if(prepared.Binding.Manager.OASISDNA?.OASIS?.HyperDriveMode!=HyperDriveModes.V2)
                return Failure(503,"CQL_V2_REQUIRED","Indexed queries require HyperDrive V2.");
            http.RequestAborted.ThrowIfCancellationRequested();
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            http.Items[CqlV1ResourceFilter.DispatchedKey]=true;
            if(prepared.CountPlan!=null)return await ExecuteIndexedScalar(http,prepared,prepared.CountPlan,admission,deadline.Token);
            if(prepared.LongCountPlan!=null)return await ExecuteIndexedScalar(http,prepared,prepared.LongCountPlan,admission,deadline.Token);
            return await ExecuteIndexedScalar(http,prepared,prepared.AnyPlan,admission,deadline.Token);
        }
        catch(UsageProtocolException cause){return Failure(cause.StatusCode,cause.Code,"Indexed scalar admission failed; retain the operation ID for reconciliation.");}
        catch(ArgumentException) when(!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        {return Failure(400,"CQL_REQUEST_INVALID","Invalid bounded indexed scalar intent.");}
        catch(ProjectionWireException){return Failure(503,"CQL_PROVIDER_RESULT_INVALID","Indexed scalar has an invalid wire domain.");}
        catch(ResponseLimitException){return Failure(413,"CQL_RESPONSE_LIMIT","Indexed scalar exceeds the response limit.");}
        catch(OperationCanceledException){return Failure(http.RequestAborted.IsCancellationRequested?499:504,"CQL_QUERY_CANCELLED","Indexed scalar cancelled.");}
        catch(Exception){return Failure(503,"CQL_INDEX_READ_FAILED","Indexed scalar failed; no partial result returned.");}
    }

    private static async Task<IActionResult> ExecuteIndexedScalar<TResult>(HttpContext http,CqlIndexedV1Preparation prepared,
        CosmicScalarPlan<Holon,TResult> plan,PreparedStorageExecution admission,CancellationToken cancellationToken)
    {
        var result=await prepared.Binding.Manager.ExecuteIndexedScalarQueryAsync(plan,
            (ICosmicChainQueryIndexScalarReader)prepared.Binding.Reader,prepared.Binding.Observer,cancellationToken);
        if(result.IsError)
        {
            if(!admission.ProviderExecutionPossible)http.Items.Remove(CqlV1ResourceFilter.DispatchedKey);
            return QueryFailure(http,result.ErrorCode);
        }
        cancellationToken.ThrowIfCancellationRequested();
        var scalar=result.Result;
        var response=IndexedEvidence("indexed-scalar",scalar.Checkpoint,scalar.Head);
        response["assurance"]=scalar.Assurance;
        response["operation"]=plan.Query.Operation.ToString();
        response["valueType"]=plan.Query.Operation==CosmicScalarOperation.Count?"Int32":plan.Query.Operation==CosmicScalarOperation.LongCount?"Int64":"Boolean";
        response["value"]=ProjectionWireValue(scalar.Value);
        return JsonResponse(new OASISResult<object>(response){IsLoaded=true});
    }

    private static IActionResult IndexedResponse(string mode,CosmicChainQueryCheckpoint checkpoint,CosmicChainQueryFinalizedHead head,
        IReadOnlyList<CosmicChainQueryRecordReceipt> receipts,object rows,int count,object columns=null)
    {
        var response=IndexedEvidence(mode,checkpoint,head);
        response["assurance"]="trusted-source-observation";
        response["count"]=count;response["rows"]=rows;
        response["receipts"]=receipts.Select(receipt=>new{position=Position(receipt.Position),receipt.CommitReference,record=receipt.Record});
        if(columns!=null)response["columns"]=columns;
        return JsonResponse(new OASISResult<object>(response){IsLoaded=true,LoadedCount=count});
    }

    private static Dictionary<string,object> IndexedEvidence(string mode,CosmicChainQueryCheckpoint checkpoint,CosmicChainQueryFinalizedHead head)
    {
        return new Dictionary<string,object>{
            ["mode"]=mode,["provider"]=checkpoint.Source.SourceProvider.ToString(),
            ["executionProvider"]=checkpoint.Source.ExecutionProvider.ToString(),
            ["checkpoint"]=new{source=new{sourceProvider=checkpoint.Source.SourceProvider.ToString(),checkpoint.Source.NetworkIdentity,
                executionProvider=checkpoint.Source.ExecutionProvider.ToString(),checkpoint.Source.IndexId,checkpoint.Source.SchemaVersion,
                checkpoint.Source.CatalogueHash,coverageStartSlot=checkpoint.Source.CoverageStartSlot.ToString(CultureInfo.InvariantCulture)},
                position=Position(checkpoint.Position),completeFromSlot=checkpoint.CompleteFromSlot.ToString(CultureInfo.InvariantCulture)},
            ["finalizedHead"]=new{sourceProvider=head.SourceProvider.ToString(),head.NetworkIdentity,position=Position(head.Position),head.ObservedAt}
        };
    }

    private static object Position(CosmicChainQueryPosition position)
        =>new{slot=position.Slot.ToString(CultureInfo.InvariantCulture),position.BlockIdentity};
}
