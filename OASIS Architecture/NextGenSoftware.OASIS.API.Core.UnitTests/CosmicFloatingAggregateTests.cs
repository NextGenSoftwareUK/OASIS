using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicFloatingAggregateTests
{
    private static Type Domain(bool single, bool optional = false) => single
        ? (optional ? typeof(float?) : typeof(float)) : (optional ? typeof(double?) : typeof(double));
    private static ulong Maximum(bool single) => single ? 0x7F7FFFFFUL : 0x7FEFFFFFFFFFFFFFUL;
    private static ulong Sign(bool single) => single ? 1UL << 31 : 1UL << 63;
    private static int Width(bool single) => single ? 10 : 70;

    // Independent input decomposition reads IEEE fields directly. Native stores
    // must eventually produce this same signed packet, not an input-row buffer.
    private static BigInteger Units(ulong raw, bool single)
    {
        var fractionBits = single ? 23 : 52;
        var magnitude = raw & ~Sign(single);
        var exponent = (int)(magnitude >> fractionBits);
        var fraction = new BigInteger(magnitude & ((1UL << fractionBits) - 1));
        var units = exponent == 0 ? fraction : ((BigInteger.One << fractionBits) + fraction) << (exponent - 1);
        return (raw & Sign(single)) == 0 ? units : -units;
    }

    private static long[] Words(BigInteger units, bool single)
    {
        var magnitude = BigInteger.Abs(units);
        var words = new long[Width(single)];
        for (var index = words.Length - 1; index >= 0; index--)
        {
            magnitude = BigInteger.DivRem(magnitude, 1L << 30, out var digit);
            words[index] = units.Sign < 0 ? -(long)digit : (long)digit;
        }
        Assert.Equal(BigInteger.Zero, magnitude);
        return words;
    }

    private static (long[] Parts, BigInteger Total) Packet(bool single, params ulong[] inputs)
    {
        var parts = new long[Width(single)]; var total = BigInteger.Zero;
        foreach (var raw in inputs)
        {
            var units = Units(raw, single); total += units;
            var words = Words(units, single);
            for (var index = 0; index < parts.Length; index++) parts[index] = checked(parts[index] + words[index]);
        }
        return (parts, total);
    }

    // Reference oracle: binary-search the complete finite positive IEEE value
    // space, then compare exact rational distances to the two neighbors. This
    // does not use the production exponent/quantum/coefficient rounding method.
    private static ulong? Nearest(BigInteger total, long denominator, bool single)
    {
        var numerator = BigInteger.Abs(total);
        ulong lower = 0, upper = Maximum(single);
        while (lower < upper)
        {
            var middle = lower + (upper - lower + 1) / 2;
            if (Units(middle, single) * denominator <= numerator) lower = middle;
            else upper = middle - 1;
        }
        var below = numerator - Units(lower, single) * denominator;
        var next = lower == Maximum(single) ? BigInteger.One << (single ? 277 : 2098) : Units(lower + 1, single);
        var above = next * denominator - numerator;
        var chooseUpper = above < below || above == below && (lower & 1) != 0;
        if (chooseUpper && lower == Maximum(single)) return null;
        var result = chooseUpper ? lower + 1 : lower;
        return total.Sign < 0 && result != 0 ? result | Sign(single) : result;
    }

    private static void Check(bool single, CosmicScalarOperation operation, long[] parts, BigInteger total, long count)
    {
        var expected = Nearest(total, operation == CosmicScalarOperation.Sum ? 1 : count, single);
        var actual = CosmicFloatingAggregateResults.FromParts(operation, Domain(single), parts, count);
        if (expected == null) { Assert.Equal("CQL_SCALAR_OVERFLOW", actual.ErrorCode); return; }
        Assert.False(actual.IsError, actual.Message);
        var bits = single ? (ulong)unchecked((uint)BitConverter.SingleToInt32Bits(Assert.IsType<float>(actual.Result)))
            : unchecked((ulong)BitConverter.DoubleToInt64Bits(Assert.IsType<double>(actual.Result)));
        Assert.Equal(expected.Value, bits);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void AllExponentBoundariesAndSeededBitsRoundTripWithoutChangingType(bool single)
    {
        var random = new Random(20261026);
        var samples = new List<ulong> { 0, Sign(single), 1, 2, Maximum(single), Maximum(single) | Sign(single) };
        var fractionBits = single ? 23 : 52;
        for (var exponent = 0; exponent < (single ? 255 : 2047); exponent++)
        {
            var bits = (ulong)exponent << fractionBits;
            samples.Add(bits); samples.Add(bits | Sign(single));
        }
        var bytes = new byte[8];
        for (var index = 0; index < 4096; index++)
        {
            random.NextBytes(bytes);
            var bits = BitConverter.ToUInt64(bytes) & (single ? uint.MaxValue : ulong.MaxValue);
            if ((bits & ~Sign(single)) <= Maximum(single)) samples.Add(bits);
        }
        foreach (var raw in samples)
        {
            var packet = Packet(single, raw);
            foreach (var operation in new[] { CosmicScalarOperation.Sum, CosmicScalarOperation.Average })
            {
                var actual = CosmicFloatingAggregateResults.FromParts(operation, Domain(single), packet.Parts, 1);
                Assert.False(actual.IsError, actual.Message);
                var bits = single ? (ulong)unchecked((uint)BitConverter.SingleToInt32Bits(Assert.IsType<float>(actual.Result)))
                    : unchecked((ulong)BitConverter.DoubleToInt64Bits(Assert.IsType<double>(actual.Result)));
                Assert.Equal((raw & ~Sign(single)) == 0 ? 0UL : raw, bits);
            }
        }
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void RandomReductionsMatchIndependentNearestNeighborOracle(bool single)
    {
        var random = new Random(20261027); var bytes = new byte[8];
        for (var sample = 0; sample < 128; sample++)
        {
            var inputs = new List<ulong>();
            while (inputs.Count < 2 + sample % 7)
            {
                random.NextBytes(bytes);
                var raw = BitConverter.ToUInt64(bytes) & (single ? uint.MaxValue : ulong.MaxValue);
                if ((raw & ~Sign(single)) <= Maximum(single)) inputs.Add(raw);
            }
            var packet = Packet(single, inputs.ToArray());
            Check(single, CosmicScalarOperation.Sum, packet.Parts, packet.Total, inputs.Count);
            Check(single, CosmicScalarOperation.Average, packet.Parts, packet.Total, inputs.Count);
        }
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void CancellationUnderflowNormalBoundaryAndOverflowRetainExactRounding(bool single)
    {
        var maximum = Maximum(single); var sign = Sign(single);
        var fraction = single ? 23 : 52;
        var one = (ulong)(single ? 127 : 1023) << fraction;
        var halfUlp = (ulong)(single ? 127 - 24 : 1023 - 53) << fraction;
        var cases = new[]
        {
            new[] { maximum, maximum, maximum | sign, maximum | sign, 1UL },
            new[] { maximum, maximum }, new[] { maximum, 1UL },
            new[] { 1UL, 0UL }, new[] { 3UL, 0UL }, new[] { sign | 1UL, 0UL },
            new[] { (1UL << fraction) - 1, 1UL << fraction },
            new[] { one, halfUlp }, new[] { one + 1, halfUlp },
            new[] { maximum, (ulong)(single ? 127 + 103 : 1023 + 970) << fraction },
            new[] { maximum, (ulong)(single ? 127 + 102 : 1023 + 969) << fraction }
        };
        foreach (var inputs in cases)
        {
            var packet = Packet(single, inputs);
            Check(single, CosmicScalarOperation.Sum, packet.Parts, packet.Total, inputs.Length);
            Check(single, CosmicScalarOperation.Average, packet.Parts, packet.Total, inputs.Length);
            var negative = packet.Parts.Select(part => -part).ToArray();
            Check(single, CosmicScalarOperation.Sum, negative, -packet.Total, inputs.Length);
            Check(single, CosmicScalarOperation.Average, negative, -packet.Total, inputs.Length);
        }
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void EmptyNullableCountsShapeAndWorkBoundsRemainExplicit(bool single)
    {
        var empty = new long[Width(single)];
        Assert.Equal("CQL_EMPTY_SEQUENCE", CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Average, Domain(single), empty, 0).ErrorCode);
        Assert.Null(CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Average, Domain(single,true), empty, 0).Result);
        Assert.False(CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Sum, Domain(single,true), empty, 0).IsError);
        Assert.Equal("CQL_DATA_INVALID", CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Sum, Domain(single), empty, -1).ErrorCode);
        Assert.Equal("CQL_QUERY_LIMIT", CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Sum, Domain(single), empty, (long)int.MaxValue+1).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID", CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Sum, Domain(single), new long[Width(single)-1], 1).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID", CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Sum, Domain(single), Words(Units(Maximum(single),single)+1,single), 1).ErrorCode);
        empty[0] = 1L << 30;
        Assert.Equal("CQL_DATA_INVALID", CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Sum, Domain(single), empty, 1).ErrorCode);
        var maximum = Words(Units(Maximum(single),single),single).Select(part => checked(part*int.MaxValue)).ToArray();
        Check(single, CosmicScalarOperation.Average, maximum, Units(Maximum(single),single)*int.MaxValue, int.MaxValue);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void OneInputCannotHideSourceRoundingOrNoncanonicalDigits(bool single)
    {
        var fraction = single ? 23 : 52;
        var one = (ulong)(single ? 127 : 1023) << fraction;
        var inexact = Words(Units(one, single) + 1, single);
        Assert.Equal("CQL_DATA_INVALID", CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Sum, Domain(single), inexact, 1).ErrorCode);
        var mixed = new long[Width(single)]; mixed[^2] = 1; mixed[^1] = -64;
        Assert.Equal("CQL_DATA_INVALID", CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Average, Domain(single), mixed, 1).ErrorCode);
    }

    [Fact]
    public void ForeignDomainsAndOperationsCannotUseTheFloatingPacket()
    {
        Assert.Equal("CQL_PLAN_INVALID", CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Count, typeof(double), new long[70], 0).ErrorCode);
        Assert.Equal("CQL_PLAN_INVALID", CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Sum, typeof(decimal), new long[70], 0).ErrorCode);
        Assert.Equal("CQL_PLAN_INVALID", CosmicFloatingAggregateResults.FromParts(CosmicScalarOperation.Sum, null!, new long[70], 0).ErrorCode);
    }
}
