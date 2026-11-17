using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class NativeRecordFilterTests
{
    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(9007199254740993L)]
    [InlineData(long.MaxValue)]
    public void LengthRetainsTheInputDomainAndExactBound(long bound)
    {
        var filter = new NativeRecordFilter("label", CosmicQueryComparisonOperator.GreaterThanOrEqual,
            bound, NativeRecordFunction.Utf16Length);
        Assert.Equal(typeof(string), filter.InputType);
        Assert.Equal(bound, Assert.IsType<long>(filter.Value));
        var rebound = filter.WithField("source_label");
        Assert.Equal("label", filter.Field);
        Assert.Equal("source_label", rebound.Field);
        Assert.Equal(filter.Operator, rebound.Operator);
        Assert.Equal(filter.Function, rebound.Function);
        Assert.Equal(filter.Value, rebound.Value);
    }

    [Theory]
    [InlineData(NativeRecordFunction.Contains)]
    [InlineData(NativeRecordFunction.StartsWith)]
    [InlineData(NativeRecordFunction.EndsWith)]
    public void StringMatchesRetainLiteralAndRejectUnsupportedDomains(NativeRecordFunction function)
    {
        var value = "$.*'\u0000\U0001F600";
        var filter = new NativeRecordFilter("alias", CosmicQueryComparisonOperator.Equal, value, function);
        Assert.Equal(typeof(string), filter.InputType);
        Assert.Equal(value, filter.WithField("source").Value);
        Assert.Equal(function, filter.WithField("source").Function);
        Assert.Throws<ArgumentException>(() => new NativeRecordFilter("x", CosmicQueryComparisonOperator.Equal, 1L, function));
        Assert.Throws<ArgumentException>(() => new NativeRecordFilter("x", CosmicQueryComparisonOperator.NotEqual, value, function));
        Assert.Throws<ArgumentException>(() => new NativeRecordFilter("x", CosmicQueryComparisonOperator.Equal, new string('a', 4097), function));
    }

    [Fact]
    public void EqualityAndTypedFiltersCaptureCallerOwnedCollections()
    {
        var pairs = new List<KeyValuePair<string, object>> { new("tier", "gold") };
        var first = NativeRecordFilter.CaptureEqualities(pairs);
        pairs[0] = new("tier", "silver");
        Assert.Equal("gold", first[0].Value);
        Assert.Equal(CosmicQueryComparisonOperator.Equal, first[0].Operator);
        var filters = first.ToArray();
        var captured = NativeRecordFilter.Capture(filters);
        filters[0] = new("points", CosmicQueryComparisonOperator.GreaterThan, 7L);
        Assert.Equal("tier", captured[0].Field);
        Assert.Equal("gold", captured[0].Value);
    }

    [Fact]
    public void InvalidOrUnsupportedDomainsCannotBecomeNativeQueryText()
    {
        Assert.Throws<ArgumentException>(() => new NativeRecordFilter("x", (CosmicQueryComparisonOperator)999, 1L));
        Assert.Throws<ArgumentException>(() => new NativeRecordFilter("x", CosmicQueryComparisonOperator.Equal,
            1L, (NativeRecordFunction)999));
        Assert.Throws<ArgumentException>(() => new NativeRecordFilter("x", CosmicQueryComparisonOperator.Equal,
            1, NativeRecordFunction.Utf16Length));
        Assert.Throws<ArgumentException>(() => new NativeRecordFilter("x", CosmicQueryComparisonOperator.GreaterThan, "a"));
        Assert.Throws<ArgumentException>(() => new NativeRecordFilter("x", CosmicQueryComparisonOperator.GreaterThan, true));
        Assert.Throws<ArgumentException>(() => new NativeRecordFilter("x", CosmicQueryComparisonOperator.Equal, new object()));
        Assert.Throws<ArgumentException>(() => new NativeRecordFilter("x", CosmicQueryComparisonOperator.Equal, null));
        Assert.Throws<ArgumentException>(() => new NativeRecordFilter("x\n", CosmicQueryComparisonOperator.Equal, 1L));
        Assert.Throws<ArgumentException>(() => NativeRecordFilter.Capture(Array.Empty<NativeRecordFilter>()));
        Assert.Throws<ArgumentException>(() => NativeRecordFilter.Capture(new NativeRecordFilter[] { null }));
        Assert.Throws<ArgumentException>(() => NativeRecordFilter.CaptureEqualities(
            Enumerable.Range(0, 9).Select(i => new KeyValuePair<string, object>("x", (long)i)).ToArray()));
    }
}
