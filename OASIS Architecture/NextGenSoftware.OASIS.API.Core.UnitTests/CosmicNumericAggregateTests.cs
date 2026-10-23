using System.Globalization;
using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicNumericAggregateTests
{
    private static readonly CosmicProjectionCapabilities Reads = new(Array.Empty<CosmicProjectionFieldCapability>(),
        new[] { typeof(int), typeof(int?), typeof(long), typeof(long?) });
    private static readonly CosmicScalarCapabilities Numeric = new(Array.Empty<CosmicScalarOperation>(), new[]
    {
        new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, Reads),
        new CosmicScalarSelectionCapability(CosmicScalarOperation.Average, Reads)
    });
    private static CosmicQuery<Holon> Source => CosmicQuery<Holon>.Create();

    private static (long High, long Low, long Count) Packet(params long[] values)
    {
        BigInteger high = 0, low = 0;
        foreach (var value in values) { high += value / 4294967296L; low += value % 4294967296L; }
        return ((long)high, (long)low, values.LongLength);
    }

    [Fact]
    public void NumericIntentPreservesSourceAccessWindowAndResultDomains()
    {
        var actor = Guid.NewGuid();
        var source = Source.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).OrderBy(h => h.Name).Skip(3).Take(7).Result;
        var sum = source.Sum(h => CosmicQueryMetadata.Value<long?>(h, "balance"));
        var average = source.Average(h => CosmicQueryMetadata.Value<long?>(h, "balance"));
        Assert.False(sum.IsError, sum.Message); Assert.False(average.IsError, average.Message);
        Assert.Same(source, sum.Result.Source); Assert.Same(source, average.Result.Source);
        Assert.Equal(typeof(long?), sum.Result.Selector.ValueType);
        Assert.IsType<CosmicScalarQuery<Holon, decimal?>>(average.Result);
        Assert.Equal(actor, average.Result.Source.ReadAccess.ActorId); Assert.Equal(3, average.Result.Source.Offset);
    }

    [Theory]
    [InlineData(0L, 0L, 0L, 0L)]
    [InlineData(0L, -1L, 1L, -1L)]
    [InlineData(2147483647L, 4294967295L, 1L, long.MaxValue)]
    [InlineData(-2147483648L, 0L, 1L, long.MinValue)]
    public void SumExactBoundaryPackets(long high, long low, long count, long expected)
    {
        var query = Source.Sum(h => CosmicQueryMetadata.Value<long>(h, "value")).Result;
        var result = CosmicNumericScalarResults.FromParts(query, high, low, count);
        Assert.False(result.IsError, result.Message); Assert.True(result.IsLoaded); Assert.Equal(expected, result.Result);
    }

    [Fact]
    public void SumUsesMathematicalTotalAndDoesNotOverflowAnIntermediateInt64()
    {
        var p = Packet(long.MaxValue, 1, -1);
        var query = Source.Sum(h => CosmicQueryMetadata.Value<long>(h, "value")).Result;
        Assert.Equal(long.MaxValue, CosmicNumericScalarResults.FromParts(query, p.High, p.Low, p.Count).Result);
        var cancellation = Packet(long.MinValue, long.MaxValue);
        Assert.Equal(-1L, CosmicNumericScalarResults.FromParts(query, cancellation.High, cancellation.Low, cancellation.Count).Result);
    }

    [Theory]
    [InlineData(long.MaxValue, 1L)] [InlineData(long.MinValue, -1L)]
    public void SumRejectsTrueFinalOverflow(long first, long second)
    {
        var p = Packet(first, second);
        var query = Source.Sum(h => CosmicQueryMetadata.Value<long>(h, "value")).Result;
        var result = CosmicNumericScalarResults.FromParts(query, p.High, p.Low, p.Count);
        Assert.Equal("CQL_SCALAR_OVERFLOW", result.ErrorCode); Assert.False(result.IsLoaded);
    }

    [Fact]
    public void SumChecksInt32AndNullableEmptyReturnsZero()
    {
        var sum = Source.Sum(h => h.Version).Result;
        Assert.Equal("CQL_SCALAR_OVERFLOW", CosmicNumericScalarResults.FromParts(sum, 0, (long)int.MaxValue + 1, 2).ErrorCode);
        var optional = Source.Sum(h => CosmicQueryMetadata.Value<int?>(h, "value")).Result;
        var empty = CosmicNumericScalarResults.FromParts(optional, 0, 0, 0);
        Assert.False(empty.IsError); Assert.Equal(0, empty.Result);
        Assert.Equal(0, CosmicSelectedScalarResults.FromValue(optional, null, false).Result);
    }

    [Theory]
    [InlineData(1L, 3L, "0.3333333333333333333333333333")]
    [InlineData(-1L, 3L, "-0.3333333333333333333333333333")]
    [InlineData(1L, 6L, "0.1666666666666666666666666667")]
    [InlineData(1L, 536870912L, "0.0000000018626451492309570312")]
    [InlineData(3L, 536870912L, "0.0000000055879354476928710938")]
    public void AverageUsesExactRationalAndRoundToEven(long numerator, long count, string expected)
    {
        var query = Source.Average(h => CosmicQueryMetadata.Value<long>(h, "value")).Result;
        var result = CosmicNumericScalarResults.FromParts(query, 0, numerator, count);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), result.Result);
    }

    [Fact]
    public void AveragePreservesLargeIntegersEvenWhenSumWouldOverflow()
    {
        var query = Source.Average(h => CosmicQueryMetadata.Value<long>(h, "value")).Result;
        foreach (var pair in new[] { (new[] { long.MaxValue, long.MaxValue }, (decimal)long.MaxValue),
            (new[] { 9007199254740993L, 9007199254740995L }, 9007199254740994m),
            (new[] { long.MinValue, long.MaxValue }, -0.5m) })
        {
            var p = Packet(pair.Item1); var result = CosmicNumericScalarResults.FromParts(query, p.High, p.Low, p.Count);
            Assert.False(result.IsError, result.Message); Assert.Equal(pair.Item2, result.Result);
        }
    }

    [Fact]
    public void AverageRequiredAndNullableEmptyHaveDistinctResults()
    {
        var required = Source.Average(h => h.Version).Result;
        Assert.Equal("CQL_EMPTY_SEQUENCE", CosmicNumericScalarResults.FromParts(required, 0, 0, 0).ErrorCode);
        var optional = Source.Average(h => CosmicQueryMetadata.Value<long?>(h, "value")).Result;
        var empty = CosmicNumericScalarResults.FromParts(optional, 0, 0, 0);
        Assert.False(empty.IsError); Assert.Null(empty.Result); Assert.True(empty.IsLoaded);
    }

    [Theory]
    [InlineData(0L, 1L, 0L)] [InlineData(1L, 0L, 0L)] [InlineData(0L, 0L, -1L)]
    [InlineData(long.MaxValue, 0L, 1L)] [InlineData(0L, long.MinValue, 1L)]
    public void ImpossiblePacketsNeverPublish(long high, long low, long count)
    {
        var query = Source.Sum(h => CosmicQueryMetadata.Value<long>(h, "value")).Result;
        var result = CosmicNumericScalarResults.FromParts(query, high, low, count);
        Assert.Equal("CQL_DATA_INVALID", result.ErrorCode); Assert.False(result.IsLoaded);
    }

    [Fact]
    public void WorkLimitIsAnErrorAndNeverAHiddenInputTruncation()
    {
        var query = Source.Sum(h => CosmicQueryMetadata.Value<long>(h, "value")).Result;
        Assert.Equal("CQL_QUERY_LIMIT", CosmicNumericScalarResults.FromParts(query, 0, 0, (long)int.MaxValue + 1).ErrorCode);
    }

    [Fact]
    public void NumericSelectorsAndPermissionsAreExplicit()
    {
        Assert.True(Source.Sum(h => h.Name).IsError);
        Assert.True(Source.Sum(h => h.IsActive).IsError);
        Assert.True(Source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "value")).IsError);
        var query = Source.Sum(h => CosmicQueryMetadata.Value<long>(h, "value")).Result;
        Assert.False(Numeric.Admit(query).IsError);
        var counts = new CosmicScalarCapabilities(CosmicScalarOperation.Count, CosmicScalarOperation.LongCount);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", counts.Admit(query).ErrorCode);
        var extrema = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
            { new CosmicScalarSelectionCapability(CosmicScalarOperation.Min, Reads) });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", extrema.Admit(query).ErrorCode);
        var wrong = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
            { new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum,
                new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), new[] { typeof(int) })) });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", wrong.Admit(query).ErrorCode);
        Assert.Throws<ArgumentException>(() => new CosmicScalarCapabilities(CosmicScalarOperation.Sum));
    }
    [Fact]
    public void MaximumWorkBoundKeepsTheFullAverageNumeratorExactInDecimal()
    {
        var query = Source.Average(h => CosmicQueryMetadata.Value<long>(h, "value")).Result;
        const long count = int.MaxValue;
        var minimum = CosmicNumericScalarResults.FromParts(query, -2147483648L * count, 0, count);
        Assert.False(minimum.IsError, minimum.Message); Assert.Equal((decimal)long.MinValue, minimum.Result);
        var maximum = CosmicNumericScalarResults.FromParts(query, 2147483647L * count, 4294967295L * count, count);
        Assert.False(maximum.IsError, maximum.Message); Assert.Equal((decimal)long.MaxValue, maximum.Result);
    }

}
