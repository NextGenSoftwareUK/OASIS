using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace OASIS.ProviderTests.Fixtures;

public enum CqlEmptyWindowShape { Rows, Graph, Projection }

/// <summary>Shared native contract: protected validation precedes an empty output window.</summary>
public static class CqlEmptyWindowAcceptance
{
    public static async Task VerifyAsync(ICosmicQueryStorageProvider adapter, Holon root, CqlEmptyWindowShape shape)
    {
        var id = root.Id;
        await ReadAsync(Source(1), expectLimit: true);
        await ReadAsync(Source(100), expectLimit: false);

        // A successful plain read after both probes proves that failure did not
        // retain the old read owner, transaction or native traversal state.
        var fresh = await adapter.ExecuteCosmicQueryAsync(Plan(
            Value(CosmicQuery<Holon>.Create().Where(h => h.Id == id))));
        if (fresh.IsError || fresh.Result == null || fresh.Result.Count != 1)
            throw new InvalidOperationException("Fresh native read failed after empty-window probes: " + fresh.Message);

        CosmicQuery<Holon> Source(int maximumNodes)
        {
            var query = Value(CosmicQuery<Holon>.Create().Where(h => h.Id == id)
                .WhereAnyDescendant(h => h.Name != "absent", Value(CosmicQueryTraversalOptions.Create(2, maximumNodes)))
                .Take(0));
            return shape == CqlEmptyWindowShape.Graph
                ? Value(query.WithChildren(Value(CosmicQueryGraphOptions.Create(maxChildDepth: 1))))
                : query;
        }

        CosmicQueryPlan<Holon> Plan(CosmicQuery<Holon> query)
            => Value(CosmicQueryPlanner.Plan(query, adapter.QueryProviderType,
                new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) }));

        async Task ReadAsync(CosmicQuery<Holon> source, bool expectLimit)
        {
            if (shape == CqlEmptyWindowShape.Projection)
            {
                if (adapter is not ICosmicQueryProjectionStorageProvider projection)
                    throw new InvalidOperationException("Native projection adapter required.");
                var selected = Value(source.Select(h => h.Name));
                var plan = Value(CosmicProjectionPlanner.Plan(selected, adapter.QueryProviderType,
                    new[] { new CosmicProjectionTarget(adapter.QueryProviderType, adapter.QueryCapabilities, projection.ProjectionCapabilities) }));
                Check(await projection.ExecuteCosmicProjectionAsync(plan), expectLimit);
            }
            else
                Check(await adapter.ExecuteCosmicQueryAsync(Plan(source)), expectLimit);
        }
    }

    private static T Value<T>(OASISResult<T> result)
    {
        if (result == null || result.IsError)
            throw new InvalidOperationException("Empty-window intent/admission failed: " + result?.Message);
        return result.Result;
    }

    private static void Check<T>(OASISResult<IReadOnlyList<T>> result, bool expectLimit)
    {
        if (result == null) throw new InvalidOperationException("Native reader returned no envelope.");
        if (expectLimit)
        {
            if (!result.IsError || result.ErrorCode != "CQL_QUERY_LIMIT" || result.IsLoaded || result.Result != null)
                throw new InvalidOperationException("Empty output suppressed protected traversal validation: " + result.ErrorCode + " " + result.Message);
        }
        else if (result.IsError || result.Result == null || result.Result.Count != 0)
            throw new InvalidOperationException("A valid empty query published a sentinel row or failed: " + result.Message);
    }
}
