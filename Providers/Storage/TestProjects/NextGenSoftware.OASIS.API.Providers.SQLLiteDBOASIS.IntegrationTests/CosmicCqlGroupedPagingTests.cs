using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task NativeGroupedPageUsesOrderedRanksAndRetainsEveryProofPacket()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", 1L), ("b", 2L), ("b", 3L), ("c", 4L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Rows = g.LongCount() }).OrderByDescending(g => g.Rows).Skip(1).Take(1).Result;
        var plan = GroupPlan(provider, query);
        var proof = await ((ICosmicQueryGroupedStorageProvider)provider).ExecuteCosmicGroupedAsync(plan);
        proof.IsError.Should().BeFalse(proof.Message);
        proof.Result.Select(row => row.IsResult).Should().Equal(false, true, false);
        proof.Result.Select(row => row.InputCount).Should().Equal(2L, 1L, 1L);
        var result = await CosmicGroupedExecutor.ExecuteAsync(plan, new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Single().Key.Should().Be("a");
    }

    [Theory]
    [InlineData(0)] [InlineData(1)]
    public async Task GroupedPageCannotHideMalformedOffPageAggregate(int take)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", 1L), ("z", "invalid"));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Take(take).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_DATA_INVALID");
    }

    [Fact]
    public async Task GroupedPageCannotHideFinalOverflowInSkippedGroup()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", long.MaxValue), ("a", 1L), ("z", 1L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Skip(1).Take(1).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_SCALAR_OVERFLOW");
    }

    [Fact]
    public async Task EmptyGroupedPageStillEnforcesCompleteGroupBudget()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", 1L), ("b", 1L), ("c", 1L));
        var options = CosmicGroupingOptions.Create(2).Result;
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"), options)
            .Select(g => g.LongCount()).Take(0).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_QUERY_LIMIT");
    }

    [Fact]
    public async Task SourceWindowAndResultWindowRemainIndependentAtNativeExecution()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", 1L), ("b", 2L), ("z", "outside source"));
        var selected = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).OrderBy(h => h.Name).Take(2)
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Sum = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected.Skip(1).Take(1).Result), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Single().Sum.Should().Be(2L);
        var empty = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected.Take(0).Result), new[] { (ICosmicQueryStorageProvider)provider });
        empty.IsError.Should().BeFalse(empty.Message); empty.Result.Providers.Single().Rows.Should().BeEmpty();
    }
}
