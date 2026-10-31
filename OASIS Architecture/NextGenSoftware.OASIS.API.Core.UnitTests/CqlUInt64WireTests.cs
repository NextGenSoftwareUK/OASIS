using System;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlUInt64WireTests
{
    private static CqlV1Request Source(JsonElement value) => new()
    {
        Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "counter" }, Scalar = CqlV1Scalar.UInt64,
            Operator = CqlV1Operator.Equal, Value = value } }
    };
    private static CqlGroupedV1Request Group(JsonElement value, bool nullable = false) => new()
    {
        Keys = new[] { new CqlProjectionV1Column { Alias = "counter", MetadataPath = new[] { "counter" }, ValueType = "UInt64", Nullable = nullable } },
        Columns = new[] { new CqlGroupedV1Column { Alias = "Counter", KeyAlias = "counter" } },
        ResultFilters = new[] { new CqlGroupedV1Filter { Alias = "Counter", Operator = CqlV1Operator.Equal, Value = value } }
    };

    [Theory]
    [InlineData("0")] [InlineData("9007199254740993")]
    [InlineData("9223372036854775808")] [InlineData("18446744073709551615")]
    public void SourceUnsignedStringsPreserveEveryBit(string text)
    {
        var query = CqlV1Compiler.Compile(Source(JsonSerializer.SerializeToElement(text)), Guid.NewGuid(), null);
        var predicate = Assert.Single(query.Predicates.OfType<CosmicQueryComparison>());
        Assert.Equal(typeof(ulong), predicate.Path.ValueType);
        Assert.Equal(decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture), Assert.IsType<decimal>(predicate.Literal.Value));
        Assert.NotNull(query.ReadAccess);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void GroupedUnsignedAliasesRetainDeclaredNullableType(bool nullable)
    {
        var query = CqlV1Compiler.CompileGrouped(Group(JsonSerializer.SerializeToElement("18446744073709551615"), nullable), Guid.NewGuid(), null);
        var predicate = Assert.IsType<CosmicQueryComparison>(Assert.Single(query.ResultPredicates));
        Assert.Equal(nullable ? typeof(ulong?) : typeof(ulong), predicate.Path.ValueType);
        Assert.Equal((decimal)ulong.MaxValue, Assert.IsType<decimal>(predicate.Literal.Value));
    }

    [Theory]
    [InlineData("")] [InlineData("-1")] [InlineData("+1")] [InlineData(" 1")] [InlineData("1 ")]
    [InlineData("1.0")] [InlineData("1e2")] [InlineData("18446744073709551616")]
    [InlineData("9999999999999999999999999")] [InlineData("NaN")] [InlineData("Infinity")]
    public void InvalidUnsignedTextCannotChangeNumericMeaning(string text)
    {
        var value = JsonSerializer.SerializeToElement(text);
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(Source(value), Guid.NewGuid(), null));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(Group(value), Guid.NewGuid(), null));
    }

    [Theory]
    [InlineData("0")] [InlineData("18446744073709551615")] [InlineData("true")]
    public void UnsignedWireRequiresStringValues(string json)
    {
        using var doc = JsonDocument.Parse(json);
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(Source(doc.RootElement), Guid.NewGuid(), null));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(Group(doc.RootElement), Guid.NewGuid(), null));
    }
}
