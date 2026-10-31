using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>
/// Eight runnable C# examples, identical for MongoDB, SQLite and Neo4j.
/// The host owns the synthetic fixture and supplies its root ID and actor.
/// This class only queries; it never creates, changes or deletes records.
/// </summary>
public static class SourceAndProjectedAllDemo
{
    public static async Task<int> RunAsync(
        HolonManager manager,
        ICosmicQueryProjectedAllStorageProvider adapter,
        Guid rootId,
        Guid actor,
        TextWriter output)
    {
        // Explicit provider scope keeps this walkthrough on the supplied store.
        // Plan against its actual capabilities; unsupported shapes fail here.
        var scope = Value(CosmicQueryScope.Named(adapter.QueryProviderType));
        var source = Value(CosmicQuery<Holon>.Create(scope)
            .Where(row => row.Id == rootId));

        var bounds = Value(CosmicQueryTraversalOptions.Create(
            maximumDepth: 2,
            maximumNodes: 4));

        // 1. Nested source filtering: a direct child must have a matching
        // descendant. Bounds apply to each visited descendant scope.
        // This trusted backend example intentionally has no user visibility
        // restriction. Example 3 adds the user-facing owner/public policy.
        var nested = Value(source.WhereAnyChild(child =>
            CosmicQueryRelations.AnyDescendant(
                child,
                leaf => leaf.Name == "grandchild one",
                bounds)));

        var rowPlan = Value(CosmicQueryPlanner.Plan(
            nested,
            adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) }));

        var rows = Value(await manager.ExecuteQueryAsync(rowPlan, new[] { adapter }));
        Check(rows.Providers.Single().Rows.Single().Name == "protected source root");
        await Show("1. Child -> descendant source filter", "protected source root");

        // 2. NOT composes with a bounded native relationship predicate.
        // Count is a native scalar; it does not fetch holons to count in C#.
        var withoutMissingChild = Value(source.Where(row =>
            !CosmicQueryRelations.AnyDescendant(
                row,
                child => child.Name == "absent",
                bounds)));

        var scalar = (ICosmicQueryScalarStorageProvider)adapter;
        var countPlan = Value(CosmicScalarPlanner.Plan(
            Value(withoutMissingChild.LongCount()),
            adapter.QueryProviderType,
            new[] { new CosmicScalarTarget(adapter.QueryProviderType, adapter.QueryCapabilities, scalar.ScalarCapabilities) }));

        var counted = Value(await manager.ExecuteScalarQueryAsync(countPlan, new[] { adapter }));
        Check(counted.Providers.Single().Value == 1);
        await Show("2. NOT relationship + native LongCount", "1");

        // 3. Visibility applies at EVERY relationship hop. Public grandchildren
        // behind another actor's private parent cannot make the root qualify.
        var visible = Value(source.WithReadAccess(Value(CosmicQueryReadAccess.OwnerOrPublic(actor))));
        var visibleNested = Value(visible.WhereAnyChild(child =>
            CosmicQueryRelations.AnyDescendant(child, leaf => leaf.Name == "grandchild one", bounds)));

        var projection = (ICosmicQueryProjectionStorageProvider)adapter;
        var projectionPlan = Value(CosmicProjectionPlanner.Plan(
            Value(visibleNested.Select(row => row.Name)),
            adapter.QueryProviderType,
            new[] { new CosmicProjectionTarget(adapter.QueryProviderType, adapter.QueryCapabilities, projection.ProjectionCapabilities) }));

        var projected = Value(await manager.ExecuteProjectionQueryAsync(projectionPlan, new[] { adapter }));
        Check(projected.Providers.Single().Rows.Count == 0);
        await Show("3. Visibility before nested filtering/projection", "0 rows");

        // 4-5. Select first, then filter/order/page the SELECTED sequence.
        // All runs over that final sequence natively. A false result is a
        // successful query with a counterexample, not an execution error.
        var selected = nested
            .Select(row => new { row.Name, row.Version })
            .Where(value => value.Version > 0)
            .OrderBy(value => value.Name)
            .Skip(0)
            .Take(1);

        await All("4. Selected pipeline -> All (true)",
            selected.All(value => value.Name == "protected source root"), true);

        await All("5. Selected pipeline -> All (false)",
            selected.All(value => value.Version < 0), false);

        // 6-7. All over an empty final sequence is true. Source validation still
        // completes; an empty page cannot turn a traversal error into success.
        await All("6. Empty selected window -> All", selected.Take(0).All(value => false), true);

        await All("7. Empty source window -> All",
            nested.Take(0).Select(row => row.Name).All(value => false), true);

        // 8. A true OR branch and Take(0) cannot hide an excessive visited scope.
        // Handle the error explicitly; never display a fabricated true/false.
        var tight = Value(CosmicQueryTraversalOptions.Create(2, 1));
        var invalid = source.Where(row => row.Name == "protected source root"
            || CosmicQueryRelations.AnyDescendant(row, child => true, tight))
            .Take(0)
            .Select(row => row.Name)
            .All(value => true);

        var rejected = await manager.ExecuteProjectedAllQueryAsync(Plan(Value(invalid)), new[] { adapter });
        Check(rejected.IsError && rejected.ErrorCode == "CQL_QUERY_LIMIT"
            && !rejected.IsLoaded && rejected.Result == null);
        await Show("8. Protected traversal error survives OR/Take(0)", rejected.ErrorCode);

        return 8;

        CosmicProjectedAllPlan<Holon, T> Plan<T>(CosmicProjectedAllQuery<Holon, T> query)
            => Value(CosmicProjectedAllPlanner.Plan(query, adapter.QueryProviderType,
                new[] { new CosmicProjectionCompositionTarget(adapter.QueryProviderType,
                    adapter.QueryCapabilities, adapter.ProjectionCapabilities, adapter.CompositionCapabilities) }));

        async Task All<T>(string title, OASISResult<CosmicProjectedAllQuery<Holon, T>> captured, bool expected)
        {
            var result = Value(await manager.ExecuteProjectedAllQueryAsync(Plan(Value(captured)), new[] { adapter }));
            Check(result.Providers.Single().Value == expected);
            await Show(title, expected.ToString());
        }

        async Task Show(string title, string result)
            => await output.WriteLineAsync($"\n{title}\n\n    {result}\n");
    }

    private static T Value<T>(OASISResult<T> result)
    {
        if (result.IsError) throw new InvalidOperationException(result.ErrorCode + ": " + result.Message, result.Exception);
        return result.Result;
    }

    private static void Check(bool condition)
    {
        if (!condition) throw new InvalidOperationException("The native demo result differs from its documented fixture.");
    }
}
