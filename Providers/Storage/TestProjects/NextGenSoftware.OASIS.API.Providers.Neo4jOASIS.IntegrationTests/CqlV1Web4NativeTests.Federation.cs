using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    private static async Task<int> VerifyNativeFederation(
        Dictionary<ProviderType, IOASISStorageProvider> providers, Guid actor, Guid outsider,
        string tag, Guid[] ids)
    {
        // These SDK controls run before hosted usage enrollment, on the same
        // read-only fixture. They do not inflate the signed HTTP/billing totals.
        var adapters = providers.Values.Cast<ICosmicQueryStorageProvider>().ToArray();
        var targets = adapters.Select(adapter => new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities)).ToArray();
        var defaultProvider = targets[0].Provider;
        var access = CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible)
            .WithReadAccess(access).Where(row => CosmicQueryMetadata.Value<string>(row, "fixture") == tag).Result;
        var checks = 0;

        async Task<CosmicFederationResult> Read(CosmicQuery<Holon> query, CosmicFederationOptions? options = null)
        {
            var plan = CosmicFederationPlanner.Plan(query, options ?? CosmicFederationOptions.Create().Result,
                defaultProvider, targets);
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await CosmicFederationExecutor.ExecuteAsync(plan.Result, adapters);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            checks++;
            return result.Result;
        }

        void VisibleCopies(CosmicFederationResult result)
        {
            Assert.AreEqual(9, result.ObservedInputRows);
            Assert.AreEqual(9, result.Rows.Count);
            CollectionAssert.AreEquivalent(new[] { ids[2], ids[3], ids[4] },
                result.Rows.Select(row => row.Selected.Row.Id).Distinct().ToArray());
            foreach (var id in new[] { ids[2], ids[3], ids[4] })
                CollectionAssert.AreEquivalent(providers.Keys.ToArray(),
                    result.Rows.Where(row => row.Selected.Row.Id == id).Select(row => row.Selected.Provider).ToArray());
        }

        var byName = source.OrderBy(row => row.Name).Result;
        VisibleCopies(await Read(byName));

        var page = await Read(byName, CosmicFederationOptions.Create(skip: 2, take: 3).Result);
        CollectionAssert.AreEqual(new[] { ids[2], ids[3], ids[3] }, page.Rows.Select(row => row.Selected.Row.Id).ToArray());
        Assert.AreEqual(9, page.ResolvedRows);

        var priority = new[] { ProviderType.Neo4jOASIS, ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS };
        var resolved = await Read(byName, CosmicFederationOptions.Create(CosmicFederationIdentityMode.ProviderPriority,
            providerPriority: priority).Result);
        CollectionAssert.AreEqual(new[] { ids[2], ids[3], ids[4] }, resolved.Rows.Select(row => row.Selected.Row.Id).ToArray());
        foreach (var row in resolved.Rows)
        {
            Assert.AreEqual(ProviderType.Neo4jOASIS, row.Selected.Provider);
            Assert.AreEqual(3, row.Copies.Count);
            Assert.IsFalse(row.CausalAgreementEstablished);
        }

        async Task Ascending(CosmicQuery<Holon> query)
        {
            var result = await Read(query);
            VisibleCopies(result);
            CollectionAssert.AreEqual(new[] { ids[2], ids[2], ids[2], ids[3], ids[3], ids[3], ids[4], ids[4], ids[4] },
                result.Rows.Select(row => row.Selected.Row.Id).ToArray());
        }

        // Adjacent exact values expose rounding through double or text codecs.
        await Ascending(source.OrderBy(row => CosmicQueryMetadata.Value<decimal>(row, "price")).Result);
        await Ascending(source.OrderByDescending(row => CosmicQueryMetadata.Value<ulong>(row, "counter")).Result);
        await Ascending(source.OrderBy(row => CosmicQueryMetadata.Value<float>(row, "singleReading")).Result);
        await Ascending(source.OrderBy(row => CosmicQueryMetadata.Value<double>(row, "reading")).Result);
        await Ascending(source.OrderBy(row => CosmicQueryMetadata.Value<long>(row, "extremum")).Result);
        await Ascending(source.OrderBy(row => CosmicQueryMetadata.Value<DateTime>(row, "observedAt")).Result);

        VisibleCopies(await Read(source.OrderBy(row => CosmicQueryMetadata.Value<Guid>(row, "guid")).Result));
        VisibleCopies(await Read(source.OrderBy(row => CosmicQueryMetadata.Value<long?>(row, "nullable")).Result));

        var publicOnly = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(outsider).Result)
            .Where(row => CosmicQueryMetadata.Value<string>(row, "fixture") == tag).Result;
        var publicRows = await Read(publicOnly);
        Assert.AreEqual(3, publicRows.Rows.Count);
        Assert.IsTrue(publicRows.Rows.All(row => row.Selected.Row.Id == ids[3]));

        var limited = CosmicFederationPlanner.Plan(byName,
            CosmicFederationOptions.Create(maximumInputRows: 1).Result, defaultProvider, targets);
        Assert.IsFalse(limited.IsError, limited.Message);
        var rejected = await CosmicFederationExecutor.ExecuteAsync(limited.Result, adapters);
        Assert.AreEqual("CQL_FEDERATION_INPUT_LIMIT", rejected.ErrorCode);
        Assert.IsNull(rejected.Result);
        checks++;

        // Native source windows stay independent of the global output window.
        var sourcePage = await Read(byName.Skip(1).Take(1).Result,
            CosmicFederationOptions.Create(skip: 1, take: 1).Result);
        Assert.AreEqual(3, sourcePage.ObservedInputRows);
        Assert.AreEqual(ids[3], sourcePage.Rows.Single().Selected.Row.Id);
        Assert.AreEqual(providers.Keys.OrderBy(provider => provider).ElementAt(1), sourcePage.Rows.Single().Selected.Provider);
        return checks;
    }
}
