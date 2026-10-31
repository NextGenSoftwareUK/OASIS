using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>
/// The same bounded IN query through HolonManager and LINQ on IPFS, SOLID,
/// Mastodon or explicit public Solana/Google/Ethereum/Polygon/Arbitrum/Cosmos/Polkadot/NEAR/Base/Avalanche/BNB metadata profiles.
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
            ProviderType.SolanaOASIS => "SolanaAccountAddress",
            ProviderType.GoogleCloudOASIS => "GCSObjectKey",
            ProviderType.EthereumOASIS => "EthereumAccountAddress",
            ProviderType.PolygonOASIS => "PolygonAccountAddress",
            ProviderType.ArbitrumOASIS => "ArbitrumAccountAddress",
            ProviderType.CosmosBlockChainOASIS => "CosmosValidatorAddress",
            ProviderType.PolkadotOASIS => "PolkadotBlockHash",
            ProviderType.NEAROASIS => "NearAccountId",
            ProviderType.BaseOASIS => "BaseAccountAddress",
            ProviderType.OptimismOASIS => "OptimismAccountAddress",
            ProviderType.LineaOASIS => "LineaAccountAddress",
            ProviderType.ScrollOASIS => "ScrollAccountAddress",
            ProviderType.AvalancheOASIS => "AvalancheAccountAddress",
            ProviderType.BNBChainOASIS => "BNBChainAccountAddress",
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

    /// <summary>One query over Mongo plus one enrolled protocol provider; preserve physical-copy provenance.</summary>
    public static async Task<CosmicFederationResult> RunFederatedAsync(
        HolonManager manager,
        ICosmicQueryStorageProvider mongo,
        ICosmicQueryStorageProvider protocol,
        IEnumerable<string> nativeKeys,
        Guid actor,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        if (mongo.QueryProviderType != ProviderType.MongoDBOASIS)
            throw new ArgumentException("The first target must be the explicitly configured Mongo provider.", nameof(mongo));
        if (protocol.QueryProviderType is not (ProviderType.IPFSOASIS or ProviderType.SOLIDOASIS or ProviderType.ActivityPubOASIS or ProviderType.SolanaOASIS or ProviderType.GoogleCloudOASIS))
            throw new ArgumentException("Select IPFS, SOLID, Mastodon or public Solana/Google metadata as the second target.", nameof(protocol));
        var descriptor = protocol.QueryCapabilities.StringKeySetLookup
            ?? throw new ArgumentException("Enroll native membership on the protocol target before planning.", nameof(protocol));
        var keys = nativeKeys.ToArray();
        var metadataKey = descriptor.MetadataKey;
        var access = Require(CosmicQueryReadAccess.OwnerOrPublic(actor));

        // 1. Both providers receive exactly the same captured IN predicate. Mongo
        // records must carry the corresponding native key as metadata to match.
        // Scope resolves only these two supplied adapters; it is not a failover list.
        var source = Require(CosmicQuery<Holon>
            .Create(Require(CosmicQueryScope.Named(mongo.QueryProviderType, protocol.QueryProviderType)))
            .WithReadAccess(access)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, metadataKey))));

        // 2. Read complete bounded sources, then merge globally. Preserve physical
        // copies so the demo visibly proves native participation on both backends.
        // No native ordering or distributed repeatable snapshot is claimed here.
        var options = Require(CosmicFederationOptions.Create(
            take: 80,
            maximumInputRows: 80,
            maximumRowsPerProvider: 40,
            maximumProviderAttempts: 2,
            maximumDurationMilliseconds: 30000));
        var adapters = new[] { mongo, protocol };
        var targets = adapters.Select(adapter => new CosmicQueryTarget(
            adapter.QueryProviderType, adapter.QueryCapabilities)).ToArray();
        var plan = Require(CosmicFederationPlanner.Plan(source, options, mongo.QueryProviderType, targets));

        // 3. The shared executor dispatches both native queries. An error in either
        // source fails this query; it never substitutes the other provider's rows.
        var result = Require(await manager.ExecuteFederationQueryAsync(plan, adapters, cancellationToken));
        await output.WriteLineAsync("\n=== CQL: Mongo + protocol federation ===\n");
        await output.WriteLineAsync($"Observed physical rows: {result.ObservedInputRows}; returned: {result.Rows.Count}\n");
        foreach (var row in result.Rows)
            await output.WriteLineAsync($"  {row.Selected.Provider}: {row.Selected.Row.Id}\n");

        return result;
    }

    private static T Require<T>(OASISResult<T> result)
    {
        if (result == null || result.IsError || result.Result == null)
            throw new InvalidOperationException(result?.ErrorCode + ": " + result?.Message, result?.Exception);

        return result.Result;
    }
}
