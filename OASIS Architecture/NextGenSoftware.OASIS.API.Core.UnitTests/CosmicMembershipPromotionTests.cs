using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicMembershipPromotionTests
{
    [Fact]
    public void SingleWideningPreservesDistinctDoubleMembersAndStorageGuard()
    {
        var members = new[] { 0.1d, (double)0.1f };
        var query = CosmicQuery<Holon>.Create().Where(h => members.Contains((double)CosmicQueryMetadata.Value<float>(h, "reading")));
        Assert.False(query.IsError, query.Message);
        var predicate = Assert.IsType<CosmicQueryMembership>(Assert.Single(query.Result.Predicates));
        Assert.Equal(typeof(float), predicate.Path.ValueType);
        Assert.Equal(0.1d, predicate.Values[0].Value);
        Assert.Equal((double)0.1f, predicate.Values[1].Value);
        Assert.NotEqual(predicate.Values[0].Value, predicate.Values[1].Value);
    }

    [Fact]
    public void IntegralWideningPreservesNarrowStoredDomainAndInt64Bounds()
    {
        var members = new[] { long.MinValue, 7L, long.MaxValue };
        var query = CosmicQuery<Holon>.Create().Where(h => members.Contains((long)CosmicQueryMetadata.Value<int>(h, "count")));
        Assert.False(query.IsError, query.Message);
        var predicate = Assert.IsType<CosmicQueryMembership>(Assert.Single(query.Result.Predicates));
        Assert.Equal(typeof(int), predicate.Path.ValueType);
        Assert.Equal(long.MinValue, predicate.Values[0].Value);
        Assert.Equal(long.MaxValue, predicate.Values[2].Value);
    }

    [Fact]
    public void PromotionCannotChangeNullableEnumOrFractionalContracts()
    {
        double?[] nullable = { null, 0.1d };
        Assert.True(CosmicQuery<Holon>.Create().Where(h => nullable.Contains((double?)CosmicQueryMetadata.Value<float>(h, "reading"))).IsError);
        var underlying = new[] { (int)HolonType.Quest };
        Assert.True(CosmicQuery<Holon>.Create().Where(h => underlying.Contains((int)h.HolonType)).IsError);
        var fractional = new[] { 1.5m };
        Assert.True(CosmicQuery<Holon>.Create().Where(h => fractional.Contains((decimal)CosmicQueryMetadata.Value<ulong>(h, "counter"))).IsError);
    }
}
