using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedOrderingAdmissionTests
{
    private static CosmicProjectionCapabilities Fields => new(new[]
    { new CosmicProjectionFieldCapability("Name", typeof(string)), new CosmicProjectionFieldCapability("Version", typeof(int)) }, Array.Empty<Type>());
    private static CosmicGroupedCapabilities Profile(CosmicGroupedOrderingCapabilities? ordering = null) => new(Fields,
        new CosmicScalarCapabilities(new[] { CosmicScalarOperation.LongCount },
            new[] { new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, Fields) }), ordering: ordering);

    [Fact]
    public void AggregateOrderingRequiresBothOperationAndExactResultDomain()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { Rows = g.LongCount(), Total = g.Sum(h => h.Version) }).Result;
        var ordering = new CosmicGroupedOrderingCapabilities(Array.Empty<Type>(),
            new[] { new CosmicGroupedAggregateOrderingCapability(CosmicScalarOperation.LongCount, typeof(long)) });
        Assert.False(Profile(ordering).Admit(query.OrderBy(g => g.Rows).Result).IsError);
        Assert.True(Profile(ordering).Admit(query.OrderBy(g => g.Total).Result).IsError);
        Assert.True(Profile().Admit(query.OrderBy(g => g.Rows).Result).IsError);
    }

    [Fact]
    public void KeyAndAggregateOrderingPermissionsAreIndependentAndSnapshotted()
    {
        var keys = new[] { typeof(string) };
        var aggregates = new[] { new CosmicGroupedAggregateOrderingCapability(CosmicScalarOperation.LongCount, typeof(long)) };
        var ordering = new CosmicGroupedOrderingCapabilities(keys, aggregates);
        keys[0] = typeof(int); aggregates[0] = new CosmicGroupedAggregateOrderingCapability(CosmicScalarOperation.Count, typeof(int));
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => new { Name = g.Key, Rows = g.LongCount() }).Result;
        Assert.False(Profile(ordering).Admit(query.OrderBy(g => g.Name).ThenBy(g => g.Rows).Result).IsError);
        var onlyCounts = new CosmicGroupedOrderingCapabilities(Array.Empty<Type>(), ordering.Aggregates);
        Assert.True(Profile(onlyCounts).Admit(query.OrderBy(g => g.Name).Result).IsError);
    }

    [Fact]
    public void ActualAdapterCannotBorrowClaimedOrderingPermission()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.LongCount()).OrderBy(g => g).Result;
        var rows = new CosmicQueryCapabilities(CosmicQueryFeatures.Paging, Array.Empty<CosmicQueryFieldCapability>());
        var ordering = new CosmicGroupedOrderingCapabilities(Array.Empty<Type>(),
            new[] { new CosmicGroupedAggregateOrderingCapability(CosmicScalarOperation.LongCount, typeof(long)) });
        var plan = CosmicGroupedPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { new CosmicGroupedTarget(ProviderType.MongoDBOASIS, rows, Profile(ordering)) });
        Assert.False(plan.IsError, plan.Message);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicGroupedPlanner.Admit(plan.Result,
            ProviderType.MongoDBOASIS, rows, Profile()).ErrorCode);
    }

    [Fact]
    public void ImpossibleDeclarationsAndExcessSortColumnsReject()
    {
        Assert.Throws<ArgumentException>(() => new CosmicGroupedAggregateOrderingCapability(CosmicScalarOperation.Any, typeof(bool)));
        Assert.Throws<ArgumentException>(() => new CosmicGroupedAggregateOrderingCapability(CosmicScalarOperation.LongCount, typeof(int)));
        Assert.Throws<ArgumentException>(() => new CosmicGroupedOrderingCapabilities(new[] { typeof(object) }, Array.Empty<CosmicGroupedAggregateOrderingCapability>()));
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => new { Name = g.Key, Rows = g.LongCount() }).Result;
        var ordering = new CosmicGroupedOrderingCapabilities(new[] { typeof(string) },
            new[] { new CosmicGroupedAggregateOrderingCapability(CosmicScalarOperation.LongCount, typeof(long)) }, maximumColumns: 1);
        Assert.True(Profile(ordering).Admit(query.OrderBy(g => g.Name).ThenBy(g => g.Rows).Result).IsError);
    }
}
