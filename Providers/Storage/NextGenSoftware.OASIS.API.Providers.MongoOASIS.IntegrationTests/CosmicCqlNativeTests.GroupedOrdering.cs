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
    public Task NativeGroupedOrderingUsesCountsAndCompleteIdentityTies()
        => WithMongoGraph(async fixture =>
        {
            var selected = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL", StringComparison.Ordinal))
                .GroupBy(h => h.HolonType).Select(g => new { g.Key, Rows = g.LongCount() })
                .OrderByDescending(g => g.Rows).ThenBy(g => g.Key).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selected));
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(3, result.Result.Count);
            Assert.Equal(2L, result.Result[0].Values[1]);
            Assert.Equal(new long[] { 2, 1, 1 }, result.Result.Select(row => (long)row.Values[1]));
        });

    [Fact]
    public Task NativeGroupedSumSortRetainsInt64PrecisionBeyondBinary64()
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var selected = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => h.Name).Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) })
                .OrderByDescending(g => g.Total).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selected));
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(new long[] { 9007199254740993, 9007199254740992 }, result.Result.Select(row => (long)row.Values[1]));
        }, configure: root =>
        {
            root.MetaData["stock"] = 9007199254740992L;
            root.Children.First().MetaData["stock"] = 9007199254740993L;
        });
}
