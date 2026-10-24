using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedFilterProofTests
{
    [Fact]
    public void NativeMembershipMustMatchFilterThenPageOverCompleteProof()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { g.Key, Rows = g.LongCount() }).Where(row => row.Rows > 1).Skip(1).Take(1).Result;
        var rows = new[] { new CosmicGroupedRow(3, false, new object[] { "A" }, "A", 3L),
            new CosmicGroupedRow(1, false, new object[] { "B" }, "B", 1L),
            new CosmicGroupedRow(2, true, new object[] { "C" }, "C", 2L) };
        var result = CosmicGroupedResults.Materialize(query, rows);
        Assert.False(result.IsError, result.Message); Assert.Equal("C", Assert.Single(result.Result).Key);
        rows[1] = new CosmicGroupedRow(1, true, new object[] { "B" }, "B", 1L);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicGroupedResults.Materialize(query, rows).ErrorCode);
    }

    [Fact]
    public void ExcludedRowsStillRequireExactDomainsAndCounts()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.LongCount()).Where(rows => false).Result;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicGroupedResults.Materialize(query,
            new[] { new CosmicGroupedRow(1, false, new object[] { "A" }, "wrong") }).ErrorCode);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicGroupedResults.Materialize(query,
            new[] { new CosmicGroupedRow(1, false, new object[] { "A" }, 2L) }).ErrorCode);
        Assert.False(CosmicGroupedResults.Materialize(query,
            new[] { new CosmicGroupedRow(1, false, new object[] { "A" }, 1L) }).IsError);
    }

    [Theory]
    [InlineData(null, true)] [InlineData(2L, false)] [InlineData(3L, true)]
    public void NullableInequalityAndNegationRetainLiftedSemantics(long? value, bool matches)
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => g.Min(h => CosmicQueryMetadata.Value<long?>(h, "stock"))).Where(min => !(min == 2L)).Result;
        var proof = new[] { new CosmicGroupedRow(1, matches, new object[] { "A" }, value) };
        Assert.False(CosmicGroupedResults.Materialize(query, proof).IsError);
    }

    [Theory]
    [InlineData(9007199254740992L, false)] [InlineData(9007199254740993L, true)]
    public void MixedDecimalComparisonDoesNotRoundInt64Cells(long value, bool matches)
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")))
            .Where(total => total > 9007199254740992m).Result;
        Assert.False(CosmicGroupedResults.Materialize(query,
            new[] { new CosmicGroupedRow(1, matches, new object[] { "A" }, value) }).IsError);
    }

    [Fact]
    public void SharedGrammarSupportsMembershipStringsAndFieldComparisonProof()
    {
        var allowed = new[] { "coffee", "tea" };
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { Name = g.Key, Rows = g.LongCount(), Total = g.Sum(h => h.Version) })
            .Where(row => allowed.Contains(row.Name) && row.Name.StartsWith("c", StringComparison.Ordinal) && row.Rows > row.Total).Result;
        var result = CosmicGroupedResults.Materialize(query,
            new[] { new CosmicGroupedRow(3, true, new object[] { "coffee" }, "coffee", 3L, 2),
                new CosmicGroupedRow(1, false, new object[] { "tea" }, "tea", 1L, 0) });
        Assert.False(result.IsError, result.Message); Assert.Equal("coffee", Assert.Single(result.Result).Name);
    }

    [Fact]
    public void FalseFilterCannotHideDuplicateIdentityOrPayloadLimit()
    {
        var grouping = CosmicQuery<Holon>.Create().GroupBy(h => h.Name, CosmicGroupingOptions.Create(10, 100).Result);
        var query = grouping.Select(g => g.LongCount()).Where(rows => false).Result;
        Assert.Equal("CQL_RESULT_LIMIT", CosmicGroupedResults.Materialize(query,
            new[] { new CosmicGroupedRow(1, false, new object[] { new string('x', 200) }, 1L) }).ErrorCode);
        query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.LongCount()).Where(rows => false).Result;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicGroupedResults.Materialize(query,
            new[] { new CosmicGroupedRow(1, false, new object[] { "A" }, 1L),
                new CosmicGroupedRow(1, false, new object[] { "A" }, 1L) }).ErrorCode);
    }

    [Theory]
    [InlineData(0, true)] [InlineData(1, false)]
    public void Binary64SubnormalLiteralKeepsItsExactPositiveValue(int value, bool matches)
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => g.Sum(h => h.Version)).Where(total => total < double.Epsilon).Result;
        Assert.False(CosmicGroupedResults.Materialize(query,
            new[] { new CosmicGroupedRow(1, matches, new object[] { "A" }, value) }).IsError);
    }

    [Theory]
    [InlineData("0.3333333333333333333333333333", true)] [InlineData("-0.1", false)]
    public void NormalizedDecimalAverageProofKeepsFractionAndSign(string text, bool matches)
    {
        var value = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Average(h => h.Version)).Where(mean => mean > 0.3m).Result;
        Assert.False(CosmicGroupedResults.Materialize(query,
            new[] { new CosmicGroupedRow(3, matches, new object[] { "A" }, value) }).IsError);
    }

    [Fact]
    public void GuidComparisonProofUsesExactDetachedDomain()
    {
        var first = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var second = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Id).Select(g => g.Key).Where(id => id == second).Result;
        var result = CosmicGroupedResults.Materialize(query, new[] {
            new CosmicGroupedRow(1, false, new object[] { first }, first), new CosmicGroupedRow(1, true, new object[] { second }, second) });
        Assert.False(result.IsError, result.Message); Assert.Equal(second, Assert.Single(result.Result));
    }

    [Fact]
    public void DefinedEnumProofUsesExactIntegralLiteralWithoutCustomConversion()
    {
        var value = NextGenSoftware.OASIS.API.Core.Enums.HolonType.Quest;
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.HolonType).Select(g => g.Key).Where(type => type == value).Result;
        Assert.False(CosmicGroupedResults.Materialize(query,
            new[] { new CosmicGroupedRow(1, true, new object[] { value }, value) }).IsError);
    }

    [Fact]
    public void DateComparisonProofRequiresUtcAndPreservesTicks()
    {
        var boundary = new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
        var next = boundary.AddTicks(1);
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.CreatedDate).Select(g => g.Key).Where(date => date > boundary).Result;
        var result = CosmicGroupedResults.Materialize(query, new[] {
            new CosmicGroupedRow(1, false, new object[] { boundary }, boundary), new CosmicGroupedRow(1, true, new object[] { next }, next) });
        Assert.False(result.IsError, result.Message); Assert.Equal(next, Assert.Single(result.Result));
    }
}
