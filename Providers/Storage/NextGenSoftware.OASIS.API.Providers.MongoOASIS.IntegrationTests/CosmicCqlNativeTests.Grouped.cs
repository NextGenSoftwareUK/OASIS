using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    private static CosmicGroupedPlan<T, TKey, TResult> GroupedPlan<T, TKey, TResult>(CosmicGroupedSelection<T, TKey, TResult> query)
        where T : IHolon
    {
        var plan = CosmicGroupedPlanner.Plan(query, Core.Enums.ProviderType.MongoDBOASIS,
            new[] { new CosmicGroupedTarget(Core.Enums.ProviderType.MongoDBOASIS,
                Provider.CqlCapabilities, Provider.CqlGroupedCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        return plan.Result;
    }

    [Fact]
    public Task NativeGroupingCombinesAllAggregateFamiliesWithoutHydration()
        => WithMongoGraph(async fixture =>
        {
            var query = CosmicQuery<ScalarMustNotConstruct>.Create()
                .Where(h => h.Name.StartsWith("owned CQL graph", System.StringComparison.Ordinal))
                .GroupBy(h => h.Version)
                .Select(g => new { g.Key, Count = g.Count(), Rows = g.LongCount(),
                    Min = g.Min(h => h.Version), Max = g.Max(h => h.Version),
                    Total = g.Sum(h => h.Version), Mean = g.Average(h => h.Version) }).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(Assert.Single(result.Result.Providers).Rows);
            Assert.Equal(1, row.Key);
            Assert.Equal(4, row.Count); Assert.Equal(4L, row.Rows);
            Assert.Equal(1, row.Min); Assert.Equal(1, row.Max);
            Assert.Equal(4, row.Total); Assert.Equal(1m, row.Mean);
        });

    [Fact]
    public Task NativeGroupingAppliesSourceWindowBeforeReduction()
        => WithMongoGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL graph", System.StringComparison.Ordinal))
                .OrderBy(h => h.Name).Skip(1).Take(2).GroupBy(h => h.Version)
                .Select(g => new { g.Key, Rows = g.LongCount(), Total = g.Sum(h => h.Version) }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result);
            Assert.Equal(2L, row.InputCount); Assert.Equal(2L, row.Values[1]); Assert.Equal(2, row.Values[2]);
        });

    [Fact]
    public Task NativeGroupingRejectsExcessGroupsWithoutPublishingPartialRows()
        => WithMongoGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned CQL graph", System.StringComparison.Ordinal))
                .GroupBy(h => h.Name).Select(g => new { g.Key, Rows = g.LongCount() }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query), maximumGroups: 1);
            Assert.True(result.IsError); Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode);
            Assert.Null(result.Result);
        });
}
