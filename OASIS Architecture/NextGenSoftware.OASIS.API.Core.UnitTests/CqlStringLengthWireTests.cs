using System.Globalization;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlStringLengthWireTests
{
    private static CqlV1Request Request(string value, bool metadata = false) => new()
    {
        Filters = new[]
        {
            new CqlV1Filter
            {
                Field = metadata ? null : "Name",
                MetadataPath = metadata ? new[] { "title" } : null,
                Scalar = CqlV1Scalar.String, Function = CqlV1Function.Utf16Length,
                Operator = CqlV1Operator.GreaterThanOrEqual,
                Value = JsonSerializer.SerializeToElement(value)
            }
        }
    };

    [Theory]
    [InlineData("-9223372036854775808")]
    [InlineData("-9007199254740993")]
    [InlineData("0")]
    [InlineData("2")]
    [InlineData("9007199254740993")]
    [InlineData("9223372036854775807")]
    public void ExactIntegerOutputBoundRetainsTheDeclaredStringInput(string value)
    {
        foreach (var metadata in new[] { false, true })
        {
            var request = Request(value, metadata);
            foreach (var operation in new[] { CqlV1Operator.Equal, CqlV1Operator.NotEqual,
                CqlV1Operator.GreaterThan, CqlV1Operator.GreaterThanOrEqual,
                CqlV1Operator.LessThan, CqlV1Operator.LessThanOrEqual })
            {
                request.Filters[0].Operator = operation;
                var captured = CqlRequestCapture.Capture(request);
                var query = CqlV1Compiler.Compile(captured, Guid.NewGuid(), null);
                var length = Assert.Single(query.Predicates.OfType<CosmicQueryStringLength>());
                Assert.Equal(typeof(string), length.Path.ValueType);
                Assert.Equal(metadata, length.Path.IsMetadata);
                Assert.Equal(long.Parse(value, CultureInfo.InvariantCulture), length.Value);
                Assert.Equal(Enum.Parse<CosmicQueryComparisonOperator>(operation.ToString()), length.Operator);
            }
        }
    }

    [Fact]
    public void CapturedFunctionAndPathCannotChangeWithTheCallerDescription()
    {
        var request = Request("2", metadata: true);
        var captured = CqlRequestCapture.Capture(request);
        request.Filters[0].Function = null;
        request.Filters[0].MetadataPath![0] = "changed";
        request.Filters[0].Value = JsonSerializer.SerializeToElement("7");
        var query = CqlV1Compiler.Compile(captured, Guid.NewGuid(), null);
        var length = Assert.Single(query.Predicates.OfType<CosmicQueryStringLength>());
        Assert.Equal(new[] { "title" }, length.Path.Segments);
        Assert.Equal(2L, length.Value);
    }

    [Theory]
    [InlineData(CqlV1Operator.In)]
    [InlineData(CqlV1Operator.Contains)]
    [InlineData(CqlV1Operator.IsNull)]
    [InlineData(CqlV1Operator.Exists)]
    public void NoncomparisonFunctionShapesAreRejected(CqlV1Operator operation)
    {
        var request = Request("2", metadata: operation == CqlV1Operator.Exists);
        request.Filters[0].Operator = operation;
        if (operation is CqlV1Operator.Exists or CqlV1Operator.IsNull)
            request.Filters[0].Value = default;
        if (operation == CqlV1Operator.In)
            request.Filters[0].Value = JsonSerializer.SerializeToElement(new[] { "1", "2" });
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(request, Guid.NewGuid(), null));
    }

    [Fact]
    public void InvalidFunctionAndNonstringDomainsAreRejected()
    {
        var request = Request("2");
        request.Filters[0].Function = (CqlV1Function)10;
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(request, Guid.NewGuid(), null));
        request.Filters[0].Function = CqlV1Function.Utf16Length;
        request.Filters[0].Field = "Version";
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(request, Guid.NewGuid(), null));
        request = Request("2", metadata: true);
        request.Filters[0].Scalar = CqlV1Scalar.Integer;
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(request, Guid.NewGuid(), null));
    }

    [Fact]
    public void VirtualLengthPreservesTheInputAndExactBound()
    {
        var filter = Request("2", metadata: true).Filters[0];
        filter.Operator = CqlV1Operator.Equal;
        var native = Assert.Single(CqlV1Compiler.CompileVirtualMatches(new[] { filter }));
        Assert.Equal(NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeRecordFunction.Utf16Length, native.Function);
        Assert.Equal(typeof(string), native.InputType);
        Assert.Equal(2L, Assert.IsType<long>(native.Value));
        Assert.Equal("title", native.Field);
        filter.Function = (CqlV1Function)99;
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileVirtualMatches(new[] { filter }));
    }

    [Fact]
    public void JsonRoundTripPreservesTheFunctionAndOrdinaryFiltersOmitIt()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var text = JsonSerializer.Serialize(Request("2"), options);
        var request = JsonSerializer.Deserialize<CqlV1Request>(text, options)!;
        Assert.Equal(CqlV1Function.Utf16Length, request.Filters[0].Function);
        request.Filters[0].Function = null;
        Assert.DoesNotContain("\"function\"", JsonSerializer.Serialize(request, options));
    }

    [Theory]
    [InlineData(CqlV1Operator.Equal, CosmicQueryComparisonOperator.Equal)]
    [InlineData(CqlV1Operator.NotEqual, CosmicQueryComparisonOperator.NotEqual)]
    [InlineData(CqlV1Operator.GreaterThan, CosmicQueryComparisonOperator.GreaterThan)]
    [InlineData(CqlV1Operator.GreaterThanOrEqual, CosmicQueryComparisonOperator.GreaterThanOrEqual)]
    [InlineData(CqlV1Operator.LessThan, CosmicQueryComparisonOperator.LessThan)]
    [InlineData(CqlV1Operator.LessThanOrEqual, CosmicQueryComparisonOperator.LessThanOrEqual)]
    public void VirtualLengthRetainsAllComparisonsAndExactBounds(CqlV1Operator operation,
        CosmicQueryComparisonOperator expected)
    {
        foreach (var bound in new[] { long.MinValue, 9007199254740993L, long.MaxValue })
        {
            var request = Request(bound.ToString(System.Globalization.CultureInfo.InvariantCulture), metadata: true);
            request.Filters[0].Operator = operation;
            var filter = Assert.Single(CqlV1Compiler.CompileVirtualMatches(request.Filters));
            Assert.Equal(expected, filter.Operator);
            Assert.Equal(bound, Assert.IsType<long>(filter.Value));
            Assert.Equal(typeof(string), filter.InputType);
        }
    }


}
