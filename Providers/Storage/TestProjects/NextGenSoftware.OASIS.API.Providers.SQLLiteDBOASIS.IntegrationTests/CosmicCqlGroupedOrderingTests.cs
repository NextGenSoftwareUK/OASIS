using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GroupedSumOrderingPreservesAdjacentInt64ValuesAndNegativeBorrow(bool descending)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        // These totals expose floating-point rounding and truncation-versus-floor mistakes.
        var ids = await SeedGroups(provider, ("large", long.MaxValue), ("adjacent", long.MaxValue - 1),
            ("negative", -4294967296L), ("negative", -1L), ("zero", 0L));
        var selected = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
        var ordered = descending ? selected.OrderByDescending(g => g.Total).Result : selected.OrderBy(g => g.Total).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, ordered), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var expected = new[] { "negative", "zero", "adjacent", "large" };
        result.Result.Providers.Single().Rows.Select(r => r.Key).Should().Equal(descending ? expected.Reverse() : expected);
    }

    [Fact]
    public async Task GroupedCountOrderingUsesCompleteIdentityForTies()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("z", 1L), ("a", 1L), ("many", 1L), ("many", 2L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Count = g.Count() }).OrderByDescending(g => g.Count).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Select(r => r.Key).Should().Equal("many", "a", "z");
    }

    [Fact]
    public void GroupedOrderingDoesNotBorrowSumPermissionForAverage()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { Total = g.Sum(h => h.Version), Mean = g.Average(h => h.Version) }).Result;
        SQLLiteDBOASIS.CqlGroupedCapabilities.Admit(query.OrderBy(g => g.Total).Result).IsError.Should().BeFalse();
        SQLLiteDBOASIS.CqlGroupedCapabilities.Admit(query.OrderBy(g => g.Mean).Result).ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
    }
}
