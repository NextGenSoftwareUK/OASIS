using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>Native Mongo plus an explicitly bound Solana index; no native-shaped index adapter.</summary>
public static class MixedFederationWalkthrough
{
    public static async Task<OASISResult<CosmicFederationResult>> RunAsync(
        HolonManager manager,
        ICosmicQueryStorageProvider mongo,
        ICosmicChainQueryIndexReader index,
        ICosmicChainQuerySourceObserver observer,
        CosmicChainQueryReadRequirement requirement,
        Guid actor,
        Guid readableId,
        string selectedName,
        CancellationToken cancellationToken = default)
    {
        // 1. Freeze the common predicate and explicit provider scope. Mongo reads
        // its native records; Solana reads its declared, independently checked index.
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(
            ProviderType.MongoDBOASIS, ProviderType.SolanaOASIS).Result)
            .Where(row => row.Name == selectedName);
        if (source.IsError) return Failure(source.ErrorCode, source.Message);

        // 2. Each source receives its own trusted catalogue. The index requirement
        // binds source/network/index/schema/finality and freshness, not just a DB.
        var policies = new[]
        {
            new CosmicFederationReadPolicy(ProviderType.MongoDBOASIS,
                CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.MongoDBOASIS, new[] { readableId }).Result),
            new CosmicFederationReadPolicy(ProviderType.SolanaOASIS,
                CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.SolanaOASIS, new[] { readableId }).Result)
        };
        var options = CosmicFederationOptions.Create(take: 4, maximumInputRows: 4,
            maximumRowsPerProvider: 2, maximumProviderAttempts: 2,
            maximumDurationMilliseconds: 30000).Result;
        var plan = CosmicFederationPlanner.Plan(source.Result, options, ProviderType.MongoDBOASIS,
            new[]
            {
                new CosmicQueryTarget(ProviderType.MongoDBOASIS, mongo.QueryCapabilities),
                new CosmicQueryTarget(ProviderType.SolanaOASIS, index.IndexedRowCapabilities, index.Source)
            }, policies, new[] { new CosmicFederationIndexRequirement(requirement) });
        if (plan.IsError) return Failure(plan.ErrorCode, plan.Message);

        // 3. The mixed executor keeps native adapters separate from indexed readers.
        // It preserves per-copy receipts and per-read assurance, even for zero rows.
        // Failed freshness/visibility/source checks fail the whole query.
        return await manager.ExecuteMixedFederationQueryAsync(plan.Result,
            new[] { mongo }, new[] { new CosmicFederationIndexedReader(index, observer) }, cancellationToken);
    }

    private static OASISResult<CosmicFederationResult> Failure(string code, string message)
        => new() { IsError = true, ErrorCode = code, Message = message };
}
