using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlUInt64CaptureTests
{
    [Fact]
    public void MaximumUnsignedLiteralUsesExactDecimalPacket()
    {
        var query = CosmicQuery<Holon>.Create()
            .Where(row => CosmicQueryMetadata.Value<ulong>(row, "counter") == ulong.MaxValue);
        Assert.False(query.IsError, query.Message);
        var comparison = Assert.IsType<CosmicQueryComparison>(Assert.Single(query.Result.Predicates));
        Assert.Equal(typeof(ulong), comparison.Path.ValueType);
        Assert.Equal(CosmicQueryScalarKind.Decimal, comparison.Literal.Kind);
        Assert.Equal((decimal)ulong.MaxValue, Assert.IsType<decimal>(comparison.Literal.Value));
    }

    [Fact]
    public void UnsignedGroupingRetainsDeclaredKeyType()
    {
        var query = CosmicQuery<Holon>.Create()
            .GroupBy(row => CosmicQueryMetadata.Value<ulong?>(row, "counter"));
        Assert.False(query.IsError, query.Message);
        Assert.Equal(typeof(ulong?), Assert.Single(query.Result.Key.Columns).Path.ValueType);
    }

    [Fact]
    public void UnsignedToDecimalConversionIsLossless()
    {
        var query = CosmicQuery<Holon>.Create()
            .Where(row => (decimal)CosmicQueryMetadata.Value<ulong>(row, "counter") == (decimal)ulong.MaxValue);
        Assert.False(query.IsError, query.Message);
    }

    [Fact]
    public void UnsignedToBinary64ConversionCannotHidePrecisionLoss()
    {
        var query = CosmicQuery<Holon>.Create()
            .Where(row => (double)CosmicQueryMetadata.Value<ulong>(row, "counter") == 1d);
        Assert.True(query.IsError);
    }
}
