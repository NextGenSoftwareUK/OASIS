using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task NativeGroupingCompositeKeysRetainTheirExactDomains()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("coffee", 1L), ("tea", 9L), ("coffee", 2L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => new { Category = CosmicQueryMetadata.Value<string>(h, "category"), h.Version })
            .Select(g => new { g.Key.Category, g.Key.Version, Count = g.Count() }).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var rows = result.Result.Providers.Single().Rows;
        rows.Select(r => r.Category).Should().Equal("coffee", "tea");
        rows.Select(r => r.Count).Should().Equal(2, 1);
        rows[0].Version.Should().Be(0);
    }

    [Fact]
    public async Task NativeGroupingNullKeysAndNullableEmptyAggregatesRemainTyped()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, (null, null), (null, null), ("tea", 3L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Count = g.Count(), Sum = g.Sum(h => CosmicQueryMetadata.Value<long?>(h, "stock")),
                Average = g.Average(h => CosmicQueryMetadata.Value<long?>(h, "stock")) }).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var rows = result.Result.Providers.Single().Rows;
        rows[0].Key.Should().BeNull(); rows[0].Count.Should().Be(2); rows[0].Sum.Should().Be(0); rows[0].Average.Should().BeNull();
        rows[1].Average.Should().Be(3m);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NativeGroupingRejectsWrongKeyOrAggregateTypes(bool badKey)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = badKey ? await SeedGroups(provider, (123L, 1L)) : await SeedGroups(provider, ("coffee", "1"));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.ErrorCode.Should().Be("CQL_DATA_INVALID"); result.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task NativeGroupingRejectsOverflowButAverageRetainsAnOverflowingNumerator()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("coffee", long.MaxValue), ("coffee", long.MaxValue));
        var grouped = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"));
        var sum = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, grouped.Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Result), new[] { (ICosmicQueryStorageProvider)provider });
        sum.ErrorCode.Should().Be("CQL_SCALAR_OVERFLOW");
        var average = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, grouped.Select(g => g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Result), new[] { (ICosmicQueryStorageProvider)provider });
        average.IsError.Should().BeFalse(average.Message + average.Exception);
        average.Result.Providers.Single().Rows.Single().Should().Be((decimal)long.MaxValue);
    }

    [Fact]
    public async Task NativeGroupingInputIsNotTruncatedByGroupOutputBudget()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        await SeedGroups(provider, Enumerable.Range(1, 137).Select(n => ((object?)"coffee", (object?)(long)n)).ToArray());
        var query = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned group ", StringComparison.Ordinal))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"), CosmicGroupingOptions.Create(1).Result)
            .Select(g => new { Count = g.LongCount(), Sum = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                Mean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var row = result.Result.Providers.Single().Rows.Single();
        row.Count.Should().Be(137); row.Sum.Should().Be(9453); row.Mean.Should().Be(69m);
    }

    [Fact]
    public async Task NativeGroupingExcessGroupsAreNotAPartialSuccess()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("coffee", 1L), ("tea", 2L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"), CosmicGroupingOptions.Create(1).Result)
            .Select(g => g.Count()).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeTrue(); result.Result.Should().BeNull();
    }

    [Fact]
    public async Task NativeGroupingGuidRepresentationsShareOneLogicalKey()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id = Guid.NewGuid(); var ids = await SeedGroups(provider, (id.ToString("D"), 1L), (id.ToString("D").ToUpperInvariant(), 2L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).GroupBy(h => CosmicQueryMetadata.Value<Guid>(h, "category"))
            .Select(g => new { g.Key, Count = g.Count() }).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Single().Key.Should().Be(id);
        result.Result.Providers.Single().Rows.Single().Count.Should().Be(2);
    }

    [Fact]
    public async Task NativeGroupingUnicodeOrderMatchesTheCoreScalarContract()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("\U00010000", 1L), ("\uE000", 2L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => g.Key).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Should().Equal("\uE000", "\U00010000");
    }
    [Fact]
    public async Task NativeGroupingVisibilityPrecedesOverflowAndParticipation()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actor = Guid.NewGuid(); var foreign = Guid.NewGuid(); var ids = new List<Guid>();
        foreach (var item in new[] { (actor, false, 7L), (foreign, false, long.MaxValue), (foreign, true, -1L) })
        {
            var row = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = item.Item1, IsPublic = item.Item2,
                MetaData = new() { ["category"] = "coffee", ["stock"] = item.Item3 } };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse(); ids.Add(row.Id);
        }
        var keys = ids.ToArray();
        var query = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
            .Where(h => keys.Contains(h.Id)).GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { Count = g.Count(), Sum = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                Mean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var grouped = result.Result.Providers.Single().Rows.Single();
        grouped.Count.Should().Be(2); grouped.Sum.Should().Be(6); grouped.Mean.Should().Be(3m);
    }

    [Fact]
    public async Task NativeGroupingEmptyWindowAndPreCancellationDoNotFabricateGroups()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var query = CosmicQuery<Holon>.Create().Take(0).GroupBy(h => h.Name).Select(g => g.Count()).Result;
        var plan = GroupPlan(provider, query);
        var result = await CosmicGroupedExecutor.ExecuteAsync(plan, new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Should().BeEmpty();
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var cancelled = await CosmicGroupedExecutor.ExecuteAsync(plan, new[] { (ICosmicQueryStorageProvider)provider }, cancellation.Token);
        cancelled.ErrorCode.Should().Be("CQL_QUERY_CANCELLED");
    }

    [Fact]
    public async Task NativeGroupingDateKeysNormalizeEquivalentUtcFractions()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("2026-10-23T00:00:00.1Z", 1L), ("2026-10-23T00:00:00.1000000Z", 2L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).GroupBy(h => CosmicQueryMetadata.Value<DateTime>(h, "category"))
            .Select(g => new { g.Key, Count = g.Count() }).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { (ICosmicQueryStorageProvider)provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var group = result.Result.Providers.Single().Rows.Single();
        group.Count.Should().Be(2); group.Key.Kind.Should().Be(DateTimeKind.Utc);
        group.Key.Should().Be(new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(100));
    }

    [Fact]
    public async Task NativeGroupingInactiveProviderReturnsAnExplicitError()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        var result = await provider.ExecuteCosmicGroupedAsync(GroupPlan(provider, query));
        result.ErrorCode.Should().Be("CQL_PROVIDER_NOT_READY");
    }

}
