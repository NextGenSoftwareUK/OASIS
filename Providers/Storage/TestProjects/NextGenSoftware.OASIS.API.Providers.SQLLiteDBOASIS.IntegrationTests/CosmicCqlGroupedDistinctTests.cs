using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private static CosmicGroupedSelection<Holon, string, CosmicProjectionRecord> DistinctSelection<T>(
        Guid[] ids, Expression<Func<Holon, T>> selector, int maximumGroups = 1000)
    {
        var group = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"),
                CosmicGroupingOptions.Create(maximumGroups).Result).Result;
        return group.SelectColumns(new[]
        {
            CosmicGroupedBinding<Holon, string>.Key(group, "Category", 0).Result,
            CosmicGroupedBinding<Holon, string>.CountValues(group, "Present", selector).Result,
            CosmicGroupedBinding<Holon, string>.CountValues(group, "Unique", selector, true).Result
        }).Result;
    }

    [Fact]
    public async Task NativeGroupedDistinctKeepsOrdinalUnicodeAndEmbeddedNull()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", "A"), ("a", "a"), ("a", "λ"),
            ("a", "λ"), ("a", "x\0y"), ("a", "x"), ("a", null));
        var selected = DistinctSelection(ids, h => CosmicQueryMetadata.Value<string>(h, "stock"));
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected),
            new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var row = result.Result.Providers.Single().Rows.Single();
        row["Present"].Should().Be(6L);
        row["Unique"].Should().Be(5L);
    }

    [Fact]
    public async Task NativeGroupedDistinctPreservesAdjacentInt64AndAllNullGroups()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", 9007199254740992L), ("a", 9007199254740993L),
            ("a", 9007199254740993L), ("a", null), ("b", null));
        var selected = DistinctSelection(ids, h => CosmicQueryMetadata.Value<long?>(h, "stock"));
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected),
            new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var rows = result.Result.Providers.Single().Rows;
        rows.Should().HaveCount(2);
        rows.Single(row => (string)row["Category"] == "a")["Unique"].Should().Be(2L);
        rows.Single(row => (string)row["Category"] == "b")["Unique"].Should().Be(0L);
    }

    [Fact]
    public async Task NativeGroupedDistinctCannotHideInvalidOffPageValues()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", 1L), ("z", "wrong domain"));
        var selected = DistinctSelection(ids, h => CosmicQueryMetadata.Value<long?>(h, "stock")).Take(0).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected),
            new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("CQL_DATA_INVALID");
    }

    [Fact]
    public async Task NativeGroupedDistinctCannotHideExcessGroupsBehindEmptyPage()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", 1L), ("b", 1L));
        var selected = DistinctSelection(ids, h => CosmicQueryMetadata.Value<long?>(h, "stock"), 1).Take(0).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, selected),
            new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("CQL_QUERY_LIMIT");
    }
}
