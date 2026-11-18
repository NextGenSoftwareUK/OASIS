using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicProjectionResultTests_ComputedFunctions
{
    private sealed class ComputedShape
    {
        public static int Constructions;
        public ComputedShape() { Constructions++; }
        public int Units { get; set; }
    }

    [Fact]
    public void LengthCaptureSeparatesSourceAndResultDomainsWithoutRunningConstructors()
    {
        ComputedShape.Constructions = 0;
        var captured = CosmicQuery<Holon>.Create().Select(row => new ComputedShape { Units = row.Name.Length });
        Assert.False(captured.IsError, captured.Message);
        Assert.Equal(0, ComputedShape.Constructions);
        var column = Assert.Single(captured.Result.Columns);
        Assert.Equal(CosmicProjectionFunction.Utf16Length, column.Function);
        Assert.Equal(typeof(string), column.Path.ValueType);
        Assert.Equal(typeof(int), column.ValueType);
        Assert.Equal(new[] { "Name" }, column.Path.Segments);
    }

    [Fact]
    public void NullableMetadataLengthRetainsTheOriginalPathAndDeclaredOutput()
    {
        var captured = CosmicQuery<Holon>.Create().Select(row =>
            (int?)CosmicQueryMetadata.Value<string>(row, "caption").Length);
        Assert.False(captured.IsError, captured.Message);
        var column = Assert.Single(captured.Result.Columns);
        Assert.True(column.Path.IsMetadata);
        Assert.Equal(new[] { "caption" }, column.Path.Segments);
        Assert.Equal(typeof(string), column.Path.ValueType);
        Assert.Equal(typeof(int?), column.ValueType);
    }

    [Fact]
    public void EveryOutputCellIsValidatedBeforeAnyConstructorRuns()
    {
        var query = CosmicQuery<Holon>.Create().Select(row => new ComputedShape { Units = row.Name.Length }).Result;
        ComputedShape.Constructions = 0;
        var invalid = CosmicProjectionResults.Materialize(query,
            new[] { new CosmicProjectionRow(2), new CosmicProjectionRow("7") });
        Assert.True(invalid.IsError);
        Assert.Equal(0, ComputedShape.Constructions);
        var valid = CosmicProjectionResults.Materialize(query,
            new[] { new CosmicProjectionRow(2), new CosmicProjectionRow(7) });
        Assert.False(valid.IsError, valid.Message);
        Assert.Equal(new[] { 2, 7 }, valid.Result.Select(row => row.Units));
        Assert.Equal(2, ComputedShape.Constructions);
    }

    [Fact]
    public void FunctionsRequireIndependentNativeAdmissionAndDoNotBroadenMetadataRoots()
    {
        var source = CosmicQuery<Holon>.Create();
        var canonical = source.Select(row => row.Name.Length).Result;
        var metadata = source.Select(row => CosmicQueryMetadata.Value<string>(row, "caption").Length).Result;
        var other = source.Select(row => CosmicQueryMetadata.Value<string>(row, "private").Length).Result;
        var plain = new CosmicProjectionCapabilities(
            new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) },
            new[] { typeof(string), typeof(int), typeof(int?) });
        Assert.True(plain.Admit(canonical).IsError);
        var functions = new[] { CosmicProjectionFunction.Utf16Length };
        var declared = plain.WithMetadataRoots(new[] { "caption" }).WithFunctions(functions);
        functions[0] = (CosmicProjectionFunction)999;
        Assert.False(declared.Admit(canonical).IsError);
        Assert.False(declared.Admit(metadata).IsError);
        Assert.True(declared.Admit(other).IsError);
        Assert.False(declared.WithMetadataRoots(new[] { "caption" }).Admit(metadata).IsError);
        Assert.True(plain.Admit(canonical).IsError);
        Assert.Throws<ArgumentException>(() => plain.WithFunctions((CosmicProjectionFunction)999));
        var missingInput = new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(),
            new[] { typeof(string), typeof(int) }).WithFunctions(CosmicProjectionFunction.Utf16Length);
        Assert.True(missingInput.Admit(canonical).IsError);
    }

    [Fact]
    public void CaptureDoesNotSilentlyEnrollComputedGroupingOrArbitraryExpressions()
    {
        var source = CosmicQuery<Holon>.Create();
        Assert.True(source.GroupBy(row => row.Name.Length).IsError);
        Assert.True(source.Select(row => row.Name.Length + 1).IsError);
        Assert.True(source.Select(row => (long)row.Name.Length).IsError);
        Assert.True(source.Select(row => row.Name.GetHashCode()).IsError);
    }
}
