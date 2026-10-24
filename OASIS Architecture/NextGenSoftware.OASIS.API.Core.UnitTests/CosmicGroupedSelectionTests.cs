using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedSelectionTests
{
    [Fact]
    public void GroupSelectionCapturesKeysAndMultipleTypedAggregatesWithoutExecution()
    {
        var source = CosmicQuery<Holon>.Create().Where(h => h.Version > 0).Result;
        var grouped = source.GroupBy(h => new { h.Version, h.Name }).Result;
        var result = grouped.Select(g => new
        {
            g.Key.Name,
            Rows = g.LongCount(),
            Sum = g.Sum(h => h.Version),
            Average = g.Average(h => h.Version),
            Min = g.Min(h => h.Name),
            Max = g.Max(h => h.Version)
        });
        Assert.False(result.IsError, result.Message);
        Assert.Same(grouped, result.Result.Grouping);
        Assert.Equal(6, result.Result.Columns.Count);
        Assert.Equal(1, result.Result.Columns[0].KeyIndex);
        Assert.Equal(CosmicScalarOperation.LongCount, result.Result.Columns[1].Operation);
        Assert.Equal(typeof(decimal), result.Result.Columns[3].ValueType);
        Assert.Equal(typeof(string), result.Result.Columns[4].ValueType);
    }

    [Fact]
    public void ScalarKeySelectionRetainsItsExactNullableDomain()
    {
        var result = CosmicQuery<Holon>.Create().GroupBy(h => CosmicQueryMetadata.Value<long?>(h, "optional"))
            .Select(g => g.Key);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(typeof(long?), result.Result.Columns.Single().ValueType);
        Assert.Equal(0, result.Result.Columns.Single().KeyIndex);
    }

    [Fact]
    public void CountAndNullableAverageKeepDifferentResultDomains()
    {
        var result = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => new
        { Count = g.Count(), Average = g.Average(h => CosmicQueryMetadata.Value<long?>(h, "optional")) });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(typeof(int), result.Result.Columns[0].ValueType);
        Assert.Equal(typeof(decimal?), result.Result.Columns[1].ValueType);
    }

    [Fact]
    public void GroupedDtoConstructionIsDeferred()
    {
        var result = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => new ThrowingDto { Count = g.Count() });
        Assert.False(result.IsError, result.Message);
    }

    public sealed class ThrowingDto
    {
        public ThrowingDto() => throw new InvalidOperationException("Capture cannot invoke a result constructor.");
        public int Count { get; set; }
    }

    [Fact]
    public void ArbitraryGroupedMethodsCannotExecuteDuringCapture()
        => Assert.True(CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => Forbidden(g.Key)).IsError);

    private static string Forbidden(string _) => throw new InvalidOperationException("Never execute.");

    [Fact]
    public void AggregateSelectorCodeCannotExecuteDuringCapture()
        => Assert.True(CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Sum(h => ForbiddenNumber(h))).IsError);

    private static int ForbiddenNumber(Holon _) => throw new InvalidOperationException("Never execute.");

    [Fact]
    public void GroupedArithmeticDoesNotBecomeAClientEvaluator()
        => Assert.True(CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count() + 1).IsError);

    [Fact]
    public void UnsupportedSumDomainRejects()
        => Assert.True(CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "money"))).IsError);

    [Fact]
    public void CompositeKeyMustBeSelectedThroughExplicitFlatMembers()
        => Assert.True(CosmicQuery<Holon>.Create().GroupBy(h => new { h.Name, h.Version }).Select(g => g.Key).IsError);

    [Fact]
    public void FailedGroupingCannotBecomeASuccessfulSelection()
    {
        var grouped = CosmicQuery<Holon>.Create().GroupBy(h => Forbidden(h.Name));
        var result = grouped.Select(g => g.Count());
        Assert.True(result.IsError);
        Assert.Equal(grouped.ErrorCode, result.ErrorCode);
    }
    [Theory]
    [InlineData(32, false)]
    [InlineData(33, true)]
    public void GroupedWidthIsBoundedBeforeAnyConstructorRuns(int width, bool rejected)
    {
        var group = Expression.Parameter(typeof(ICosmicGrouping<Holon, string>), "group");
        var count = Expression.Call(group, typeof(ICosmicGrouping<Holon, string>).GetMethod("Count")!);
        var bindings = typeof(WideDto).GetProperties().OrderBy(p => p.Name, StringComparer.Ordinal)
            .Take(width).Select(property => Expression.Bind(property, count));
        var shape = Expression.MemberInit(Expression.New(typeof(WideDto)), bindings);
        var selector = Expression.Lambda<Func<ICosmicGrouping<Holon, string>, WideDto>>(shape, group);
        var result = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(selector);
        Assert.Equal(rejected, result.IsError);
        if (!rejected) Assert.Equal(width, result.Result.Columns.Count);
    }

    public sealed class WideDto
    {
        public WideDto() => throw new InvalidOperationException("Width checks cannot construct user objects.");
        public int V00 { get; set; }
        public int V01 { get; set; }
        public int V02 { get; set; }
        public int V03 { get; set; }
        public int V04 { get; set; }
        public int V05 { get; set; }
        public int V06 { get; set; }
        public int V07 { get; set; }
        public int V08 { get; set; }
        public int V09 { get; set; }
        public int V10 { get; set; }
        public int V11 { get; set; }
        public int V12 { get; set; }
        public int V13 { get; set; }
        public int V14 { get; set; }
        public int V15 { get; set; }
        public int V16 { get; set; }
        public int V17 { get; set; }
        public int V18 { get; set; }
        public int V19 { get; set; }
        public int V20 { get; set; }
        public int V21 { get; set; }
        public int V22 { get; set; }
        public int V23 { get; set; }
        public int V24 { get; set; }
        public int V25 { get; set; }
        public int V26 { get; set; }
        public int V27 { get; set; }
        public int V28 { get; set; }
        public int V29 { get; set; }
        public int V30 { get; set; }
        public int V31 { get; set; }
        public int V32 { get; set; }
    }

    [Fact]
    public void MissingGroupingAndSelectionFailWithoutExecution()
    {
        Assert.True(((CosmicGroupedQuery<Holon, string>)null!).Select(g => g.Count()).IsError);
        Assert.True(CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select((Expression<Func<ICosmicGrouping<Holon, string>, int>>)null!).IsError);
    }

}
