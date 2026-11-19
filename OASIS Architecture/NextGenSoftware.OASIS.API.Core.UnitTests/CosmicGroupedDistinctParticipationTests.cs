using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedDistinctParticipationTests
{
    [Fact]
    public void RequiredDistinctCanHaveFewerValuesThanInputs()
    {
        foreach (var type in new[] { typeof(bool), typeof(int), typeof(long), typeof(float), typeof(double) })
            Assert.False(CosmicGroupedResults.ValidateParticipation(type, 3, 2, true).IsError);
    }

    [Fact]
    public void RequiredDistinctStillRejectsZeroNegativeAndExcessCardinality()
    {
        foreach (var count in new long[] { -1, 0, 4 })
            Assert.Equal("CQL_DATA_INVALID", CosmicGroupedResults.ValidateParticipation(typeof(int), 3, count, true).ErrorCode);
        Assert.True(CosmicGroupedResults.ValidateParticipation(typeof(int), 0, 0, true).IsError);
    }

    [Fact]
    public void NullableDistinctCanHaveNoPresentValues()
    {
        foreach (var type in new[] { typeof(string), typeof(bool?), typeof(int?), typeof(long?), typeof(float?), typeof(double?) })
            Assert.False(CosmicGroupedResults.ValidateParticipation(type, 3, 0, true).IsError);
    }

    [Fact]
    public void ExistingOrdinaryRequiredParticipationStillRequiresEveryInput()
    {
        Assert.True(CosmicGroupedResults.ValidateParticipation(typeof(int), 3, 2).IsError);
        Assert.True(CosmicGroupedResults.ValidateParticipation(typeof(int), 3, 2, false).IsError);
        Assert.False(CosmicGroupedResults.ValidateParticipation(typeof(int), 3, 3).IsError);
        Assert.False(CosmicGroupedResults.ValidateParticipation(typeof(int?), 3, 2).IsError);
    }

    [Fact]
    public void MaterializerAcceptsDistinctRequiredCountBelowOrdinaryCount()
    {
        var grouping = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        var query = grouping.SelectColumns(new[] {
            CosmicGroupedBinding<Holon, string>.CountValues(grouping, "Present", h => h.Version).Result,
            CosmicGroupedBinding<Holon, string>.CountValues(grouping, "Unique", h => h.Version, true).Result
        }).Result;
        var result = CosmicGroupedResults.Materialize(query,
            new[] { new CosmicGroupedRow(3, new object[] { "same" }, 3L, 2L) });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(2L, result.Result.Single()["Unique"]);
    }

    [Fact]
    public void MaterializerRejectsZeroDistinctForRequiredValues()
    {
        var grouping = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        var query = grouping.SelectColumns(new[] {
            CosmicGroupedBinding<Holon, string>.CountValues(grouping, "Unique", h => h.Version, true).Result
        }).Result;
        var result = CosmicGroupedResults.Materialize(query,
            new[] { new CosmicGroupedRow(3, new object[] { "same" }, 0L) });
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode);
    }
}
