using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicFloatingOrderKeyTests
{
    [Fact]
    public void SingleGroupingCapturesTheDeclaredNullableKeyWithoutEnrollingProviders()
    {
        var result = CosmicQuery<Holon>.Create().GroupBy(h => CosmicQueryMetadata.Value<float?>(h, "reading"));
        Assert.False(result.IsError, result.Message);
        Assert.Equal(typeof(float?), result.Result.Key.Columns[0].Path.ValueType);
    }

    [Fact]
    public void DoubleGroupingCapturesTheDeclaredNullableKeyWithoutEnrollingProviders()
    {
        var result = CosmicQuery<Holon>.Create().GroupBy(h => CosmicQueryMetadata.Value<double?>(h, "reading"));
        Assert.False(result.IsError, result.Message);
        Assert.Equal(typeof(double?), result.Result.Key.Columns[0].Path.ValueType);
    }

    [Fact]
    public void DoubleKeysRoundTrip4096FiniteBitPatternsWithoutDecimalConversion()
    {
        var random = new Random(20261025);
        var bytes = new byte[8];
        for (var accepted = 0; accepted < 4096;)
        {
            random.NextBytes(bytes);
            var value = BitConverter.ToDouble(bytes, 0);
            if (double.IsNaN(value) || double.IsInfinity(value)) continue;
            var result = CosmicFloatingScalarResults.DoubleFromOrderKey(CosmicFloatingScalarResults.DoubleOrderKey(value));
            Assert.False(result.IsError, result.Message);
            Assert.Equal(BitConverter.DoubleToInt64Bits(value == 0d ? 0d : value), BitConverter.DoubleToInt64Bits(result.Result));
            accepted++;
        }
    }

    [Fact]
    public void SingleKeysRoundTrip4096FiniteBitPatternsIncludingSubnormals()
    {
        var random = new Random(20261026);
        var bytes = new byte[4];
        for (var accepted = 0; accepted < 4096;)
        {
            random.NextBytes(bytes);
            var value = BitConverter.ToSingle(bytes, 0);
            if (float.IsNaN(value) || float.IsInfinity(value)) continue;
            var result = CosmicFloatingScalarResults.SingleFromOrderKey(CosmicFloatingScalarResults.SingleOrderKey(value));
            Assert.False(result.IsError, result.Message);
            Assert.Equal(BitConverter.SingleToInt32Bits(value == 0f ? 0f : value), BitConverter.SingleToInt32Bits(result.Result));
            accepted++;
        }
    }

    [Fact]
    public void OrdinalKeysMatchFiniteNumericOrderingAtEveryBoundary()
    {
        var values = new[] { double.MaxValue, -double.Epsilon, 0d, double.MinValue,
            double.Epsilon, -1d, 1d, float.MinValue, float.MaxValue, -float.Epsilon, float.Epsilon };
        var ordered = values.Select(CosmicFloatingScalarResults.DoubleOrderKey).OrderBy(key => key, StringComparer.Ordinal)
            .Select(key => CosmicFloatingScalarResults.DoubleFromOrderKey(key).Result).ToArray();
        Assert.Equal(values.OrderBy(value => value).ToArray(), ordered);
        Assert.Equal(CosmicFloatingScalarResults.DoubleOrderKey(0d),
            CosmicFloatingScalarResults.DoubleOrderKey(BitConverter.Int64BitsToDouble(long.MinValue)));
    }

    [Fact]
    public void NonfiniteValuesCannotBecomeKeys()
    {
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => CosmicFloatingScalarResults.DoubleOrderKey(value));
        foreach (var value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => CosmicFloatingScalarResults.SingleOrderKey(value));
    }

    [Fact]
    public void MalformedNoncanonicalAndNonfiniteNativeKeysAreDataErrors()
    {
        foreach (var key in new[] { "", "0", "bff0000000000000", "7FFFFFFFFFFFFFFF", "FFF0000000000000", "000FFFFFFFFFFFFF", "FFF8000000000000" })
        {
            var result = CosmicFloatingScalarResults.DoubleFromOrderKey(key);
            Assert.True(result.IsError);
            Assert.Equal("CQL_DATA_INVALID", result.ErrorCode);
            Assert.False(result.IsLoaded);
        }
    }

    [Fact]
    public void SinglePacketsCannotRoundBinary64OnlyValuesOrUnderflow()
    {
        foreach (var value in new[] { 0.1d, double.Epsilon, double.MaxValue, 1.0000000000000002d })
        {
            var result = CosmicFloatingScalarResults.SingleFromOrderKey(CosmicFloatingScalarResults.DoubleOrderKey(value));
            Assert.True(result.IsError);
            Assert.Equal("CQL_DATA_INVALID", result.ErrorCode);
        }
        foreach (var value in new[] { float.MinValue, -float.Epsilon, 0f, float.Epsilon, 0.1f, float.MaxValue })
            Assert.Equal(value, CosmicFloatingScalarResults.SingleFromOrderKey(CosmicFloatingScalarResults.SingleOrderKey(value)).Result);
    }
}
