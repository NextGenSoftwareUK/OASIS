using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>
/// LINQ-style CQL over an already-authorized native provider. The host calls the
/// SAME method for MongoDB, SQLite and Neo4j; no provider-specific query code.
/// For untrusted callers, use WEB4 v1: its server enforces identity and access.
/// </summary>
public static class V1NativeQueries
{
    public static async Task<int> RunAsync(
        HolonManager manager,
        ICosmicQueryStorageProvider adapter,
        Guid actor,
        string fixtureTag,
        TextWriter output)
    {
        var completedQueries = 0;
        var provider = adapter.QueryProviderType;
        var scope = Value(CosmicQueryScope.Named(provider));

        // Local SDK queries run inside the host's trust boundary. We supply the
        // access policy explicitly here; HTTP v1 always adds it on the server.
        var access = Value(CosmicQueryReadAccess.OwnerOrPublic(actor));

        var visible = Value(CosmicQuery<Holon>.Create(scope)
            .WithReadAccess(access)
            .Where(holon => CosmicQueryMetadata.Value<string>(holon, "fixture") == fixtureTag));

        await output.WriteLineAsync($"\n===== C# / LINQ-style CQL: {provider} =====\n");

        // 1. Compose filters and a deterministic native page.
        // Expected: C-public, because visibility is applied before Skip/Take.
        var page = Value(visible
            .Where(holon => CosmicQueryMetadata.Value<long>(holon, "stock") >= 7)
            .OrderBy(holon => holon.Name)
            .ThenBy(holon => holon.Id)
            .Skip(1)
            .Take(1));

        await ShowAsync("1. Native filter / sort / page", page, "C-public");

        // 2. Nested paths, OR and explicit ordinal string operations.
        // A-private remains excluded by the earlier visible query predicate.
        var nested = Value(visible
            .Where(holon => CosmicQueryMetadata.Value<string>(holon, "location", "city") == "London")
            .Where(holon => holon.Name == "A-private" || holon.Name.StartsWith("D-", StringComparison.Ordinal))
            .OrderBy(holon => holon.Name)
            .Take(10));

        await ShowAsync("2. Nested metadata / protected OR", nested, "D-owned");

        // 3. long literals retain their exact value beyond double precision.
        var precise = Value(visible
            .Where(holon => CosmicQueryMetadata.Value<long>(holon, "precision") == 9007199254740993L)
            .OrderBy(holon => holon.Name)
            .Take(10));

        await ShowAsync("3. Exact Int64 metadata", precise, "B-owned", "C-public", "D-owned");

        // 4. The SAME policy applies to every selected child before native limits.
        // A plain root Where(...) cannot express this child-access guarantee.
        var shape = Value(CosmicQueryGraphOptions.Create(
            recursive: true,
            maxChildDepth: 2,
            maximumNodes: 8));

        var graphQuery = Value(visible
            .Where(holon => holon.Name == "C-public")
            .WithChildren(shape));

        var graphPlan = Value(CosmicQueryPlanner.Plan(
            graphQuery,
            provider,
            new[] { new CosmicQueryTarget(provider, adapter.QueryCapabilities) }));

        var graph = Value(await manager.ExecuteQueryAsync(graphPlan, new[] { adapter }));
        var root = graph.Providers.Single().Rows.Single();
        var children = root.Children.Select(child => child.Name).ToArray();

        if (!children.SequenceEqual(new[] { "public child", "owned child" }))
            throw new InvalidOperationException("Child visibility or persisted membership ordering changed.");

        completedQueries++;

        await output.WriteLineAsync("4. Access-bound graph: public root / selected children");
        await output.WriteLineAsync("\n  " + root.Name + "\n    " + string.Join("\n    ", children) + "\n");

        // This is a selected view. For a root-only edit use saveChildren: false;
        // AcceptChanges() cannot authorize replacement of hidden children.
        // For public JSON use WEB4's graph DTOs, which omit private store state.

        // 5. Match parents by a VISIBLE direct child without loading a graph.
        // Each WhereAnyChild is a separate native EXISTS condition. Two clauses
        // may match different siblings; the same actor policy protects both.
        var parentsWithChildren = Value(visible
            .WhereAnyChild(child => child.Name == "public child")
            .WhereAnyChild(child => child.Name == "owned child")
            .OrderBy(parent => parent.Name)
            .Take(1));

        await ShowAsync("5. Native child existence / independent siblings", parentsWithChildren, "C-public");

        // 6. Search a bounded descendant scope without loading child output.
        // A private intermediate prunes its entire branch, even a public leaf.
        // MaximumNodes counts all visible canonical descendants, not just matches.
        var traversal = Value(CosmicQueryTraversalOptions.Create(
            maximumDepth: 2,
            maximumNodes: 2));

        var parentsWithDescendants = Value(visible
            .WhereAnyDescendant(child => child.Name == "owned child", traversal)
            .OrderBy(parent => parent.Name)
            .Take(1));

        await ShowAsync("6. Bounded native descendant existence", parentsWithDescendants, "C-public");

        // 7. A terminal builds an immutable intent; it does not run a row query
        // or hydrate entities. Omitted Take counts the complete visible source.
        await ShowScalarAsync("7. Native LongCount / complete visible source",
            Value(visible.LongCount()), 3L);

        // 8. Window algebra belongs to the source: Skip/Take happen before Count.
        await ShowScalarAsync("8. Count after an explicit source window",
            Value(visible.Skip(1).Take(1).Count()), 1);

        // 9. Access is applied before Any, too. An inaccessible match is false.
        await ShowScalarAsync("9. Any cannot reveal a private root",
            Value(visible.Where(holon => holon.Name == "A-private").Any()), false);

        // 10. Select only the values this screen needs. Source visibility and
        // Skip/Take run natively before selection; no holon or child is hydrated.
        // These detached DTO values carry no entity save/change-tracking state.
        if (adapter is not ICosmicQueryProjectionStorageProvider projectionAdapter)
            throw new InvalidOperationException("This provider has no selected-column contract.");

        var selected = Value(visible
            .OrderBy(holon => holon.Name)
            .ThenBy(holon => holon.Id)
            .Skip(1)
            .Take(1)
            .Select(holon => new
            {
                Label = holon.Name,
                Precision = CosmicQueryMetadata.Value<long>(holon, "precision"),
                Optional = CosmicQueryMetadata.Value<long?>(holon, "nullable"),
                City = CosmicQueryMetadata.Value<string>(holon, "location", "city")
            }));

        var selectedPlan = Value(CosmicProjectionPlanner.Plan(
            selected,
            provider,
            new[]
            {
                new CosmicProjectionTarget(
                    provider,
                    adapter.QueryCapabilities,
                    projectionAdapter.ProjectionCapabilities)
            }));

        var projected = Value(await manager.ExecuteProjectionQueryAsync(
            selectedPlan,
            new[] { adapter },
            maximumResults: 10));

        var summary = projected.Providers.Single().Rows.Single();

        if (summary.Label != "C-public" || summary.Precision != 9007199254740993L
            || summary.Optional != null || summary.City != "London")
            throw new InvalidOperationException("Native selected-column values changed.");

        completedQueries++;

        await output.WriteLineAsync("10. Native Select / detached typed values");
        await output.WriteLineAsync($"\n  {summary.Label}\n  Exact Int64: {summary.Precision}\n  Optional: null\n  City: {summary.City}\n");

        return completedQueries;

        async Task ShowScalarAsync<TResult>(
            string title,
            CosmicScalarQuery<Holon, TResult> query,
            TResult expected)
        {
            // Scalar capability is additive; row support alone does not grant it.
            if (adapter is not ICosmicQueryScalarStorageProvider scalarAdapter)
                throw new InvalidOperationException("This provider has no scalar contract.");

            var plan = Value(CosmicScalarPlanner.Plan(
                query,
                provider,
                new[] { new CosmicScalarTarget(provider,
                    adapter.QueryCapabilities, scalarAdapter.ScalarCapabilities) }));

            var execution = Value(await manager.ExecuteScalarQueryAsync(plan, new[] { adapter }));
            var result = execution.Providers.Single().Value;

            if (!EqualityComparer<TResult>.Default.Equals(result, expected))
                throw new InvalidOperationException(title + " returned an unexpected scalar.");

            completedQueries++;

            await output.WriteLineAsync(title);
            await output.WriteLineAsync($"\n  {typeof(TResult).Name}: {result}\n");
        }

        async Task ShowAsync(string title, CosmicQuery<Holon> query, params string[] expected)
        {
            // Planning checks this concrete provider's actual capabilities
            // before execution. Unsupported expressions fail; no client scan.
            var plan = Value(CosmicQueryPlanner.Plan(
                query,
                provider,
                new[] { new CosmicQueryTarget(provider, adapter.QueryCapabilities) }));

            var execution = Value(await manager.ExecuteQueryAsync(
                plan,
                new[] { adapter },
                maximumResults: 10));

            var names = execution.Providers.Single().Rows.Select(holon => holon.Name).ToArray();

            if (!names.SequenceEqual(expected))
                throw new InvalidOperationException(title + " returned unexpected native rows.");

            completedQueries++;

            await output.WriteLineAsync(title);
            await output.WriteLineAsync("\n  " + string.Join("\n  ", names) + "\n");
        }
    }

    // Application code should handle OASISResult errors at its own boundary.
    // The demo fails loudly so a broken query cannot look like an empty result.
    private static T Value<T>(OASISResult<T> result)
    {
        if (result.IsError)
            throw new InvalidOperationException(result.Message, result.Exception);

        return result.Result;
    }
}
