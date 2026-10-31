using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Fact]
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
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result.Providers.Single().Rows);
            Assert.Equal(14L, row.Integer); Assert.Equal(2.5m, row.Decimal);
            Assert.Equal(2.5f, row.Single); Assert.Equal(2.5d, row.Double);
            Assert.Equal(1.25f, row.SingleMean); Assert.Equal(1.25d, row.DoubleMean);
        });

    [Fact]
    public Task NativeFloatingGroupedInputWorkIsIndependentOfGraphPublicationLimit()
        => WithDecimalNumericRows(Enumerable.Repeat((object)1d, 137).ToArray(), async (fixture, ids) =>
        {
            var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot, maximumNodes: 1).Result;
            var query = CosmicQuery<Holon>.Create().WithChildren(graph)
                .Where(h => h.Name.StartsWith("owned CQL graph decimal", StringComparison.Ordinal))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric"))).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(137d, Assert.Single(result.Result.Providers.Single().Rows));
        });
}
