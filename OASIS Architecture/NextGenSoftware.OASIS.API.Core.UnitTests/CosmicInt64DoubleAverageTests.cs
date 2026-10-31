using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicInt64DoubleAverageTests
{
    private static CosmicScalarQuery<Holon, double> Query => CosmicQuery<Holon>.Create()
        .AverageAsDouble(row => CosmicQueryMetadata.Value<long>(row, "value")).Result;

    private static CosmicScalarQuery<Holon, double?> NullableQuery => CosmicQuery<Holon>.Create()
        .AverageAsDouble(row => CosmicQueryMetadata.Value<long?>(row, "value")).Result;

    [Fact]
    public void CheckedAverageRequiresItsOwnCapabilityAndAcknowledgement()
    {
        var reads = new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(),
            new[] { typeof(long), typeof(long?) });

        CosmicScalarCapabilities Profile(bool enabled) => new(Array.Empty<CosmicScalarOperation>(),
            new[] { new CosmicScalarSelectionCapability(CosmicScalarOperation.Average, reads,
                new[] { typeof(long), typeof(long?) }, false, enabled) });

        Assert.True(Query.UsesInt64DoubleAverage);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Profile(false).Admit(Query).ErrorCode);
        Assert.False(Profile(true).Admit(Query).IsError);
        Assert.False(Profile(true).Admit(NullableQuery).IsError);
        Assert.True(CosmicNumericScalarResults.FromParts(Query, 0, 1, 1).IsError);

        // Existing fluent Average keeps its exact decimal result contract.
        var exact = CosmicQuery<Holon>.Create()
            .Average(row => CosmicQueryMetadata.Value<long>(row, "value")).Result;
        Assert.False(exact.UsesInt64DoubleAverage);
        Assert.False(Profile(false).Admit(exact).IsError);
        Assert.Equal(1m / 3m, CosmicNumericScalarResults.FromParts(exact, 0, 1, 3).Result);
    }

    [Fact]
    public void OverflowIsAPropertyOfEveryVisitedPrefixNotJustTheFinalSum()
    {
        Assert.Throws<OverflowException>(() => new[] { long.MaxValue, 1L, -long.MaxValue }.Average());
        var overflow = CosmicNumericScalarResults.FromCheckedInt64AverageParts(Query, 0, 1, 3, true);
        Assert.Equal("CQL_SCALAR_OVERFLOW", overflow.ErrorCode);

        var valid = CosmicNumericScalarResults.FromCheckedInt64AverageParts(Query, 0, 1, 3, false);
        Assert.False(valid.IsError, valid.Message);
        Assert.Equal(new[] { -long.MaxValue, 1L, long.MaxValue }.Average(), valid.Result);
    }

    [Fact]
    public void ValidPacketsConvertTheCheckedInt64SumOnceBeforeDivision()
    {
        foreach (var sum in new[] { long.MinValue, long.MaxValue, 9007199254740993L, -7L, 0L })
        {
            var high = sum / CosmicNumericScalarResults.LimbBase;
            var low = sum % CosmicNumericScalarResults.LimbBase;
            var actual = CosmicNumericScalarResults.FromCheckedInt64AverageParts(Query, high, low, 3, false);
            Assert.False(actual.IsError, actual.Message);
            Assert.Equal(BitConverter.DoubleToInt64Bits((double)sum / 3), BitConverter.DoubleToInt64Bits(actual.Result));
        }
    }

    [Fact]
    public void NullableAndEmptyResultsRetainStandardSemantics()
    {
        var empty = CosmicNumericScalarResults.FromCheckedInt64AverageParts(NullableQuery, 0, 0, 0, false);
        Assert.False(empty.IsError);
        Assert.True(empty.IsLoaded);
        Assert.Null(empty.Result);
        Assert.Equal("CQL_EMPTY_SEQUENCE",
            CosmicNumericScalarResults.FromCheckedInt64AverageParts(Query, 0, 0, 0, false).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID",
            CosmicNumericScalarResults.FromCheckedInt64AverageParts(NullableQuery, 0, 0, 0, true).ErrorCode);
    }

    [Fact]
    public void ContradictoryOrUnboundedPacketsCannotBecomeSuccessfulMeans()
    {
        var high = long.MaxValue / CosmicNumericScalarResults.LimbBase;
        var low = long.MaxValue % CosmicNumericScalarResults.LimbBase + 1;
        Assert.Equal("CQL_DATA_INVALID",
            CosmicNumericScalarResults.FromCheckedInt64AverageParts(Query, high, low, 2, false).ErrorCode);
        Assert.Equal("CQL_SCALAR_OVERFLOW",
            CosmicNumericScalarResults.FromCheckedInt64AverageParts(Query, high, low, 2, true).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID",
            CosmicNumericScalarResults.FromCheckedInt64AverageParts(Query, 0, 1, 0, false).ErrorCode);
        Assert.Equal("CQL_QUERY_LIMIT", CosmicNumericScalarResults.FromCheckedInt64AverageParts(Query, 0, 0,
            (long)CosmicNumericScalarResults.MaximumParticipatingValues + 1, false).ErrorCode);
    }
}
