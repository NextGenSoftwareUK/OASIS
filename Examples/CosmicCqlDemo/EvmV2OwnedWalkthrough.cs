using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

/// <summary>
/// Read existing owned holons through the shared V2 EVM provider.
/// The caller supplies an activated shipping facade and trusted record catalogue.
/// This example never deploys a contract, seeds a chain or changes routing settings.
/// </summary>
public static class EvmV2OwnedWalkthrough
{
    public static async Task<(int Full, int Subset)> RunAsync(
        HolonManager manager,
        Web3CoreOASISBaseProvider provider,
        IEnumerable<Guid> authorizedIds,
        Guid actor,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // 1. Use the real V2 facade selected by the application. The type here
        // deliberately excludes the old Ethereum provider and its different ABI.
        // These three existing live IDs come from trusted application policy,
        // never directly from an untrusted browser's requested IDs.
        var ids = authorizedIds.ToArray();
        if (actor == Guid.Empty || ids.Length != 3 || ids.Any(id => id == Guid.Empty)
            || ids.Distinct().Count() != 3)
            throw new ArgumentException("Supply an actor and three distinct authorized existing IDs.");

        // 2. Select one concrete authority. The access catalogue constrains every
        // captured business predicate; knowing a GUID alone grants no read access.
        var scope = Require(CosmicQueryScope.Named(provider.QueryProviderType));
        var access = Require(CosmicQueryReadAccess.AuthoritativeRecords(
            actor, provider.QueryProviderType, ids));
        var source = Require(CosmicQuery<Holon>.Create(scope).WithReadAccess(access));
        var query = Require(CosmicLinq.Create(manager, provider, source, maximumResults: 3));

        // 3. Materialization goes through HolonManager and the production V2
        // canonical read/decryption path. No managed full-chain scan is performed.
        var full = Require(await query.ToCosmicListAsync(cancellationToken));

        // 4. LINQ Contains captures bounded native key selection. Missing/deleted
        // records remain absent; this example does not invent replacement rows.
        var selected = ids.Take(2).ToArray();
        var subset = Require(await query.Where(row => selected.Contains(row.Id))
            .ToCosmicListAsync(cancellationToken));

        await output.WriteLineAsync("\n=== Owned holons: Web3Core V2 CQL ===\n");
        await output.WriteLineAsync($"Provider: {provider.QueryProviderType}; all: {full.Count}; selected: {subset.Count}\n");
        foreach (var row in subset)
            await output.WriteLineAsync($"  {row.Id:D}  {row.Name}");

        await output.WriteLineAsync("\nIndependent native key observations; no cross-provider snapshot or finality guarantee.\n");
        return (full.Count, subset.Count);
    }

    public static Task<CosmicFederationResult> RunWithMongoAsync(
        HolonManager manager,
        ICosmicQueryStorageProvider mongo,
        Web3CoreOASISBaseProvider provider,
        IEnumerable<Guid> selectedIds,
        IEnumerable<Guid> authorizedMongoIds,
        IEnumerable<Guid> authorizedChainIds,
        Guid actor,
        TextWriter output,
        CancellationToken cancellationToken = default)
        // Reuse the existing teaching preset: separate trusted policies, explicit
        // Mongo + chain scope, bounded complete reads, physical-copy provenance.
        => IdentityFederationWalkthrough.RunAsync(manager, mongo, provider,
            selectedIds, authorizedMongoIds, authorizedChainIds, actor, output, cancellationToken);

    private static T Require<T>(OASISResult<T> result)
    {
        if (result.IsError || result.Result == null)
            throw new InvalidOperationException(result.ErrorCode + ": " + result.Message, result.Exception);
        return result.Result;
    }
}
