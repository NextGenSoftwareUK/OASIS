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
    public static async Task RunAsync(
        HolonManager manager,
        ICosmicQueryStorageProvider adapter,
        Guid actor,
        string fixtureTag,
        TextWriter output)
    {
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
