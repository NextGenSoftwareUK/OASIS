using System;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlDoubleWireTests
{
    [Fact]
    public void DoubleWireTypePreservesExistingScalarIdentities()
    {
        Assert.Equal(4, (int)CqlV1Scalar.DateTime);
        Assert.Equal(5, (int)CqlV1Scalar.UInt64);
        Assert.Equal(6, (int)CqlV1Scalar.Decimal);
        Assert.Equal(7, (int)CqlV1Scalar.Double);
    }

    private static CqlV1Request Source(JsonElement value) => new()
    {
        Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "counter" }, Scalar = CqlV1Scalar.Double,
            Operator = CqlV1Operator.Equal, Value = value } }
    };
    private static CqlGroupedV1Request Group(JsonElement value, bool nullable = false) => new()
    {
        Keys = new[] { new CqlProjectionV1Column { Alias = "counter", MetadataPath = new[] { "counter" }, ValueType = "Double", Nullable = nullable } },
        Columns = new[] { new CqlGroupedV1Column { Alias = "Counter", KeyAlias = "counter" } },
        ResultFilters = new[] { new CqlGroupedV1Filter { Alias = "Counter", Operator = CqlV1Operator.Equal, Value = value } }
    };

    [Theory]
    [InlineData("0")] [InlineData("-1.7976931348623157E+308")]
    [InlineData("1.7976931348623157E+308")] [InlineData("5E-324")]
    [InlineData("-5E-324")] [InlineData("1.0000000000000002")]
    public void SourceDoubleStringsPreserveEveryDigit(string text)
    {
        var query = CqlV1Compiler.Compile(Source(JsonSerializer.SerializeToElement(text)), Guid.NewGuid(), null);
        var predicate = Assert.Single(query.Predicates.OfType<CosmicQueryComparison>());
        Assert.Equal(typeof(double), predicate.Path.ValueType);
        Assert.Equal(double.Parse(text, System.Globalization.CultureInfo.InvariantCulture), Assert.IsType<double>(predicate.Literal.Value));
        Assert.NotNull(query.ReadAccess);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void GroupedDoubleAliasesRetainDeclaredNullableType(bool nullable)
    {
        var query = CqlV1Compiler.CompileGrouped(Group(JsonSerializer.SerializeToElement("1.0000000000000002"), nullable), Guid.NewGuid(), null);
        var predicate = Assert.IsType<CosmicQueryComparison>(Assert.Single(query.ResultPredicates));
        Assert.Equal(nullable ? typeof(double?) : typeof(double), predicate.Path.ValueType);
        Assert.Equal(1.0000000000000002d, Assert.IsType<double>(predicate.Literal.Value));
    }

    [Theory]
    [InlineData("")] [InlineData("-0")] [InlineData("+1")] [InlineData(" 1")] [InlineData("1 ")]
    [InlineData("01")] [InlineData("1e2")] [InlineData("1E+309")]
    [InlineData("1.00000000000000001")] [InlineData("1E-1000")] [InlineData("0.0")] [InlineData("5e-324")] [InlineData("NaN")] [InlineData("Infinity")]
    public void InvalidDoubleTextCannotChangeNumericMeaning(string text)
    {
        var value = JsonSerializer.SerializeToElement(text);
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(Source(value), Guid.NewGuid(), null));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(Group(value), Guid.NewGuid(), null));
    }

    [Theory]
    [InlineData("0")] [InlineData("1.0000000000000002")] [InlineData("true")]
    public void DoubleWireRequiresStringValues(string json)
    {
        using var doc = JsonDocument.Parse(json);
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(Source(doc.RootElement), Guid.NewGuid(), null));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(Group(doc.RootElement), Guid.NewGuid(), null));
    }
}
