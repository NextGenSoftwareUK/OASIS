using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(true, true)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(false, false)]
    public async Task NativeGroupedAverageOrdersEveryEnrolledIntegerInput(bool int32, bool nullable)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", nullable ? null : 1L), ("b", 1L), ("b", 2L));
        var grouping = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category")).Result;
        if (nullable)
        {
            var selected = int32
                ? grouping.Select(g => g.Average(h => CosmicQueryMetadata.Value<int?>(h, "stock"))).Result
                : grouping.Select(g => g.Average(h => CosmicQueryMetadata.Value<long?>(h, "stock"))).Result;
            var query = selected.OrderBy(value => value).Result;
            var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
            result.IsError.Should().BeFalse(result.Message + result.Exception);
            result.Result.Providers.Single().Rows.Should().Equal(new decimal?[] { null, 1.5m });
        }
        else
        {
            var selected = int32
                ? grouping.Select(g => g.Average(h => CosmicQueryMetadata.Value<int>(h, "stock"))).Result
                : grouping.Select(g => g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Result;
            var query = selected.OrderByDescending(value => value).Result;
            var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
            result.IsError.Should().BeFalse(result.Message + result.Exception);
            result.Result.Providers.Single().Rows.Should().Equal(1.5m, 1m);
        }
    }

    [Fact]
    public async Task NativeGroupedAverageFiltersThePublishedRoundedDecimal()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("third", 0L), ("third", 0L), ("third", 1L),
            ("two-thirds", 1L), ("two-thirds", 1L), ("two-thirds", 0L), ("negative", -1L));
        var query = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Mean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock")) })
            .Where(row => row.Mean == 1m / 3m).OrderBy(row => row.Mean).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var row = result.Result.Providers.Single().Rows.Single();
        row.Key.Should().Be("third");
        row.Mean.Should().Be(1m / 3m);
    }

    [Fact]
    public async Task NativeGroupedAverageOrderingPreservesOverflowingNumeratorsAndAdjacentInt64()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("maximum", long.MaxValue), ("maximum", long.MaxValue),
            ("half", long.MaxValue), ("half", long.MaxValue - 1),
            ("minimum", long.MinValue), ("minimum", long.MinValue + 1));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Mean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock")) })
            .Where(row => row.Mean > decimal.MinValue && row.Mean < decimal.MaxValue)
            .OrderByDescending(row => row.Mean).Skip(1).Take(1).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var row = result.Result.Providers.Single().Rows.Single();
        row.Key.Should().Be("half");
        row.Mean.Should().Be((decimal)long.MaxValue - 0.5m);
    }

    [Fact]
    public async Task NativeGroupedNullableAverageMembershipAndFieldComparisonUseExactKeys()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("empty", null), ("half", 1L), ("half", 2L), ("whole", 4L));
        var selected = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Mean = g.Average(h => CosmicQueryMetadata.Value<long?>(h, "stock")),
                OtherMean = g.Average(h => CosmicQueryMetadata.Value<int?>(h, "stock")),
                Total = g.Sum(h => CosmicQueryMetadata.Value<long?>(h, "stock")) }).Result;
        decimal?[] values = { null, 1.5m };
        var query = selected.Where(row => values.Contains(row.Mean)).OrderBy(row => row.Mean).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Select(row => row.Key).Should().Equal("empty", "half");

        var comparedIntent = selected.Where(row => row.Mean == row.OtherMean);
        comparedIntent.IsError.Should().BeFalse(comparedIntent.Message);
        var compared = comparedIntent.Result;
        var comparison = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, compared), new[] { (ICosmicQueryStorageProvider)provider });
        comparison.IsError.Should().BeFalse(comparison.Message + comparison.Exception);
        comparison.Result.Providers.Single().Rows.Select(row => row.Key).Should().Equal("empty", "half", "whole");
        // Preserve the shared expression contract: mixed CLR field domains need
        // separate capture/admission work, even though their numeric casts widen.
        selected.Where(row => row.Mean < row.Total).ErrorCode.Should().Be("CQL_EXPRESSION_UNSUPPORTED");
    }

    [Fact]
    public async Task NativeGroupedAverageRoundingTiesAgreeWithDecimalDivide()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        const long baseline = 9000000000000000000L;
        var seed = new List<(object? Key, object? Value)>();
        for (var index = 0; index < 1024; index++)
        {
            seed.Add(("even", baseline + (index == 0 ? 1 : 0)));
            seed.Add(("odd", baseline + (index == 0 ? 3 : 0)));
        }
        await SeedGroups(provider, seed.ToArray());
        // No 2,048-ID membership literal: retain the normal bounded predicate grammar.
        var selected = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned group ", StringComparison.Ordinal))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Mean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
        var even = baseline + 1m / 1024m;
        var odd = baseline + 3m / 1024m;
        var query = selected.Where(row => row.Mean == even || row.Mean == odd).OrderBy(row => row.Mean).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Select(row => row.Mean).Should().Equal(even, odd);
        result.Result.Providers.Single().Rows.Select(row => row.Key).Should().Equal("even", "odd");
    }
}
