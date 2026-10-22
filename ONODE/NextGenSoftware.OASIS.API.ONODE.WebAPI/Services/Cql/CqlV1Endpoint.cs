using System;
using System.IO;
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

public static class CqlV1Endpoint
{
    public const int MaximumResponseBytes = 1024 * 1024;

    public static async Task<IActionResult> ExecuteAsync(HttpContext http, CqlV1Request request, ICqlV1Providers providers)
    {
        // The JWT-derived avatar and the server's exact reservation must agree. A DTO cannot supply an actor.
        var actor = (http.Items["Avatar"] as IAvatar)?.Id ?? Guid.Empty;
        if (actor == Guid.Empty || http.User.Identity?.IsAuthenticated != true)
            return Failure(401, "CQL_AUTHENTICATION_REQUIRED", "Authenticate with a WEB4 bearer token.");

        var admission = PreparedStorageExecution.Current;
        if (admission == null || !admission.IsForAvatar(actor) ||
            !http.Items.TryGetValue(Web4UsageMeasurement.ExecutionKey, out var handle) ||
            handle is not UsageExecutionHandle execution || !admission.IsBoundTo(execution))
            return Failure(503, "CQL_ADMISSION_REQUIRED", "Configure the CQL route's authenticated storage admission and query tariffs.");

        try
        {
            http.RequestAborted.ThrowIfCancellationRequested();
            var requested = CqlV1Compiler.RequestedProvider(request?.Provider);
            // Validate the complete request before provider lookup or manager acquisition.
            var query = CqlV1Compiler.Compile(request, actor, requested);
            var storage = providers.Resolve(requested);
            var selected = storage?.ProviderType?.Value ?? ProviderType.None;
            if (!CqlV1Compiler.Supported(selected) || storage is not ICosmicQueryStorageProvider adapter ||
                selected != adapter.QueryProviderType || requested.HasValue && requested.Value != selected || !admission.HasQueryTariff(selected))
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
                var status = result.ErrorCode == "CQL_QUERY_CANCELLED" ? (http.RequestAborted.IsCancellationRequested ? 499 : 504) : 503;
                return Failure(status, result.ErrorCode, "The query did not complete. Retain the usage operation ID for reconciliation.");
            }

            // Return bounded leaf DTOs. Never expose provider keys/revisions or runtime/relationship objects.
            var rows = result.Result.Providers.Single().Rows;
            if (rows.Any(h => !h.IsPublic && h.CreatedByAvatarId != actor))
                return Failure(503, "CQL_VISIBILITY_VIOLATION", "Provider results violated the mandatory visibility restriction.");
            var response = new OASISResult<object>(new
            {
                provider = selected.ToString(),
                count = rows.Count,
                rows = rows.Select(h => new
                {
                    h.Id, h.Name, h.Description, h.Version, h.IsPublic, h.IsActive, h.CreatedByAvatarId, h.ParentHolonId,
                    metadata = request.IncludeMetadata ? h.MetaData : null
                }).ToArray()
            }) { IsLoaded = true };
            using var buffer = new ResponseBuffer();
            using (var writer = new Utf8JsonWriter(buffer))
                JsonSerializer.Serialize(writer, response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return new FileContentResult(buffer.ToArray(), "application/json");
        }
        catch (ArgumentException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(400, "CQL_REQUEST_INVALID", "Invalid bounded CQL v1 query description."); }
        catch (InvalidOperationException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(503, "CQL_PROVIDER_UNAVAILABLE", "The requested provider is unavailable; no alternative was selected."); }
        catch (ResponseLimitException) { return Failure(413, "CQL_RESPONSE_LIMIT", "Query response exceeds the one MiB limit; narrow the query."); }
        catch (OperationCanceledException) { return Failure(http.RequestAborted.IsCancellationRequested ? 499 : 504, "CQL_QUERY_CANCELLED", "The query was cancelled."); }
        catch (Exception) { return Failure(503, "CQL_EXECUTION_FAILED", "The query did not complete. Retain the usage operation ID for reconciliation."); }
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
