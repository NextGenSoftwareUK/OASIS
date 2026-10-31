using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedParticipationTests
{
    [Theory]
    [InlineData(typeof(int), 2, 2)] [InlineData(typeof(long), 2, 2)]
    [InlineData(typeof(ulong), 2, 2)] [InlineData(typeof(decimal), 2, 2)]
    [InlineData(typeof(double), 2, 2)] [InlineData(typeof(float), 2, 2)]
    [InlineData(typeof(Guid), 2, 2)] [InlineData(typeof(DateTime), 2, 2)]
    [InlineData(typeof(HolonType), 2, 2)] [InlineData(typeof(string), 2, 0)]
    [InlineData(typeof(string), 2, 1)] [InlineData(typeof(long?), 2, 0)]
    [InlineData(typeof(long?), 2, 1)] [InlineData(typeof(Guid?), 2, 0)]
    [InlineData(typeof(decimal?), 2, 1)]
    public void ValidAggregateCoverageUsesDeclaredNullability(Type type, long inputs, long present)
    {
        var result = CosmicGroupedResults.ValidateParticipation(type, inputs, present);
        Assert.False(result.IsError, result.Message); Assert.True(result.Result);
    }

    [Theory]
    [InlineData(typeof(int), 0, 0)] [InlineData(typeof(int?), 0, 0)]
    [InlineData(typeof(string), -1, 0)] [InlineData(typeof(long), 1, -1)]
    [InlineData(typeof(long?), 1, -1)] [InlineData(typeof(long?), 1, 2)]
    [InlineData(typeof(string), 1, 2)] [InlineData(typeof(long), 2, 0)]
    [InlineData(typeof(long), 2, 1)] [InlineData(typeof(Guid), 2, 1)]
    [InlineData(typeof(decimal), 2, 0)] [InlineData(typeof(HolonType), 2, 1)]
    public void ContradictoryGroupCoverageIsInvalidData(Type type, long inputs, long present)
    {
        var result = CosmicGroupedResults.ValidateParticipation(type, inputs, present);
        Assert.True(result.IsError); Assert.Equal("CQL_DATA_INVALID", result.ErrorCode);
    }

    [Theory]
    [InlineData(null)] [InlineData(typeof(object))] [InlineData(typeof(string[]))]
    public void UndeclaredDomainsCannotUseParticipationValidationAsAdmission(Type type)
    {
        var result = CosmicGroupedResults.ValidateParticipation(type, 1, 1);
        Assert.True(result.IsError); Assert.Equal("CQL_PLAN_INVALID", result.ErrorCode);
    }
}
