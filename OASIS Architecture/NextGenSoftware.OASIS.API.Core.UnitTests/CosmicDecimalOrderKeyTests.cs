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
}
