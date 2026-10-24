using System.Linq.Expressions;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task NativeGroupedFilterCombinesAggregatesWithoutHydratingEntities()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("coffee", 1L), ("coffee", 2L), ("tea", 3L), ("water", 4L));
        var query = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { Name = g.Key, Rows = g.LongCount(), Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                Min = g.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")), Max = g.Max(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                Mean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock")) })
            .Where(row => row.Rows >= 2 && row.Total == 3L && row.Min == 1L && row.Max == 2L).OrderBy(row => row.Name).Take(1).Result;
        var plan = GroupPlan(provider, query);
        var proof = await ((ICosmicQueryGroupedStorageProvider)provider).ExecuteCosmicGroupedAsync(plan);
        proof.IsError.Should().BeFalse(proof.Message + proof.Exception);
        proof.Result.Select(row => row.IsResult).Should().Equal(true, false, false);
        var result = await CosmicGroupedExecutor.ExecuteAsync(plan, new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var row = result.Result.Providers.Single().Rows.Single();
        row.Name.Should().Be("coffee"); row.Total.Should().Be(3); row.Mean.Should().Be(1.5m);
    }

    [Fact]
    public async Task NativeGroupedFilterPagesMatchingRanksAndComparesSumToCount()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", 2L), ("a", 2L), ("b", -1L), ("c", 4L), ("c", 4L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Rows = g.LongCount(), Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) })
            .Where(row => row.Total > row.Rows).OrderBy(row => row.Key).Skip(1).Take(1).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Single().Key.Should().Be("c");
    }

    [Fact]
    public async Task NativeGroupedNullableFilterAndNegationKeepLiftedSemantics()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", null), ("b", 2L), ("c", 3L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Min = g.Min(h => CosmicQueryMetadata.Value<long?>(h, "stock")) })
            .Where(row => !(row.Min == 2L)).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Select(row => row.Key).Should().Equal("a", "c");
    }

    [Fact]
    public async Task NativeGroupedSumFilterPreservesAdjacentInt64ValuesAndNegativeBorrow()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", 9007199254740992L), ("b", 9007199254740993L), ("negative", -4294967296L), ("negative", -1L));
        var selected = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected.Where(row => row.Total > 9007199254740992m).Result),
            new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Single().Key.Should().Be("b");
        var negative = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected.Where(row => row.Total == -4294967297L).Result),
            new[] { (ICosmicQueryStorageProvider)provider });
        negative.IsError.Should().BeFalse(negative.Message + negative.Exception);
        negative.Result.Providers.Single().Rows.Single().Key.Should().Be("negative");
    }

    [Fact]
    public async Task NativeGroupedMixedFilterKeepsBinary64SubnormalAndFractionalBoundaries()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", -1), ("b", 0), ("c", 1));
        var selected = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<int>(h, "stock")) }).Result;
        var tiny = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected.Where(row => row.Total < double.Epsilon).Result),
            new[] { (ICosmicQueryStorageProvider)provider });
        tiny.IsError.Should().BeFalse(tiny.Message + tiny.Exception);
        tiny.Result.Providers.Single().Rows.Select(row => row.Key).Should().Equal("a", "b");
        var fractional = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected.Where(row => row.Total >= -0.5m).Result),
            new[] { (ICosmicQueryStorageProvider)provider });
        fractional.IsError.Should().BeFalse(fractional.Message + fractional.Exception);
        fractional.Result.Providers.Single().Rows.Select(row => row.Key).Should().Equal("b", "c");
    }

    [Fact]
    public async Task NativeGroupedMembershipAndOrdinalStringsTreatWildcardTextLiterally()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("c_%'", 1L), ("coffee", 1L), ("tea", 1L));
        var names = new[] { "c_%'", "tea" };
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category")).Select(g => g.Key)
            .Where(name => names.Contains(name) && name.StartsWith("c_", StringComparison.Ordinal)
                && name.Contains("%'", StringComparison.Ordinal) && name.EndsWith("_%'", StringComparison.Ordinal)).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Should().Equal("c_%'");
    }

    [Fact]
    public async Task NativeGroupedEmptyStringSuffixDoesNotMatchNull()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, (null, 1L), ("", 1L), ("coffee", 1L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category")).Select(g => g.Key)
            .Where(name => !name.EndsWith("", StringComparison.Ordinal)).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Single().Should().BeNull();
    }

    [Fact]
    public async Task NativeGroupedFalseFilterCannotHideMalformedAggregate()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", 1L), ("z", "invalid"));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Where(total => false).Take(0).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_DATA_INVALID");
    }

    [Fact]
    public async Task NativeGroupedFilterCannotHideOverflowInExcludedSum()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", 1L), ("z", long.MaxValue), ("z", 1L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Where(total => total < 0).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_SCALAR_OVERFLOW");
    }

    [Fact]
    public async Task NativeGroupedFalseFilterStillEnforcesCompleteGroupBudget()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", 1L), ("b", 1L), ("c", 1L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"), CosmicGroupingOptions.Create(2).Result)
            .Select(g => g.LongCount()).Where(rows => false).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_QUERY_LIMIT");
    }

    [Theory]
    [InlineData("Guid")] [InlineData("DateTime")] [InlineData("Enum")] [InlineData("Boolean")]
    public async Task NativeGroupedKeyFilterPreservesExactScalarDomains(string type)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var date = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
        if (type == "Guid") await Check(Guid.Parse("00000000-0000-0000-0000-000000000001"), Guid.Parse("00000000-0000-0000-0000-000000000002"));
        else if (type == "DateTime") await Check(date, date.AddTicks(1));
        else if (type == "Enum") await Check(NextGenSoftware.OASIS.API.Core.Enums.HolonType.Quest, NextGenSoftware.OASIS.API.Core.Enums.HolonType.Cafe);
        else await Check(true, false);
        async Task Check<TValue>(TValue expected, TValue other)
        {
            var ids = await SeedGroups(provider, (expected, 1L), (other, 1L));
            var selected = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<TValue>(h, "category")).Select(g => g.Key).Result;
            var row = Expression.Parameter(typeof(TValue), "key");
            var predicate = Expression.Lambda<Func<TValue, bool>>(Expression.Equal(row, Expression.Constant(expected, typeof(TValue))), row);
            var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected.Where(predicate).Result), new[] { (ICosmicQueryStorageProvider)provider });
            result.IsError.Should().BeFalse(result.Message + result.Exception);
            result.Result.Providers.Single().Rows.Should().Equal(expected);
        }
    }


    [Fact]
    public async Task NativeGroupedMixedBoundariesRemainExactAtBothInt64Limits()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("minimum", long.MinValue), ("middle", 0L), ("maximum", long.MaxValue));
        var selected = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
        var upper = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected.Where(row => row.Total > 9223372036854775806.5m).Result),
            new[] { (ICosmicQueryStorageProvider)provider });
        upper.IsError.Should().BeFalse(upper.Message + upper.Exception);
        upper.Result.Providers.Single().Rows.Single().Total.Should().Be(long.MaxValue);
        var lower = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected.Where(row => row.Total <= -9223372036854775807.5m).Result),
            new[] { (ICosmicQueryStorageProvider)provider });
        lower.IsError.Should().BeFalse(lower.Message + lower.Exception);
        lower.Result.Providers.Single().Rows.Single().Total.Should().Be(long.MinValue);
        var extremes = new[] { long.MinValue, long.MaxValue };
        var members = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected.Where(row => extremes.Contains(row.Total)).Result),
            new[] { (ICosmicQueryStorageProvider)provider });
        members.IsError.Should().BeFalse(members.Message + members.Exception);
        members.Result.Providers.Single().Rows.Should().HaveCount(2);
        var outside = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected.Where(row => row.Total < decimal.MaxValue).Result),
            new[] { (ICosmicQueryStorageProvider)provider });
        outside.IsError.Should().BeFalse(outside.Message + outside.Exception);
        outside.Result.Providers.Single().Rows.Should().HaveCount(3);
    }

    [Fact]
    public async Task NativeGroupedMixedFractionalInequalityRetainsNullRows()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", null), ("b", 2L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Min = g.Min(h => CosmicQueryMetadata.Value<long?>(h, "stock")) })
            .Where(row => row.Min != 0.5m).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Select(row => row.Key).Should().Equal("a", "b");
    }

    [Fact]
    public void NativeGroupedFilterDoesNotBorrowPermissionForNormalizedAverage()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Average(h => h.Version)).Where(mean => mean > 1m).Result;
        SQLLiteDBOASIS.CqlGroupedCapabilities.Admit(query).ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
    }
}
