using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Host-owned lookup of existing providers. Implementations must never activate or fail over.</summary>
public interface ICqlV1Providers
{
    IOASISStorageProvider Resolve(ProviderType? requested);
    HolonManager CreateManager(IOASISStorageProvider provider);
}

/// <summary>Records affirmative no-execution evidence even when API model validation short-circuits the action.</summary>
public sealed class CqlV1ResourceFilter : IAsyncResourceFilter
{
    internal const string DispatchedKey = "OASIS.CQL.V1.Dispatched";
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        try { await next(); }
        finally
        {
            var http = context.HttpContext;
            if (!http.Items.ContainsKey(DispatchedKey) &&
                http.Items.TryGetValue(Web4UsageMeasurement.ExecutionKey, out var value) && value is UsageExecutionHandle execution)
                await execution.RecordNoProviderExecutionAsync("CQL request rejected before provider dispatch.");
        }
    }
}

public static partial class CqlV1Endpoint
{
    public const int MaximumResponseBytes = 1024 * 1024;

    public static async Task<IActionResult> ExecuteAsync(HttpContext http, CqlV1Request request, ICqlV1Providers providers)
    {
        var rejected = CaptureAdmission(http, out var actor, out var admission);
        if (rejected != null) return rejected;

        try
        {
            http.RequestAborted.ThrowIfCancellationRequested();
            var requested = CqlV1Compiler.RequestedProvider(request?.Provider);
            // Validate the complete request before provider lookup or manager acquisition.
            var query = CqlV1Compiler.Compile(request, actor, requested);
            if (!TryResolveEligible(providers, requested, admission, out var storage, out var adapter, out var selected))
                return Failure(403, "CQL_PROVIDER_NOT_ELIGIBLE", "The selected provider is not eligible for this admitted v1 query.");

            query = query.WithScope(CosmicQueryScope.Named(selected).Result);
            var plan = CosmicQueryPlanner.Plan(query, selected, new[] { new CosmicQueryTarget(selected, adapter.QueryCapabilities) });
            if (plan.IsError) return Failure(400, plan.ErrorCode, "The selected provider does not support this query.");

            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            var manager = providers.CreateManager(storage);
            http.Items[CqlV1ResourceFilter.DispatchedKey] = true;
            var result = await manager.ExecuteQueryAsync(plan.Result, new[] { adapter }, maximumResults: request.Take, cancellationToken: deadline.Token);
            if (result.IsError)
            {
                return QueryFailure(http, result.ErrorCode);
            }

            // Return bounded leaf DTOs. Never expose provider keys/revisions or runtime/relationship objects.
            var rows = result.Result.Providers.Single().Rows;
            if (query.ReadAccess==null || query.ReadAccess.ActorId!=actor || rows.Any(h => !query.ReadAccess.CanRead(h)))
                return Failure(503, "CQL_VISIBILITY_VIOLATION", "Provider results violated the mandatory visibility restriction.");
            var response = new OASISResult<object>(new
            {
                provider = selected.ToString(),
                count = rows.Count,
                rows = CqlV1Projection.Create(rows,query.ReadAccess,request.IncludeMetadata,
                    query.GraphOptions,deadline.Token)
            }) { IsLoaded = true };
            return JsonResponse(response);
        }
        catch (ArgumentException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(400, "CQL_REQUEST_INVALID", "Invalid bounded CQL v1 query description."); }
        catch (InvalidOperationException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(503, "CQL_PROVIDER_UNAVAILABLE", "The requested provider is unavailable; no alternative was selected."); }
        catch (CqlV1Projection.Rejected cause)
        { return Failure(cause.Code=="CQL_RESPONSE_LIMIT"?413:503,cause.Code,"Query results did not satisfy the public output contract."); }
        catch (ResponseLimitException) { return Failure(413, "CQL_RESPONSE_LIMIT", "Query response exceeds the one MiB limit; narrow the query."); }
        catch (OperationCanceledException) { return Failure(http.RequestAborted.IsCancellationRequested ? 499 : 504, "CQL_QUERY_CANCELLED", "The query was cancelled."); }
        catch (Exception) { return Failure(503, "CQL_EXECUTION_FAILED", "The query did not complete. Retain the usage operation ID for reconciliation."); }
    }

    private static IActionResult CaptureAdmission(HttpContext http, out Guid actor, out PreparedStorageExecution admission)
    {
        actor = (http.Items["Avatar"] as IAvatar)?.Id ?? Guid.Empty;
        admission = null;
        if (actor == Guid.Empty || http.User.Identity?.IsAuthenticated != true)
            return Failure(401, "CQL_AUTHENTICATION_REQUIRED", "Authenticate with a WEB4 bearer token.");
        admission = PreparedStorageExecution.Current;
        if (admission == null || !admission.IsForAvatar(actor) ||
            !http.Items.TryGetValue(Web4UsageMeasurement.ExecutionKey, out var handle) ||
            handle is not UsageExecutionHandle execution || !admission.IsBoundTo(execution))
            return Failure(503, "CQL_ADMISSION_REQUIRED", "Configure the CQL route's authenticated storage admission and query tariffs.");
        return null;
    }

    private static bool TryResolveEligible(ICqlV1Providers providers, ProviderType? requested, PreparedStorageExecution admission,
        out IOASISStorageProvider storage, out ICosmicQueryStorageProvider adapter, out ProviderType selected)
    {
        storage = providers.Resolve(requested);
        adapter = storage as ICosmicQueryStorageProvider;
        selected = storage?.ProviderType?.Value ?? ProviderType.None;
        return CqlV1Compiler.Supported(selected) && adapter != null && selected == adapter.QueryProviderType
            && (!requested.HasValue || requested.Value == selected) && admission.HasQueryTariff(selected);
    }

