using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Endpoint
{
    private sealed record ComputedScalarConsumer(HttpContext Http, ICqlV1Providers Providers,
        IOASISProvider Storage, ICosmicQueryProjectionCompositionStorageProvider Adapter,
        CosmicQuery<Holon> Source, CosmicScalarOperation Operation)
        : CqlV1Compiler.ISelectionConsumer<Task<IActionResult>>
    {
        public Task<IActionResult> Bind<TInput>(string alias, Expression<Func<Holon, TInput>> selector)
        {
            if (typeof(TInput) != typeof(int) && typeof(TInput) != typeof(int?))
                throw new ArgumentException("The computed scalar transport requires an exact Int32 length output.");
            if (Operation == CosmicScalarOperation.Average)
                return typeof(TInput) == typeof(int)
                    ? ExecuteComputedScalar<TInput, decimal>(Http, Providers, Storage, Adapter, Source, selector, Operation)
                    : ExecuteComputedScalar<TInput, decimal?>(Http, Providers, Storage, Adapter, Source, selector, Operation);
            return ExecuteComputedScalar<TInput, TInput>(Http, Providers, Storage, Adapter, Source, selector, Operation);
        }
    }

    private static async Task<IActionResult> ExecuteComputedScalar<TInput, TResult>(HttpContext http,
        ICqlV1Providers providers, IOASISProvider storage, ICosmicQueryProjectionCompositionStorageProvider adapter,
        CosmicQuery<Holon> source, Expression<Func<Holon, TInput>> selector, CosmicScalarOperation operation)
    {
        var selected = adapter.QueryProviderType;
        var projection = source.Select(selector);
        if (projection.IsError)
            return Failure(400, projection.ErrorCode, "The computed selection is unsupported.");
        var targets = new[] { new CosmicProjectionCompositionTarget(selected, adapter.QueryCapabilities,
            adapter.ProjectionCapabilities, adapter.CompositionCapabilities) };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        TResult value;
        if (operation is CosmicScalarOperation.Sum or CosmicScalarOperation.Average)
        {
            var plan = CosmicProjectedNumericPlanner.Plan<Holon, TInput, TResult>(
                projection.Result, 0, operation, selected, targets);
            if (plan.IsError) return Failure(400, plan.ErrorCode, "The selected provider does not support this computed reduction.");
            var manager = providers.CreateManager(storage);
            http.Items[CqlV1ResourceFilter.DispatchedKey] = true;
            var result = await manager.ExecuteProjectedNumericQueryAsync(plan.Result,
                new ICosmicQueryStorageProvider[] { adapter }, deadline.Token);
            if (result.IsError) return QueryFailure(http, result.ErrorCode);
            deadline.Token.ThrowIfCancellationRequested();
            var scalar = result.Result.Providers.Single();
            if (scalar.Provider != selected)
                return Failure(503, "CQL_PROVIDER_RESULT_INVALID", "Computed scalar provenance differs from the admitted provider.");
            value = scalar.Value;
        }
        else
        {
            var plan = CosmicProjectedExtremumPlanner.Plan<Holon, TInput, TResult>(
                projection.Result, 0, operation, selected, targets);
            if (plan.IsError) return Failure(400, plan.ErrorCode, "The selected provider does not support this computed extremum.");
            var manager = providers.CreateManager(storage);
            http.Items[CqlV1ResourceFilter.DispatchedKey] = true;
            var result = await manager.ExecuteProjectedExtremumQueryAsync(plan.Result,
                new ICosmicQueryStorageProvider[] { adapter }, deadline.Token);
            if (result.IsError) return QueryFailure(http, result.ErrorCode);
            deadline.Token.ThrowIfCancellationRequested();
            var scalar = result.Result.Providers.Single();
            if (scalar.Provider != selected)
                return Failure(503, "CQL_PROVIDER_RESULT_INVALID", "Computed scalar provenance differs from the admitted provider.");
            value = scalar.Value;
        }
        var response = JsonResponse(new OASISResult<object>(new
        {
            version = "cql-scalar-v1", provider = selected.ToString(), operation = operation.ToString(),
            valueType = ProjectionWireType(Nullable.GetUnderlyingType(typeof(TResult)) ?? typeof(TResult)),
            value = ProjectionWireValue(value)
        }) { IsLoaded = true });
        deadline.Token.ThrowIfCancellationRequested();
        return response;
    }
}
