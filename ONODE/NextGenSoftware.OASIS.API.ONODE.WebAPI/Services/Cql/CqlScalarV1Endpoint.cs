using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
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
    public static async Task<IActionResult> ExecuteScalarAsync(HttpContext http, CqlScalarV1Request request, ICqlV1Providers providers)
    {
        var rejected = CaptureAdmission(http, out var actor, out var admission);
        if (rejected != null) return rejected;
        try
        {
            http.RequestAborted.ThrowIfCancellationRequested();
            var operation = request?.Operation switch
            {
                "Count" => CosmicScalarOperation.Count,
                "LongCount" => CosmicScalarOperation.LongCount,
                "Any" => CosmicScalarOperation.Any,
                "Min" => CosmicScalarOperation.Min,
                "Max" => CosmicScalarOperation.Max,
                _ => throw new ArgumentException("Select Count, LongCount, Any, Min or Max.")
            };
            var selecting = operation is CosmicScalarOperation.Min or CosmicScalarOperation.Max;
            if (selecting != (request.Selection != null))
                throw new ArgumentException("Min/Max require one selection; count/existence operations do not accept it.");
            var requested = CqlV1Compiler.RequestedProvider(request.Provider);
            var source = CqlV1Compiler.CompileScalarSource(request, actor, requested);
            if (!TryResolveEligible(providers, requested, admission, out var storage, out var adapter, out var selected))
                return Failure(403, "CQL_PROVIDER_NOT_ELIGIBLE", "The selected provider is not eligible for this admitted scalar query.");
            if (adapter is not ICosmicQueryScalarStorageProvider scalar)
                return Failure(400, "CQL_CAPABILITY_UNSUPPORTED", "The selected provider does not implement native scalar queries.");
            source = source.WithScope(CosmicQueryScope.Named(selected).Result);
            if (selecting)
            {
                var selection = request.Selection;
                return await CqlV1Compiler.CompileSelection(new CqlProjectionV1Column
                {
                    Alias = "Value", Field = selection.Field, MetadataPath = selection.MetadataPath,
                    ValueType = selection.ValueType, Nullable = selection.Nullable
                }, new SelectedScalarConsumer(http, providers, storage, scalar, source, operation));
            }
            return operation switch
            {
                CosmicScalarOperation.Count => await ExecuteScalar(http, providers, storage, scalar, source.Count().Result),
                CosmicScalarOperation.LongCount => await ExecuteScalar(http, providers, storage, scalar, source.LongCount().Result),
                _ => await ExecuteScalar(http, providers, storage, scalar, source.Any().Result)
            };
        }
        catch (ArgumentException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(400, "CQL_REQUEST_INVALID", "Invalid bounded CQL scalar description."); }
        catch (InvalidOperationException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(503, "CQL_PROVIDER_UNAVAILABLE", "The requested provider is unavailable; no alternative was selected."); }
        catch (ProjectionWireException) { return Failure(503, "CQL_PROVIDER_RESULT_INVALID", "The scalar has an invalid public wire domain."); }
        catch (ResponseLimitException) { return Failure(413, "CQL_RESPONSE_LIMIT", "Scalar response exceeds the public output limit."); }
        catch (OperationCanceledException) { return Failure(http.RequestAborted.IsCancellationRequested ? 499 : 504, "CQL_QUERY_CANCELLED", "The scalar query was cancelled."); }
        catch (Exception) { return Failure(503, "CQL_EXECUTION_FAILED", "The scalar query did not complete. Retain the usage operation ID for reconciliation."); }
    }

    private static async Task<IActionResult> ExecuteScalar<TResult>(HttpContext http, ICqlV1Providers providers,
        IOASISStorageProvider storage, ICosmicQueryScalarStorageProvider adapter, CosmicScalarQuery<Holon, TResult> intent)
    {
        var selected = adapter.QueryProviderType;
        var planned = CosmicScalarPlanner.Plan(intent, selected,
            new[] { new CosmicScalarTarget(selected, adapter.QueryCapabilities, adapter.ScalarCapabilities) });
        if (planned.IsError) return Failure(400, planned.ErrorCode, "The selected provider does not support this scalar query.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        var manager = providers.CreateManager(storage);
        http.Items[CqlV1ResourceFilter.DispatchedKey] = true;
        var result = await manager.ExecuteScalarQueryAsync(planned.Result,
            new ICosmicQueryStorageProvider[] { adapter }, deadline.Token);
        if (result.IsError) return QueryFailure(http, result.ErrorCode);
        deadline.Token.ThrowIfCancellationRequested();
        var scalar = result.Result.Providers.Single();
        if (scalar.Provider != selected) return Failure(503, "CQL_PROVIDER_RESULT_INVALID", "Scalar provenance differs from the admitted provider.");
        var selectedValue = intent.Selector != null;
        var domain = selectedValue
            ? ProjectionWireType(Nullable.GetUnderlyingType(typeof(TResult)) ?? typeof(TResult))
            : intent.Operation == CosmicScalarOperation.Count ? "Int32"
                : intent.Operation == CosmicScalarOperation.LongCount ? "Int64" : "Boolean";
        object value = ProjectionWireValue(scalar.Value);
        var response = JsonResponse(new OASISResult<object>(new
        {
            version = "cql-scalar-v1", provider = selected.ToString(), operation = intent.Operation.ToString(), valueType = domain, value
        }) { IsLoaded = true });
        deadline.Token.ThrowIfCancellationRequested();
        return response;
    }

    private sealed class SelectedScalarConsumer : CqlV1Compiler.ISelectionConsumer<Task<IActionResult>>
    {
        private readonly HttpContext http;
        private readonly ICqlV1Providers providers;
        private readonly IOASISStorageProvider storage;
        private readonly ICosmicQueryScalarStorageProvider adapter;
        private readonly CosmicQuery<Holon> source;
        private readonly CosmicScalarOperation operation;

        internal SelectedScalarConsumer(HttpContext http, ICqlV1Providers providers, IOASISStorageProvider storage,
            ICosmicQueryScalarStorageProvider adapter, CosmicQuery<Holon> source, CosmicScalarOperation operation)
        { this.http = http; this.providers = providers; this.storage = storage; this.adapter = adapter; this.source = source; this.operation = operation; }

        public Task<IActionResult> Bind<TValue>(string alias, Expression<Func<Holon, TValue>> selector)
        {
            var intent = operation == CosmicScalarOperation.Min ? source.Min(selector) : source.Max(selector);
            if (intent.IsError) throw new ArgumentException(intent.Message);
            return ExecuteScalar(http, providers, storage, adapter, intent.Result);
        }
    }

}
