using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

public sealed class CloudCoffeeRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int Version { get; set; }
    public string City { get; set; } = "";
}

public sealed record CloudRowsReport(string Provider, int Count, long LongCount,
    bool Any, IReadOnlyList<CloudCoffeeRow> Page);

/// <summary>
/// A small shared LINQ demo for an already activated native provider. The host
/// owns its fixtures and authenticated actor; this sample creates no storage.
/// </summary>
public static class CloudRowsWalkthrough
{
    public static async Task<CloudRowsReport> RunAsync(HolonManager manager,
        ICosmicQueryStorageProvider adapter, Guid actorId, string fixtureTag,
        TextWriter output, CancellationToken cancellationToken = default)
    {
        // 1. Scope is explicit. The supplied adapter and actor are selected by
        // the trusted host; naming a provider does not grant storage access.
        var scope = Require(CosmicQueryScope.Named(adapter.QueryProviderType));
        var access = Require(CosmicQueryReadAccess.OwnerOrPublic(actorId));

        var source = Require(CosmicQuery<Holon>.Create(scope)
            .WithReadAccess(access)
            .Where(holon => holon.IsActive &&
                CosmicQueryMetadata.Value<string>(holon, "fixture") == fixtureTag));

        // 2. Create a deferred IQueryable over WEB4/COSMIC. There is no local
        // Enumerable substitute. Unsupported operators return a CQL error.
        var root = Require(CosmicLinq.Create(manager, adapter, source, maximumResults: 10));

        var pageQuery = root
            .OrderBy(holon => holon.Id)
            .Take(10);

        await output.WriteLineAsync($"\n=== Native CQL rows: {adapter.QueryProviderType} ===\n");

        // 3. Hydrate tracked holons through HolonManager. A bounded page never
        // becomes permission to silently truncate an oversized native result.
        var entities = Require(await pageQuery.ToCosmicListAsync(cancellationToken));

        foreach (var holon in entities)
            await output.WriteLineAsync($"  {holon.Id}: {holon.Name} (version {holon.Version})");

        // 4. Select a native DTO shape, including nested metadata. CQL validates
        // every native cell and source proof before constructing any DTO.
        var selectedQuery = pageQuery.Select(holon => new CloudCoffeeRow
        {
            Id = holon.Id,
            Name = holon.Name,
            Version = holon.Version,
            City = CosmicQueryMetadata.Value<string>(holon, "location", "city")
        });

        var selected = Require(await selectedQuery.ToCosmicListAsync(cancellationToken));

        await output.WriteLineAsync("\nSelected native columns:\n");
        foreach (var row in selected)
            await output.WriteLineAsync($"  {row.Name}, {row.City}, version {row.Version}");

        // 5. Standard LINQ terminals use the provider's independent native
        // scalar capability. They do not enumerate this page or build entities.
        cancellationToken.ThrowIfCancellationRequested();
        var count = root.Count();
        var longCount = root.LongCount();
        var any = root.Any();

        await output.WriteLineAsync($"\nMatching source: Count={count}, LongCount={longCount}, Any={any}\n");

        // Each call is an independent read. This walkthrough does not promise
        // a snapshot spanning the page, projection and aggregate operations.
        return new CloudRowsReport(adapter.QueryProviderType.ToString(), count,
            longCount, any, selected);
    }

    private static T Require<T>(OASISResult<T> result)
    {
        if (result.IsError)
            throw new InvalidOperationException($"{result.ErrorCode}: {result.Message}", result.Exception);

        return result.Result;
    }
}
