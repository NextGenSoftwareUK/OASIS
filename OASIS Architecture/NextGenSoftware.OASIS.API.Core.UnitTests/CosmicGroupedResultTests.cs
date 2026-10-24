using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedResultTests
{
    public sealed class ResultDto
    {
        public static int Constructions;
        public ResultDto() => Constructions++;
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    [Fact]
    public void WholeBufferValidatesBeforeAnyDtoConstructor()
    {
        ResultDto.Constructions = 0;
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => new ResultDto { Name = g.Key, Count = g.Count() }).Result;
        var bad = CosmicGroupedResults.Materialize(query, new[]
        {
            new CosmicGroupedRow(2, new object[] { "A" }, "A", 2),
            new CosmicGroupedRow(1, new object[] { "B" }, "B", "wrong")
        });
        Assert.True(bad.IsError);
        Assert.Equal(0, ResultDto.Constructions);
        var good = CosmicGroupedResults.Materialize(query, new[] { new CosmicGroupedRow(2, new object[] { "A" }, "A", 2) });
        Assert.False(good.IsError, good.Message);
        Assert.Equal(1, ResultDto.Constructions);
        Assert.Equal("A", good.Result.Single().Name);
        Assert.Equal(2, good.Result.Single().Count);
    }

    [Theory]
    [InlineData("A", "A")]
    [InlineData("B", "A")]
    public void DuplicateOrUnorderedIdentityIsInvalid(string first, string second)
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        Assert.True(CosmicGroupedResults.Materialize(query, new[]
        { new CosmicGroupedRow(1, new object[] { first }, 1), new CosmicGroupedRow(1, new object[] { second }, 1) }).IsError);
    }

    [Fact]
    public void OmittedKeyStillRequiresExactNativeIdentity()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        Assert.True(CosmicGroupedResults.Materialize(query, new[] { new CosmicGroupedRow(1, new object[] { 123 }, 1) }).IsError);
    }

    [Fact]
    public void SelectedKeyMustAgreeWithIdentity()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Key).Result;
        Assert.True(CosmicGroupedResults.Materialize(query, new[] { new CosmicGroupedRow(1, new object[] { "A" }, "B") }).IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PublishedGroupsCannotHaveNonpositiveCounts(int count)
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        Assert.True(CosmicGroupedResults.Materialize(query, new[] { new CosmicGroupedRow(1, new object[] { "A" }, count) }).IsError);
    }

    [Fact]
    public void AverageCannotExceedItsInputDomain()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Average(h => h.Version)).Result;
        Assert.True(CosmicGroupedResults.Materialize(query, new[] { new CosmicGroupedRow(1, new object[] { "A" }, (decimal)int.MaxValue + 1) }).IsError);
    }

    [Fact]
    public void NullableSumCannotPublishNull()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Sum(h => CosmicQueryMetadata.Value<long?>(h, "stock"))).Result;
        Assert.True(CosmicGroupedResults.Materialize(query, new[] { new CosmicGroupedRow(1, new object[] { "A" }, new object[] { null! }) }).IsError);
    }

    [Fact]
    public void EmptyInputReturnsNoGroups()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        var result = CosmicGroupedResults.Materialize(query, Array.Empty<CosmicGroupedRow>());
        Assert.False(result.IsError, result.Message);
        Assert.Empty(result.Result);
    }

    [Fact]
    public void GroupCountAndPayloadBudgetsAreIndependent()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name, CosmicGroupingOptions.Create(1).Result).Select(g => g.Count()).Result;
        Assert.True(CosmicGroupedResults.Materialize(query, new[]
        { new CosmicGroupedRow(1, new object[] { "A" }, 1), new CosmicGroupedRow(1, new object[] { "B" }, 1) }).IsError);
        var tiny = CosmicQuery<Holon>.Create().GroupBy(h => h.Name, CosmicGroupingOptions.Create(maximumPayloadBytes: 1).Result).Select(g => g.Count()).Result;
        Assert.True(CosmicGroupedResults.Materialize(tiny, new[] { new CosmicGroupedRow(1, new object[] { "A" }, 1) }).IsError);
    }

    [Fact]
    public void CancellationPublishesNothing()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var result = CosmicGroupedResults.Materialize(query, new[] { new CosmicGroupedRow(1, new object[] { "A" }, 1) }, cancellation.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode);
    }

    [Fact]
    public void GroupCountCannotExceedAComposedSourceWindow()
    {
        var query = CosmicQuery<Holon>.Create().Take(1).GroupBy(h => h.Name).Select(g => g.Count()).Result;
        Assert.True(CosmicGroupedResults.Materialize(query, new[]
        { new CosmicGroupedRow(1, new object[] { "A" }, 1), new CosmicGroupedRow(1, new object[] { "B" }, 1) }).IsError);
    }
    [Fact]
    public void SharedDetachedValidationRejectsNonUtcDates()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<DateTime>(h, "instant")).Result;
        var result = CosmicProjectionResults.Materialize(query, new[] { new CosmicProjectionRow(new DateTime(638000000000000000L, DateTimeKind.Unspecified)) });
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode);
    }

    [Fact]
    public void SharedDetachedValidationRejectsUndefinedEnums()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<HolonType>(h, "kind")).Result;
        var result = CosmicProjectionResults.Materialize(query, new[] { new CosmicProjectionRow((HolonType)int.MaxValue) });
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode);
    }

    [Fact]
    public void GroupKeyOrderUsesUnicodeScalarsRatherThanUtf16Units()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        var result = CosmicGroupedResults.Materialize(query, new[]
        { new CosmicGroupedRow(1, new object[] { "\uE000" }, 1), new CosmicGroupedRow(1, new object[] { "\U00010000" }, 1) });
        Assert.False(result.IsError, result.Message);
    }

    [Fact]
    public void NullableKeysUseNullFirstAndPreserveSelectedNull()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => CosmicQueryMetadata.Value<long?>(h, "optional"))
            .Select(g => g.Key).Result;
        var result = CosmicGroupedResults.Materialize(query, new[]
        { new CosmicGroupedRow(1, new object[] { null! }, new object[] { null! }), new CosmicGroupedRow(1, new object[] { 2L }, 2L) });
        Assert.False(result.IsError, result.Message);
        Assert.Null(result.Result[0]);
        Assert.Equal(2L, result.Result[1]);
    }

    [Fact]
    public void MalformedUnicodeIdentityCannotReachPublication()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        Assert.True(CosmicGroupedResults.Materialize(query, new[] { new CosmicGroupedRow(1, new object[] { "\uD800" }, 1) }).IsError);
    }

    [Fact]
    public void SelectedCountsMustAgreeWithNativeInputProof()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        Assert.True(CosmicGroupedResults.Materialize(query, new[] { new CosmicGroupedRow(2, new object[] { "A" }, 1) }).IsError);
    }

    [Fact]
    public void InputCardinalityIsRequiredEvenWhenNoCountIsSelected()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Key).Result;
        Assert.True(CosmicGroupedResults.Materialize(query, new[] { new CosmicGroupedRow(0, new object[] { "A" }, "A") }).IsError);
        var window = CosmicQuery<Holon>.Create().Take(1).GroupBy(h => h.Name).Select(g => g.Key).Result;
        Assert.True(CosmicGroupedResults.Materialize(window, new[] { new CosmicGroupedRow(2, new object[] { "A" }, "A") }).IsError);
    }

    [Fact]
    public void LongCountRetainsExactLargeInputProof()
    {
        const long count = 9007199254740993L;
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.LongCount()).Result;
        var result = CosmicGroupedResults.Materialize(query, new[] { new CosmicGroupedRow(count, new object[] { "A" }, count) });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(count, result.Result.Single());
    }

    [Fact]
    public void NumericWorkBoundIncludesNonparticipatingNullableInputs()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Sum(h => CosmicQueryMetadata.Value<long?>(h, "optional"))).Result;
        var result = CosmicGroupedResults.Materialize(query, new[] { new CosmicGroupedRow((long)int.MaxValue + 1, new object[] { "A" }, 0L) });
        Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode);
    }

    [Fact]
    public void NativeRowsSnapshotTheirCellArrays()
    {
        var keys = new object[] { "A" }; var values = new object[] { 2 };
        var row = new CosmicGroupedRow(2, keys, values);
        keys[0] = "changed"; values[0] = "wrong";
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        var result = CosmicGroupedResults.Materialize(query, new[] { row });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(2, result.Result.Single());
    }

    [Fact]
    public void NullableAverageMayRemainNullForANonemptyAllNullGroup()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Average(h => CosmicQueryMetadata.Value<long?>(h, "optional"))).Result;
        var result = CosmicGroupedResults.Materialize(query, new[] { new CosmicGroupedRow(2, new object[] { "A" }, new object[] { null! }) });
        Assert.False(result.IsError, result.Message);
        Assert.Null(result.Result.Single());
    }

}
