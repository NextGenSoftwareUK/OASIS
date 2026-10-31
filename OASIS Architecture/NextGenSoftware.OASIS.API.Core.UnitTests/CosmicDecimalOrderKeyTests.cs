using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicDecimalOrderKeyTests
{
    [Fact]
    public void EquivalentScalesAndSignedZeroHaveOneIdentity()
    {
        Assert.Equal(CosmicNumericScalarResults.DecimalOrderKey(1m), CosmicNumericScalarResults.DecimalOrderKey(1.000m));
        Assert.Equal(CosmicNumericScalarResults.DecimalOrderKey(0m),
            CosmicNumericScalarResults.DecimalOrderKey(new decimal(0, 0, 0, true, 28)));
    }

    [Fact]
    public void OrdinalKeysPreserveFullDecimalOrderIncludingExtremesAndSubunits()
    {
        var values = new[] { decimal.MaxValue, 0m, decimal.MinValue, 1e-28m, -1e-28m,
            (decimal)long.MinValue, (decimal)long.MaxValue, -1.5m, 1.5m, 9007199254740993m, 9007199254740992m };
        Assert.Equal(values.OrderBy(value => value), values.OrderBy(CosmicNumericScalarResults.DecimalOrderKey, StringComparer.Ordinal));
        Assert.All(values, value => Assert.Equal(58, CosmicNumericScalarResults.DecimalOrderKey(value).Length));
    }

    [Fact]
    public void DeterministicMixedScaleSamplesPreserveNumericOrder()
    {
        var random = new Random(20261024);
        var values = Enumerable.Range(0, 4096).Select(_ => new decimal(
            (int)random.NextInt64(int.MinValue, (long)int.MaxValue + 1),
            (int)random.NextInt64(int.MinValue, (long)int.MaxValue + 1),
            (int)random.NextInt64(int.MinValue, (long)int.MaxValue + 1), random.Next(2) != 0, (byte)random.Next(29))).ToArray();
        Assert.Equal(values.OrderBy(value => value), values.OrderBy(CosmicNumericScalarResults.DecimalOrderKey, StringComparer.Ordinal));
    }
    [Fact]
    public void DecoderRoundTripsEveryBoundaryAndDeterministicScaleSample()
    {
        var random = new Random(20261025);
        var samples = Enumerable.Range(0, 4096).Select(_ => new decimal(
            (int)random.NextInt64(int.MinValue, (long)int.MaxValue + 1),
            (int)random.NextInt64(int.MinValue, (long)int.MaxValue + 1),
            (int)random.NextInt64(int.MinValue, (long)int.MaxValue + 1), random.Next(2) != 0, (byte)random.Next(29)))
            .Concat(new[] { decimal.MinValue, decimal.MaxValue, 0m, 1e-28m, -1e-28m, 1.000m });
        foreach (var value in samples)
        {
            var decoded = CosmicNumericScalarResults.DecimalFromOrderKey(CosmicNumericScalarResults.DecimalOrderKey(value));
            Assert.False(decoded.IsError, decoded.Message);
            Assert.True(decoded.IsLoaded);
            Assert.Equal(value, decoded.Result);
        }
    }

    [Fact]
    public void DecoderRejectsMalformedNoncanonicalAndNonrepresentableKeys()
    {
        var invalid = new[] { null, "", "1", "2" + new string('0', 57), "1" + new string('x', 57),
            "0" + new string('9', 57), "1" + new string('9', 57), "0" + new string('0', 57),
            "1" + new string('0', 56) + "-" };
        foreach (var key in invalid)
        {
            var result = CosmicNumericScalarResults.DecimalFromOrderKey(key);
            Assert.True(result.IsError); Assert.Equal("CQL_DATA_INVALID", result.ErrorCode);
        }
    }

    [Fact]
    public void DecimalGroupingRetainsExactKeyIdentity()
    {
        var query = CosmicQuery<NextGenSoftware.OASIS.API.Core.Holons.Holon>.Create()
            .GroupBy(h => CosmicQueryMetadata.Value<decimal?>(h, "price"));
        Assert.False(query.IsError, query.Message);
        Assert.Equal(typeof(decimal?), Assert.Single(query.Result.Key.Columns).Path.ValueType);
    }

}
