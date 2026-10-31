using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>
/// Read a current private-catalogue holon by its acknowledged IPFS record CID.
/// Supply an activated adapter and caller policy; this walkthrough performs no writes.
/// </summary>
public static class IpfsPointWalkthrough
{
    public static async Task<IReadOnlyList<Holon>> RunAsync(
        HolonManager manager,
        ICosmicQueryStorageProvider adapter,
        string recordCid,
        Guid actor,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        if (adapter.QueryProviderType != ProviderType.IPFSOASIS)
            throw new ArgumentException("Use an explicitly configured IPFS adapter.",nameof(adapter));

        // 1. Choose IPFS explicitly. The CID is a native content address,
        // distinct from the OASIS holon GUID or the private catalogue root CID.
        var scope = Require(CosmicQueryScope.Named(ProviderType.IPFSOASIS));

        // 2. Bind the caller's read policy before composing the source predicate.
        // Private rows require ownership; public rows can be read by other actors.
        var access = Require(CosmicQueryReadAccess.OwnerOrPublic(actor));

        var source = Require(CosmicQuery<Holon>.Create(scope)
            .WithReadAccess(access)
            .Where(holon => CosmicQueryMetadata.Value<string>(holon,"IPFSRecordCid") == recordCid));

        // 3. Build a deferred IQueryable. No daemon read occurs until execution.
        var query = Require(CosmicLinq.Create(manager,adapter,source,maximumResults:1));

        // 4. Execute through HolonManager. Unsupported predicates fail during
        // planning; cancellation and payload limits also reject partial results.
        var rows = Require(await query.ToCosmicListAsync(cancellationToken));

        await output.WriteLineAsync("\n=== CQL: IPFS current-record CID ===\n");
        await output.WriteLineAsync($"Provider: {adapter.QueryProviderType}; native key: {recordCid}");
        await output.WriteLineAsync($"Returned holons: {rows.Count}\n");

        foreach (var row in rows)
        {
            // Show the canonical holon identity and acknowledged source address.
            await output.WriteLineAsync($"  Holon: {row.Id}");
            await output.WriteLineAsync($"  Record CID: {row.ProviderUniqueStorageKey[ProviderType.IPFSOASIS]}\n");
        }

        // The encrypted catalogue is read and authenticated before exact CID selection.
        // General filtering, ordering and aggregations need an explicit query index.

        return rows;
    }

    private static T Require<T>(OASISResult<T> result)
    {
        if (result == null || result.IsError || result.Result == null)
            throw new InvalidOperationException(result?.ErrorCode+": "+result?.Message,result?.Exception);
        return result.Result;
    }
}
