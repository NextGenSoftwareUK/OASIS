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
        var visible = Value(CosmicQuery<Holon>.Create(scope)
            .Where(holon => holon.IsPublic || holon.CreatedByAvatarId == actor)
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
