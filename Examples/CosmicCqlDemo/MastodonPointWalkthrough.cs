using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>
/// Read one existing public Mastodon status through WEB4/COSMIC and LINQ.
/// The host supplies its activated adapter; this sample creates no account or post.
/// </summary>
public static class MastodonPointWalkthrough
{
    public static async Task<IReadOnlyList<Holon>> RunAsync(
        HolonManager manager,
        ICosmicQueryStorageProvider adapter,
        string nativeStatusId,
        Guid actor,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        if (adapter.QueryProviderType != ProviderType.ActivityPubOASIS)
            throw new ArgumentException("Use an explicitly configured ActivityPub/Mastodon adapter.",nameof(adapter));

        // 1. Select the logical provider explicitly. This source is a native
        // Mastodon API point read, with no local database or timeline scan.
        var scope = Require(CosmicQueryScope.Named(ProviderType.ActivityPubOASIS));

        // 2. Bind the caller's read policy before composing the source predicate.
        // The point profile publishes only public/unlisted native statuses.
        var access = Require(CosmicQueryReadAccess.OwnerOrPublic(actor));

        var source = Require(CosmicQuery<Holon>.Create(scope)
            .WithReadAccess(access)
            .Where(holon => CosmicQueryMetadata.Value<string>(holon,"MastodonStatusId") == nativeStatusId));

        // 3. Build a deferred IQueryable. The native status key is distinct from
        // the projected OASIS GUID; no hash reversal or GUID guessing is possible.
        var query = Require(CosmicLinq.Create(manager,adapter,source,maximumResults:1));

        // 4. Execute through HolonManager. Unsupported predicates fail during
        // planning; cancellation and payload limits also reject partial results.
        var rows = Require(await query.ToCosmicListAsync(cancellationToken));

        await output.WriteLineAsync("\n=== CQL: native Mastodon status point ===\n");
        await output.WriteLineAsync($"Provider: {adapter.QueryProviderType}; native key: {nativeStatusId}");
        await output.WriteLineAsync($"Returned statuses: {rows.Count}\n");

        foreach (var row in rows)
        {
            // Show source provenance, not untrusted HTML content.
            await output.WriteLineAsync($"  OASIS projection: {row.Id}");
            await output.WriteLineAsync($"  Instance: {row.MetaData["MastodonInstance"]}");
            await output.WriteLineAsync($"  Original object: {row.MetaData["MastodonObjectUri"]}\n");
        }

        // This profile does not enroll Save(), history, arbitrary metadata
        // search, ordering, scalar or projection operators for ActivityPub.
        return rows;
    }

    private static T Require<T>(OASISResult<T> result)
    {
        if (result == null || result.IsError || result.Result == null)
            throw new InvalidOperationException(result?.ErrorCode+": "+result?.Message,result?.Exception);
        return result.Result;
    }
}
