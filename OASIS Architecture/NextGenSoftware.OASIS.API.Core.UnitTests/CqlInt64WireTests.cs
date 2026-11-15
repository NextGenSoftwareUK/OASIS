using System.Globalization;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlInt64WireTests
{
    private static CqlV1Request Source(JsonElement value) => new()
    {
        Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "sequence" }, Scalar = CqlV1Scalar.Integer,
            Operator = CqlV1Operator.GreaterThanOrEqual, Value = value } }
    };

    [Theory]
    [InlineData("-9223372036854775808")]
    [InlineData("-9007199254740993")]
    [InlineData("0")]
    [InlineData("9007199254740993")]
    [InlineData("9223372036854775807")]
    public void SignedTextAndExistingNumberProduceTheSameExactNativePredicate(string value)
    {
        var expected = long.Parse(value, CultureInfo.InvariantCulture);
        foreach (var wire in new[] { JsonSerializer.SerializeToElement(value), JsonSerializer.SerializeToElement(expected) })
        {
            var query = CqlV1Compiler.Compile(Source(wire), Guid.NewGuid(), null);
            var predicate = Assert.Single(query.Predicates.OfType<CosmicQueryComparison>());
            Assert.Equal(typeof(long), predicate.Path.ValueType);
            Assert.Equal(expected, Assert.IsType<long>(predicate.Literal.Value));
        }
    }

    [Theory]
    [InlineData("")] [InlineData("+1")] [InlineData("-0")] [InlineData("01")]
    [InlineData(" 1")] [InlineData("1 ")] [InlineData("1.0")] [InlineData("1e2")]
    [InlineData("-9223372036854775809")] [InlineData("9223372036854775808")]
    public void NonCanonicalOrOutOfRangeSignedTextIsRejected(string value)
        => Assert.Throws<ArgumentException>(() =>
            CqlV1Compiler.Compile(Source(JsonSerializer.SerializeToElement(value)), Guid.NewGuid(), null));
}
