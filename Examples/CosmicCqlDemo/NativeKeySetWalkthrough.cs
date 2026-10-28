using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>
/// The same bounded IN query through HolonManager and LINQ on IPFS, SOLID or Mastodon.
/// The caller supplies an activated, explicitly enrolled native adapter and existing keys.
/// </summary>
public static class NativeKeySetWalkthrough
{
    public static async Task<IReadOnlyList<Holon>> RunAsync(
        HolonManager manager,
        ICosmicQueryStorageProvider adapter,
        IEnumerable<string> nativeKeys,
        Guid actor,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        var metadataKey = adapter.QueryProviderType switch
        {
            ProviderType.IPFSOASIS => "IPFSRecordCid",
            ProviderType.SOLIDOASIS => "SolidRecordId",
            ProviderType.ActivityPubOASIS => "MastodonStatusId",
            _ => throw new ArgumentException("Select an enrolled protocol key-set adapter.", nameof(adapter))
        };

        // 1. Freeze the chosen source keys. These are native CIDs/resource GUIDs/status
        // IDs, not interchangeable OASIS projection IDs. No discovery scan is implied.
        var keys = nativeKeys.ToArray();
        var scope = Require(CosmicQueryScope.Named(adapter.QueryProviderType));
        var access = Require(CosmicQueryReadAccess.OwnerOrPublic(actor));

        // 2. Contains becomes the provider's explicitly enrolled native IN strategy.
        // Arbitrary content predicates are rejected rather than evaluated over a scan.
        var source = Require(CosmicQuery<Holon>.Create(scope)
            .WithReadAccess(access)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, metadataKey))));

        // 3. Deferred LINQ shares the HolonManager planner/executor. The provider owns
        // source reads/decryption; access and the captured page precede publication.
        var query = Require(CosmicLinq.Create(manager, adapter, source, maximumResults: 40));
        var rows = Require(await query.ToCosmicListAsync(cancellationToken));

        await output.WriteLineAsync("\n=== CQL: native multi-record membership ===\n");
        await output.WriteLineAsync($"Provider: {adapter.QueryProviderType}");
        await output.WriteLineAsync($"Selected native keys: {keys.Length}; returned holons: {rows.Count}\n");

        foreach (var row in rows)
        {
            // Print source identity, never remote HTML or private record contents.
            await output.WriteLineAsync($"  OASIS ID: {row.Id}");
            await output.WriteLineAsync($"  Native key: {row.MetaData[metadataKey]}\n");
        }

        return rows;
    }

    private static T Require<T>(OASISResult<T> result)
    {
        if (result == null || result.IsError || result.Result == null)
            throw new InvalidOperationException(result?.ErrorCode + ": " + result?.Message, result?.Exception);

        return result.Result;
    }
}
