using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [TestMethod]
    public Task NativeFloatingGroupedMixedDomainsRetainSlotAlignment()
        => WithDecimalNumericRows(new object[] { 1.25m, 1.25m }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new
                {
                    Integer = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                    Decimal = g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")),
                    Single = g.Sum(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")),
                    Double = g.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")),
                    SingleMean = g.Average(h => CosmicQueryMetadata.Value<float>(h, "decimalNumeric")),
                    DoubleMean = g.Average(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric"))
                })
                .Where(g => g.SingleMean == 1.25f && g.DoubleMean == 1.25d).Result;
            var result = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Providers.Single().Rows.Single();
            Assert.AreEqual(14L, row.Integer); Assert.AreEqual(2.5m, row.Decimal);
            Assert.AreEqual(2.5f, row.Single); Assert.AreEqual(2.5d, row.Double);
            Assert.AreEqual(1.25f, row.SingleMean); Assert.AreEqual(1.25d, row.DoubleMean);
        });

    [TestMethod]
    public Task NativeFloatingGroupedInputWorkIsIndependentOfGraphPublicationLimit()
        => WithDecimalNumericRows(Enumerable.Repeat((object)1d, 137).ToArray(), async (fixture, ids) =>
        {
            var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderDefault, maximumNodes: 1).Result;
            var query = CosmicQuery<Holon>.Create().WithChildren(graph)
                .Where(h => h.Name.StartsWith("owned CQL graph decimal", StringComparison.Ordinal))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric"))).Result;
            var result = await GraphManager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(137d, result.Result.Providers.Single().Rows.Single());
        });
}
