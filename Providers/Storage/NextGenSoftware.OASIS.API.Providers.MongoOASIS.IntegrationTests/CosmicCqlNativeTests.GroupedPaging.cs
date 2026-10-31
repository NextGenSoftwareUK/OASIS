using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Fact]
    public Task NativeGroupedPageRetainsCompleteProofAndNativeSelectedRanks()
        => WithMongoGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal))
                .GroupBy(h => h.HolonType).Select(g => new { g.Key, Rows = g.LongCount() })
                .OrderByDescending(g => g.Rows).ThenBy(g => g.Key).Skip(1).Take(1).Result;
            var plan = GroupedPlan(query);
            var proof = await fixture.Provider.ExecuteCosmicGroupedAsync(plan);
            Assert.False(proof.IsError, proof.Message + proof.Exception);
            Assert.Equal(new[] { false, true, false }, proof.Result.Select(row => row.IsResult));
            Assert.Equal(new long[] { 2, 1, 1 }, proof.Result.Select(row => row.InputCount));
            var published = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(plan,
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(published.IsError, published.Message + published.Exception);
            Assert.Equal(proof.Result[1].Keys[0], Assert.Single(Assert.Single(published.Result.Providers).Rows).Key);
        });

    [Theory]
    [InlineData(0)] [InlineData(1)]
    public Task NativeGroupedPageCannotHideOffPageMalformedAggregate(int take)
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).GroupBy(h => h.Name)
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Take(take).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.True(result.IsError); Assert.Equal("CQL_DATA_INVALID", result.ErrorCode); Assert.Null(result.Result);
        }, configure: root =>
        {
            root.MetaData["stock"] = "invalid off-page integer";
            root.Children.First().MetaData["stock"] = 1L;
        });

    [Fact]
    public Task NativeGroupedPageCannotHideOverflowInSkippedGroup()
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id, fixture.Root.Children.Last().Id };
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Skip(1).Take(1).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.True(result.IsError); Assert.Equal("CQL_SCALAR_OVERFLOW", result.ErrorCode); Assert.Null(result.Result);
        }, configure: root =>
        {
            root.MetaData["category"] = "a"; root.MetaData["stock"] = long.MaxValue;
            root.Children.First().MetaData["category"] = "a"; root.Children.First().MetaData["stock"] = 1L;
            root.Children.Last().MetaData["category"] = "z"; root.Children.Last().MetaData["stock"] = 2L;
        });

    [Fact]
    public Task NativeEmptyGroupedPageStillEnforcesCompleteGroupBudget()
        => WithMongoGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal))
                .GroupBy(h => h.Name, CosmicGroupingOptions.Create(2).Result).Select(g => g.LongCount()).Take(0).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.True(result.IsError); Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode); Assert.Null(result.Result);
        });

    [Fact]
    public Task NativeGroupedPageKeepsSourceAndResultWindowsIndependent()
        => WithMongoGraph(async fixture =>
        {
            var selected = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal))
                .OrderBy(h => h.Name).Take(2).GroupBy(h => h.Name).Select(g => new { g.Key, Rows = g.LongCount() }).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(selected.Skip(1).Take(1).Result),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal("owned CQL graph child 1", Assert.Single(Assert.Single(result.Result.Providers).Rows).Key);
            var empty = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(selected.Take(0).Result),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(empty.IsError, empty.Message); Assert.Empty(Assert.Single(empty.Result.Providers).Rows);
        });
}