    private static IActionResult QueryFailure(HttpContext http, string code)
    {
        var status = code == "CQL_QUERY_CANCELLED" ? (http.RequestAborted.IsCancellationRequested ? 499 : 504)
            : code == "CQL_QUERY_TIMEOUT" ? 504 : 503;
        return Failure(status, code, "The query did not complete. Retain the usage operation ID for reconciliation.");
    }

    private static IActionResult JsonResponse(object response)
    {
        using var buffer = new ResponseBuffer();
        using (var writer = new Utf8JsonWriter(buffer))
            JsonSerializer.Serialize(writer, response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new FileContentResult(buffer.ToArray(), "application/json");
    }

    private static IActionResult Failure(int status, string code, string message) => new ObjectResult(
        new OASISResult<object> { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message }) { StatusCode = status };

    private sealed class ResponseLimitException : IOException { }
    private sealed class ResponseBuffer : MemoryStream
    {
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Length + count > MaximumResponseBytes) throw new ResponseLimitException();
            base.Write(buffer, offset, count);
        }
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (Length + buffer.Length > MaximumResponseBytes) throw new ResponseLimitException();
            base.Write(buffer);
        }
    }
}


/// <summary>Explicit public fields only; stored membership and provider state are never graph DTOs.</summary>
internal static class CqlV1Projection
{
    internal const int MaximumDepth=8;
    internal sealed class Row
    {
        public Guid Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int Version { get; }
        public bool IsPublic { get; }
        public bool IsActive { get; }
        public Guid CreatedByAvatarId { get; }
        public Guid ParentHolonId { get; }
        public object Metadata { get; }
        [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<Row> Children { get; }

        internal Row(IHolon source,bool metadata,Guid parent,IReadOnlyList<Row> children)
        {
            Id=source.Id;Name=source.Name;Description=source.Description;Version=source.Version;
            IsPublic=source.IsPublic;IsActive=source.IsActive;CreatedByAvatarId=source.CreatedByAvatarId;
            ParentHolonId=parent;Metadata=metadata?source.MetaData:null;Children=children;
        }
    }

    internal sealed class Rejected:Exception
    {
        internal string Code { get; }
        internal Rejected(string code):base(code){Code=code;}
    }

    internal static IReadOnlyList<Row> Create(IReadOnlyList<IHolon> roots,CosmicQueryReadAccess access,
        bool includeMetadata,CosmicQueryGraphOptions graph,CancellationToken cancellation=default)
    {
        cancellation.ThrowIfCancellationRequested();
        if(roots==null || access==null)throw new Rejected("CQL_VISIBILITY_VIOLATION");
        var result=new List<Row>();
        var pending=new Stack<(IHolon Source,List<Row> Output,Guid Parent,int Depth,HashSet<Guid> Seen)>();
        var count=roots.Count;
        if(count<0 || count>(graph?.MaximumNodes??100))throw new Rejected("CQL_RESPONSE_LIMIT");
        var rootIds=new HashSet<Guid>();
        for(var index=count-1;index>=0;index--)
        {
            cancellation.ThrowIfCancellationRequested();
            var root=roots[index];
            if(root==null || !rootIds.Add(root.Id))throw new Rejected("CQL_PROVIDER_RESULT_INVALID");
            pending.Push((root,result,Guid.Empty,0,new HashSet<Guid>()));
        }
        if(roots.Count!=count)throw new Rejected("CQL_PROVIDER_RESULT_INVALID");
        var nodes=0;
        while(pending.Count>0)
        {
            cancellation.ThrowIfCancellationRequested();
            var frame=pending.Pop();var source=frame.Source;
            if(frame.Depth>MaximumDepth)throw new Rejected("CQL_RESPONSE_LIMIT");
            if(source==null || source.Id==Guid.Empty || !frame.Seen.Add(source.Id))
                throw new Rejected("CQL_PROVIDER_RESULT_INVALID");
            if(!access.CanRead(source))throw new Rejected("CQL_VISIBILITY_VIOLATION");
            if(++nodes>(graph?.MaximumNodes??100))throw new Rejected("CQL_RESPONSE_LIMIT");
            if(graph!=null && frame.Depth>0 && (source.ParentHolonId!=frame.Parent ||
                graph.ChildType!=HolonType.All && source.HolonType!=graph.ChildType))
                throw new Rejected("CQL_PROVIDER_RESULT_INVALID");
            var output=graph==null?null:new List<Row>();
            // A graph root's inaccessible ancestor is not part of the selected view.
            var parent=graph==null?source.ParentHolonId:frame.Parent;
            var row=new Row(source,includeMetadata,parent,output?.AsReadOnly());
            if(!row.IsPublic && row.CreatedByAvatarId!=access.ActorId)throw new Rejected("CQL_VISIBILITY_VIOLATION");
            frame.Output.Add(row);
            if(graph==null)continue;
            var children=source.Children;
            var childCount=children?.Count??0;
            if(childCount<0 || !graph.IncludesChildrenAtDepth(frame.Depth) && childCount!=0)
                throw new Rejected("CQL_PROVIDER_RESULT_INVALID");
            if(childCount>graph.MaximumNodes-nodes-pending.Count)
                throw new Rejected("CQL_RESPONSE_LIMIT");
            for(var index=childCount-1;index>=0;index--)
                pending.Push((children[index],output,source.Id,frame.Depth+1,frame.Seen));
            if(children!=null && children.Count!=childCount)throw new Rejected("CQL_PROVIDER_RESULT_INVALID");
        }
        return result.AsReadOnly();
    }
}
