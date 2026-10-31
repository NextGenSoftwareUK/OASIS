using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private static CosmicGroupedPlan<T, TKey, TResult> GroupPlan<T, TKey, TResult>(SQLLiteDBOASIS provider,
        CosmicGroupedSelection<T, TKey, TResult> query) where T : IHolon
    {
        var adapter = (ICosmicQueryStorageProvider)provider;
        var claims = SQLLiteDBOASIS.CqlGroupedCapabilities;
        var result = CosmicGroupedPlanner.Plan(query, ProviderType.SQLLiteDBOASIS,
            new[] { new CosmicGroupedTarget(ProviderType.SQLLiteDBOASIS, adapter.QueryCapabilities, claims) });
        result.IsError.Should().BeFalse(result.Message);
        return result.Result;
    }

    private static async Task<Guid[]> SeedGroups(SQLLiteDBOASIS provider, params (object? Key, object? Value)[] inputs)
    {
        var ids = new List<Guid>();
        for (var index = 0; index < inputs.Length; index++)
        {
            var row = new Holon { Id = Guid.NewGuid(), Name = "owned group " + index,
                MetaData = new() { ["category"] = inputs[index].Key!, ["stock"] = inputs[index].Value! } };
            var saved = await provider.SaveHolonAsync(row, saveChildren: false);
            saved.IsError.Should().BeFalse(saved.Message);
            ids.Add(row.Id);
        }
        return ids.ToArray();
    }

    [Fact]
    public async Task NativeGroupingReducesMultipleAggregatesWithoutHydration()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("coffee", 1L), ("tea", 9L), ("coffee", 2L));
        var query = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Count = g.Count(), Rows = g.LongCount(),
                Min = g.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                Max = g.Max(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                Sum = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                Mean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var groups = result.Result.Providers.Single().Rows;
        groups.Should().HaveCount(2);
        groups[0].Key.Should().Be("coffee"); groups[0].Count.Should().Be(2); groups[0].Rows.Should().Be(2);
        groups[0].Min.Should().Be(1); groups[0].Max.Should().Be(2); groups[0].Sum.Should().Be(3); groups[0].Mean.Should().Be(1.5m);
        groups[1].Key.Should().Be("tea"); groups[1].Sum.Should().Be(9);
    }

    [Fact]
    public async Task NativeGroupingAppliesTheSourceWindowBeforeReduction()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("coffee", 1L), ("tea", 9L), ("coffee", "outside window"));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).OrderBy(h => h.Name).Take(2)
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Sum = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Select(r => r.Sum).Should().Equal(1L, 9L);
    }

    [Fact]
    public async Task NativeGroupingKeepsFinalSumsWhenIntermediateInt64TotalsOverflow()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("coffee", long.MaxValue), ("coffee", 1L), ("coffee", -1L));
        var supported = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")));
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, supported.Result), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Single().Should().Be(long.MaxValue);
    }
}
