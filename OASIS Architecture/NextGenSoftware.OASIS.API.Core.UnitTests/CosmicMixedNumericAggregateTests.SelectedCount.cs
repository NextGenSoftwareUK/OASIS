using System.Globalization;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicMixedNumericAggregateTests
{
    public static IEnumerable<object[]> SelectedCountCases()
    {
        foreach (var type in new[] { typeof(string), typeof(bool), typeof(int), typeof(long), typeof(double) })
            foreach (var distinct in new[] { false, true }) yield return new object[] { type, distinct };
    }

    [Theory]
    [MemberData(nameof(SelectedCountCases))]
    public async Task ValueCountReconcilesIdentityBeforeNullAndDistinctSemantics(Type type, bool distinct)
    {
        object Value(int value) => type == typeof(string) ? "v" + value
            : type == typeof(bool) ? value == 1 : Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        var f = await Create(type, true, new[] { Value(9), Value(1), null! },
            new[] { Value(1), Value(2), null! }, overlap: 1);
        var result = CosmicMixedProjectionResults.CountValues(f.Query, Actor, f.Mapping, f.Stored, f.Live, distinct);
        Assert.False(result.IsError, result.Message); Assert.True(result.IsLoaded);
        Assert.Equal(distinct ? 2L : 3L, result.Result);
    }

    [Theory]
    [MemberData(nameof(SelectedCountCases))]
    public async Task EmptyAndAllNullSelectedCountsAreLoadedZero(Type type, bool distinct)
    {
        foreach (var values in new[] { Array.Empty<object>(), new object[] { null! } })
        {
            var f = await Create(type, true, values, values);
            var result = CosmicMixedProjectionResults.CountValues(f.Query, Actor, f.Mapping, f.Stored, f.Live, distinct);
            Assert.False(result.IsError, result.Message); Assert.True(result.IsLoaded); Assert.Equal(0L, result.Result);
        }
    }

    [Fact]
    public async Task SelectedDistinctInt64NeverCoercesThroughDouble()
    {
        var f = await Create(typeof(long), true, new object[] { 9007199254740992L, 9007199254740993L },
            new object[] { 9007199254740993L, long.MaxValue });
        var r = CosmicMixedProjectionResults.CountValues(f.Query, Actor, f.Mapping, f.Stored, f.Live, true);
        Assert.False(r.IsError, r.Message); Assert.Equal(3L, r.Result);
    }

    [Fact]
    public async Task SelectedDistinctStringUsesOrdinalCaseAndUnicodeWithoutNormalization()
    {
        var f = await Create(typeof(string), true, new object[] { "A", "a", "\u00e9", "" },
            new object[] { "A", "e\u0301", "", null! });
        var r = CosmicMixedProjectionResults.CountValues(f.Query, Actor, f.Mapping, f.Stored, f.Live, true);
        Assert.False(r.IsError, r.Message); Assert.Equal(5L, r.Result);
    }

    [Fact]
    public async Task SelectedDistinctFiniteDoubleEquatesSignedZeroButPreservesAdjacentValues()
    {
        var f = await Create(typeof(double), true, new object[] { 0d, -0d, 1d },
            new object[] { double.Epsilon, Math.BitIncrement(1d), 1d });
        var r = CosmicMixedProjectionResults.CountValues(f.Query, Actor, f.Mapping, f.Stored, f.Live, true);
        Assert.False(r.IsError, r.Message); Assert.Equal(4L, r.Result);
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("mapping")]
    [InlineData("duplicate")]
    [InlineData("full")]
    [InlineData("shortened")]
    [InlineData("payload")]
    [InlineData("cancelled")]
    [InlineData("null-query")]
    public async Task SelectedCountRequiresTheWholeAdmittedInputEvenIfEveryValueWouldBeDistinct(string invalid)
    {
        var f = await Create(typeof(int), true, new object[] { 3 },
            invalid == "full" ? Enumerable.Repeat((object)2, 100).ToArray() : new object[] { 2 },
            readLimit: invalid == "shortened" ? 1 : 100,
            sourceActor: invalid == "actor" ? Guid.NewGuid() : null);
        using var cancellation = new CancellationTokenSource();
        if (invalid == "cancelled") cancellation.Cancel();
        var r = CosmicMixedProjectionResults.CountValues(invalid == "null-query" ? null! : f.Query, Actor,
            invalid == "mapping" ? Guid.NewGuid() : f.Mapping, f.Stored,
            invalid == "duplicate" ? new[] { f.Live[0], f.Live[0] } : f.Live, true,
            invalid == "payload" ? 1 : CosmicMixedProjectionResults.MaximumInputBytes, cancellation.Token);
        Assert.True(r.IsError);
        Assert.Equal(invalid switch { "actor" => "CQL_ACCESS_INVALID", "full" or "shortened" => "CQL_VIRTUAL_SOURCE_LIMIT",
            "null-query" => "CQL_MIXED_QUERY_INVALID", "payload" => "CQL_RESULT_LIMIT",
            "cancelled" => "CQL_QUERY_CANCELLED", _ => "CQL_PROVIDER_RESULT_INVALID" }, r.ErrorCode);
    }
}
