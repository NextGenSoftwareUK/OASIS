using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>
/// Read one encrypted SOLID holon by its canonical native record key.
/// Supply an activated adapter and caller policy; this walkthrough performs no writes.
/// </summary>
public static class SolidPointWalkthrough
{
    /// <summary>
    /// Select several encrypted native resources with one CQL membership predicate.
    /// EnableCqlRecordKeySets must be explicitly enabled on the configured adapter.
    /// Use three keys for the full sample and two of those keys for the subset.
    /// </summary>
    public static async Task<IReadOnlyList<Holon>> RunRecordSetAsync(
        HolonManager manager,
        ICosmicQueryStorageProvider adapter,
        IEnumerable<string> recordKeys,
        Guid actor,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        if (adapter.QueryProviderType != ProviderType.SOLIDOASIS)
            throw new ArgumentException("Use an explicitly configured SOLID adapter.", nameof(adapter));

        // Capture the selection before constructing a deferred query. Keys are
        // canonical record GUIDs inside this Pod, not arbitrary resource URLs.
        var keys = recordKeys.ToArray();
        var scope = Require(CosmicQueryScope.Named(ProviderType.SOLIDOASIS));
        var access = Require(CosmicQueryReadAccess.OwnerOrPublic(actor));

        var source = Require(CosmicQuery<Holon>.Create(scope)
            .WithReadAccess(access)
            .Where(holon => keys.Contains(CosmicQueryMetadata.Value<string>(holon, "SolidRecordId"))));

        // HolonManager and the shared CQL planner own execution and limits.
        // Unsupported membership rejects before HTTP; this is not a Pod scan.
        var query = Require(CosmicLinq.Create(manager, adapter, source, maximumResults: 8));
        var rows = Require(await query.ToCosmicListAsync(cancellationToken));

        await output.WriteLineAsync("\n=== CQL: SOLID encrypted resource selection ===\n");
        await output.WriteLineAsync($"Selected keys: {keys.Length}; returned holons: {rows.Count}\n");

        foreach (var row in rows)
        {
            await output.WriteLineAsync($"  Name: {row.Name}");
            await output.WriteLineAsync($"  Description: {row.Description}");
            await output.WriteLineAsync($"  Pod resource: {row.MetaData["SolidRecordResource"]}\n");
        }

        return rows;
    }

    public static async Task<IReadOnlyList<Holon>> RunAsync(
        HolonManager manager,
        ICosmicQueryStorageProvider adapter,
        string recordKey,
        Guid actor,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        if (adapter.QueryProviderType != ProviderType.SOLIDOASIS)
            throw new ArgumentException("Use an explicitly configured SOLID adapter.",nameof(adapter));

        // 1. Choose SOLID explicitly. The native key is the lowercase record GUID,
        // scoped to the configured Pod; it is not an arbitrary external URL.
        var scope = Require(CosmicQueryScope.Named(ProviderType.SOLIDOASIS));

        // 2. Bind the caller's read policy before composing the source predicate.
        // Private rows require ownership; public rows can be read by other actors.
        var access = Require(CosmicQueryReadAccess.OwnerOrPublic(actor));

        var source = Require(CosmicQuery<Holon>.Create(scope)
            .WithReadAccess(access)
            .Where(holon => CosmicQueryMetadata.Value<string>(holon,"SolidRecordId") == recordKey));

        // 3. Build a deferred IQueryable. No HTTP read occurs until execution.
        var query = Require(CosmicLinq.Create(manager,adapter,source,maximumResults:1));

        // 4. Execute through HolonManager. Unsupported predicates fail during
        // planning; cancellation and payload limits also reject partial results.
        var rows = Require(await query.ToCosmicListAsync(cancellationToken));

        await output.WriteLineAsync("\n=== CQL: SOLID encrypted record point ===\n");
        await output.WriteLineAsync($"Provider: {adapter.QueryProviderType}; native key: {recordKey}");
        await output.WriteLineAsync($"Returned holons: {rows.Count}\n");

        foreach (var row in rows)
        {
            // Show the canonical holon identity and acknowledged source address.
            await output.WriteLineAsync($"  Holon: {row.Id}");
            await output.WriteLineAsync($"  Record key: {row.ProviderUniqueStorageKey[ProviderType.SOLIDOASIS]}");
            await output.WriteLineAsync($"  Pod resource: {row.MetaData["SolidRecordResource"]}\n");
        }

        // One authenticated LDP record is decoded. General filters and ordering
        // require an explicitly supported server query extension or persistent index.

        return rows;
    }

    private static T Require<T>(OASISResult<T> result)
    {
        if (result == null || result.IsError || result.Result == null)
            throw new InvalidOperationException(result?.ErrorCode+": "+result?.Message,result?.Exception);
        return result.Result;
    }
}
