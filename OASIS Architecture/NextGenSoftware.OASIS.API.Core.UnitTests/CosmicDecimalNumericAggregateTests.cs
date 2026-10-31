using System.Globalization;
using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicDecimalNumericAggregateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DecimalSumCapturePreservesItsExactInputDomain(bool optional)
    {
        var source = CosmicQuery<Holon>.Create();
        if (optional)
        {
            var sum = source.Sum(h => CosmicQueryMetadata.Value<decimal?>(h, "price"));
            Assert.False(sum.IsError, sum.Message);
            Assert.Equal(typeof(decimal?), sum.Result.Selector.ValueType);
        }
        else
        {
            var sum = source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "price"));
            Assert.False(sum.IsError, sum.Message);
            Assert.Equal(typeof(decimal), sum.Result.Selector.ValueType);
        }
    }
    [Theory]
    [InlineData("1.25", "2.75", "4")]
    [InlineData("0.0000000000000000000000000001", "0.0000000000000000000000000002", "0.0000000000000000000000000003")]
    [InlineData("79228162514264337593543950335", "-79228162514264337593543950335", "0")]
    [InlineData("79228162514264337593543950335", "-0.1", "79228162514264337593543950335")]
    public void SumPreservesScaleAndRoundsOnlyTheFinalRepresentableResult(string first, string second, string expected)
    {
        var values = new[] { Parse(first), Parse(second) };
        var result = CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(decimal), Parts(values), 2);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(Parse(expected), Assert.IsType<decimal>(result.Result));
    }

    [Fact]
    public void CancellationRetainsTheSmallestDecimalAfterAnOversizedIntermediate()
    {
        var values = new[] { decimal.MaxValue, decimal.MaxValue, decimal.MinValue, decimal.MinValue, 0.0000000000000000000000000001m };
        var result = CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(decimal), Parts(values), values.Length);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(values[4], Assert.IsType<decimal>(result.Result));
    }

    [Fact]
    public void SumOverflowDoesNotInvalidateAnAverageWithAnOversizedNumerator()
    {
        var packet = Parts(decimal.MaxValue, decimal.MaxValue);
        var sum = CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(decimal), packet, 2);
        Assert.Equal("CQL_SCALAR_OVERFLOW", sum.ErrorCode);
        var mean = CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Average, typeof(decimal), packet, 2);
        Assert.False(mean.IsError, mean.Message);
        Assert.Equal(decimal.MaxValue, Assert.IsType<decimal>(mean.Result));
    }

    [Theory]
    [InlineData("0.0000000000000000000000000001", "0", "0")]
    [InlineData("0.0000000000000000000000000003", "0", "0.0000000000000000000000000002")]
    [InlineData("-0.0000000000000000000000000003", "0", "-0.0000000000000000000000000002")]
    [InlineData("1", "2", "1.5")]
    [InlineData("79228162514264337593543950335", "79228162514264337593543950334", "79228162514264337593543950334")]
    public void AverageRoundsTheExactRationalToEvenWithoutDoubleRounding(string first, string second, string expected)
    {
        var packet = Parts(Parse(first), Parse(second));
        var result = CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Average, typeof(decimal), packet, 2);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(Parse(expected), Assert.IsType<decimal>(result.Result));
    }

    [Fact]
    public void EmptyNullableSumAndAverageHaveDifferentContracts()
    {
        var zero = new long[7];
        var sum = CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(decimal?), zero, 0);
        Assert.False(sum.IsError, sum.Message);
        Assert.Equal(0m, sum.Result);
        var optional = CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Average, typeof(decimal?), zero, 0);
        Assert.False(optional.IsError, optional.Message);
        Assert.Null(optional.Result);
        Assert.Equal("CQL_EMPTY_SEQUENCE", CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Average, typeof(decimal), zero, 0).ErrorCode);
    }

    [Theory]
    [InlineData(-1L, "CQL_DATA_INVALID")]
    [InlineData(2147483648L, "CQL_QUERY_LIMIT")]
    public void InvalidAndExcessiveCountsFailBeforeArithmetic(long count, string code)
        => Assert.Equal(code, CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(decimal), new long[7], count).ErrorCode);

    [Fact]
    public void WrongPacketsCannotBecomeRoundedSuccesses()
    {
        Assert.Equal("CQL_DATA_INVALID", CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(decimal), new long[6], 1).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID", CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(decimal), new long[8], 1).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID", CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(decimal), null!, 1).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID", CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(decimal), new long[] { 0,0,0,0,0,0,1 }, 0).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID", CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(decimal), new long[] { 0,0,0,0,0,0,long.MaxValue }, 1).ErrorCode);
        Assert.Equal("CQL_PLAN_INVALID", CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Count, typeof(decimal), new long[7], 1).ErrorCode);
        Assert.Equal("CQL_PLAN_INVALID", CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(long), new long[7], 1).ErrorCode);
    }

    [Fact]
    public void MaximumWorkCountRetainsTheExactLargestAverage()
    {
        const long count = int.MaxValue;
        var packet = Parts(decimal.MaxValue).Select(part => checked(part * count)).ToArray();
        var result = CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Average, typeof(decimal), packet, count);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(decimal.MaxValue, result.Result);
    }

    [Fact]
    public void CaptureDoesNotEnrollLegacyArithmeticOrAcceptAnIntegerPacket()
    {
        var source = CosmicQuery<Holon>.Create();
        var query = source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "price")).Result;
        Assert.Equal("CQL_PLAN_INVALID", CosmicNumericScalarResults.FromParts(query, 0, 0, 0).ErrorCode);
        var reads = new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), new[] { typeof(decimal), typeof(decimal?) });
        var legacy = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, reads),
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Average, reads)
        });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", legacy.Admit(query).ErrorCode);
        var permission = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, reads, new[] { typeof(decimal) })
        });
        Assert.False(permission.Admit(query).IsError);
        var normalized = CosmicDecimalScalarResults.FromParts(query, Parts(1.25m), 1);
        Assert.False(normalized.IsError, normalized.Message);
        Assert.Equal(1.25m, normalized.Result);
    }

    [Fact]
    public void ScalarAndGroupedAverageCapturePreserveDecimalAndNullableTypes()
    {
        var source = CosmicQuery<Holon>.Create();
        Assert.False(source.Average(h => CosmicQueryMetadata.Value<decimal>(h, "price")).IsError);
        Assert.False(source.Average(h => CosmicQueryMetadata.Value<decimal?>(h, "price")).IsError);
        Assert.False(source.Where(h => h.IsActive).Average(h => CosmicQueryMetadata.Value<decimal>(h, "price")).IsError);
        Assert.False(source.Where(h => h.IsActive).Average(h => CosmicQueryMetadata.Value<decimal?>(h, "price")).IsError);
        var grouped = source.GroupBy(h => h.Name).Select(g => new
        {
            Total = g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "price")),
            Mean = g.Average(h => CosmicQueryMetadata.Value<decimal>(h, "price")),
            Optional = g.Average(h => CosmicQueryMetadata.Value<decimal?>(h, "price"))
        });
        Assert.False(grouped.IsError, grouped.Message);
        Assert.Equal(new[] { typeof(decimal), typeof(decimal), typeof(decimal?) }, grouped.Result.Columns.Select(column => column.ValueType));
    }

    [Fact]
    public void SeededReferenceSamplesMatchClrWithExactlyRepresentableNumerators()
    {
        var random = new Random(20261025);
        for (var sample = 0; sample < 2048; sample++)
        {
            // All inputs share a scale and small coefficients, so CLR addition
            // is exact here. Its division is an independent rounding reference.
            var scale = (byte)(sample % 29);
            var values = Enumerable.Range(0, 2 + sample % 7).Select(_ =>
                new decimal(unchecked((int)(uint)random.NextInt64(0, (long)uint.MaxValue + 1)),
                    0, 0, random.Next(2) == 0, scale)).ToArray();
            var packet = Parts(values);
            var expectedSum = values.Sum();
            var sum = CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(decimal), packet, values.Length);
            var mean = CosmicDecimalScalarResults.FromParts(CosmicScalarOperation.Average, typeof(decimal), packet, values.Length);
            Assert.False(sum.IsError, sum.Message);
            Assert.False(mean.IsError, mean.Message);
            Assert.Equal(expectedSum, sum.Result);
            Assert.Equal(decimal.Divide(expectedSum, values.Length), mean.Result);
        }
    }

    [Fact]
    public void GroupedDecimalAveragePublicationUsesTheDecimalInputRange()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => g.Average(h => CosmicQueryMetadata.Value<decimal>(h, "price"))).Result;
        var result = CosmicGroupedResults.Materialize(query,
            new[] { new CosmicGroupedRow(2, new object[] { "one" }, decimal.MaxValue) });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(decimal.MaxValue, Assert.Single(result.Result));
        var permission = new CosmicGroupedAggregateOrderingCapability(CosmicScalarOperation.Sum, typeof(decimal));
        Assert.Equal(typeof(decimal), permission.ValueType);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DecimalEmptyFacadeUsesTheDecimalPacketOwner(bool optional)
    {
        var source = CosmicQuery<Holon>.Create();
        if (optional)
        {
            var query = source.Sum(h => CosmicQueryMetadata.Value<decimal?>(h, "price")).Result;
            var result = CosmicSelectedScalarResults.FromValue(query, null!, false);
            Assert.False(result.IsError, result.Message);
            Assert.Equal((decimal?)0m, result.Result);
        }
        else
        {
            var query = source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "price")).Result;
            var result = CosmicSelectedScalarResults.FromValue(query, null!, false);
            Assert.False(result.IsError, result.Message);
            Assert.Equal(0m, result.Result);
        }
    }

    [Theory]
    [InlineData("UInt64", "-1")]
    [InlineData("UInt64", "18446744073709551616")]
    [InlineData("Int32", "2147483648")]
    [InlineData("Int32", "-2147483649")]
    [InlineData("Int64", "9223372036854775808")]
    [InlineData("Int64", "-9223372036854775809")]
    public void ScalarAveragePublicationRejectsValuesOutsideTheInputDomain(string input, string value)
    {
        var source = CosmicQuery<Holon>.Create();
        var query = input switch
        {
            "UInt64" => source.Average(h => CosmicQueryMetadata.Value<ulong>(h, "value")).Result,
            "Int32" => source.Average(h => CosmicQueryMetadata.Value<int>(h, "value")).Result,
            _ => source.Average(h => CosmicQueryMetadata.Value<long>(h, "value")).Result
        };
        var result = CosmicSelectedScalarResults.FromValue(query, Parse(value), true);
        Assert.Equal("CQL_DATA_INVALID", result.ErrorCode);
        Assert.False(result.IsLoaded);
    }

    private static decimal Parse(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    // Independent test fixture encoder from CLR Decimal bits. Native providers
    // must produce the same scaled packet from their own persisted domains.
    private static long[] Parts(params decimal[] values)
    {
        var result = new long[7];
        foreach (var value in values)
        {
            var bits = decimal.GetBits(value);
            var units = (new BigInteger((uint)bits[0]) | new BigInteger((uint)bits[1]) << 32 | new BigInteger((uint)bits[2]) << 64)
                * BigInteger.Pow(10, 28 - ((bits[3] >> 16) & 255));
            var sign = value < 0 ? -1 : 1;
            for (var index = 6; index >= 0; index--)
            {
                units = BigInteger.DivRem(units, 1000000000, out var digit);
                result[index] += sign * (long)digit;
            }
            Assert.Equal(BigInteger.Zero, units);
        }
        return result;
    }

}
