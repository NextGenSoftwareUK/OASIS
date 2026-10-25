using System;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlSingleWireTests
{
    [Fact]
    public void SingleWireTypePreservesExistingScalarIdentities()
    {
        Assert.Equal(4, (int)CqlV1Scalar.DateTime);
        Assert.Equal(5, (int)CqlV1Scalar.UInt64);
        Assert.Equal(6, (int)CqlV1Scalar.Decimal);
        Assert.Equal(7, (int)CqlV1Scalar.Double);
        Assert.Equal(8, (int)CqlV1Scalar.Single);
    }

    private static CqlV1Request Source(JsonElement value) => new()
    {
        Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "counter" }, Scalar = CqlV1Scalar.Single,
            Operator = CqlV1Operator.Equal, Value = value } }
    };
    private static CqlGroupedV1Request Group(JsonElement value, bool nullable = false) => new()
    {
        Keys = new[] { new CqlProjectionV1Column { Alias = "counter", MetadataPath = new[] { "counter" }, ValueType = "Single", Nullable = nullable } },
        Columns = new[] { new CqlGroupedV1Column { Alias = "Counter", KeyAlias = "counter" } },
        ResultFilters = new[] { new CqlGroupedV1Filter { Alias = "Counter", Operator = CqlV1Operator.Equal, Value = value } }
    };

    [Theory]
    [InlineData("0")] [InlineData("-3.4028235E+38")]
    [InlineData("3.4028235E+38")] [InlineData("1E-45")]
    [InlineData("-1E-45")] [InlineData("1.0000001")]
    public void SourceSingleStringsPreserveEveryDigit(string text)
    {
        var query = CqlV1Compiler.Compile(Source(JsonSerializer.SerializeToElement(text)), Guid.NewGuid(), null);
        var predicate = Assert.Single(query.Predicates.OfType<CosmicQueryComparison>());
        Assert.Equal(typeof(float), predicate.Path.ValueType);
        Assert.Equal((double)float.Parse(text, System.Globalization.CultureInfo.InvariantCulture), Assert.IsType<double>(predicate.Literal.Value));
        Assert.NotNull(query.ReadAccess);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void GroupedSingleAliasesRetainDeclaredNullableType(bool nullable)
    {
        var query = CqlV1Compiler.CompileGrouped(Group(JsonSerializer.SerializeToElement("1.0000001"), nullable), Guid.NewGuid(), null);
        var predicate = Assert.IsType<CosmicQueryComparison>(Assert.Single(query.ResultPredicates));
        Assert.Equal(nullable ? typeof(float?) : typeof(float), predicate.Path.ValueType);
        Assert.Equal((double)1.0000001f, Assert.IsType<double>(predicate.Literal.Value));
    }

    [Theory]
    [InlineData("")] [InlineData("-0")] [InlineData("+1")] [InlineData(" 1")] [InlineData("1 ")]
    [InlineData("01")] [InlineData("1e2")] [InlineData("1E+309")]
    [InlineData("1.00000000000000001")] [InlineData("1E-1000")] [InlineData("0.0")] [InlineData("1e-45")] [InlineData("NaN")] [InlineData("Infinity")]
    public void InvalidSingleTextCannotChangeNumericMeaning(string text)
    {
        var value = JsonSerializer.SerializeToElement(text);
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(Source(value), Guid.NewGuid(), null));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(Group(value), Guid.NewGuid(), null));
    }

    [Theory]
    [InlineData("0")] [InlineData("1.0000001")] [InlineData("true")]
    public void SingleWireRequiresStringValues(string json)
    {
        using var doc = JsonDocument.Parse(json);
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(Source(doc.RootElement), Guid.NewGuid(), null));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(Group(doc.RootElement), Guid.NewGuid(), null));
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CompleteSingleGroupRequestPreservesFiltersOrderingAndPage(bool nullable)
    {
        var request = Group(JsonSerializer.SerializeToElement("1.0000002"), nullable);
        request.Columns = request.Columns.Concat(new[] { new CqlGroupedV1Column
            { Alias = "Items", Operation = "LongCount" } }).ToArray();
        request.ResultFilters = request.ResultFilters.Concat(new[] { new CqlGroupedV1Filter
            { Alias = "Counter", Operator = CqlV1Operator.LessThan,
                Value = JsonSerializer.SerializeToElement("1.0000004") } }).ToArray();
        request.ResultOrder = new[] { new CqlGroupedV1Ordering { Alias = "Counter" } };
        request.ResultTake = 2;
        var query = CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null);
        Assert.Equal(2, query.Columns.Count);
        Assert.Equal(2, query.ResultPredicates.Count);
        Assert.Single(query.Orderings);
    }

}
