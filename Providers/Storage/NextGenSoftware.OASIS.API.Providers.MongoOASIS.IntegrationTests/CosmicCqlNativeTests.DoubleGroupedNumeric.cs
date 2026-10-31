using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public Task NativeDoubleGroupedBoundaryMatchesItsExactKey(int sample)
    {
        var value = new double[] { 0d, double.Epsilon, -double.Epsilon, double.MaxValue, double.MinValue, 0.1d }[sample];
        return WithDecimalNumericRows(new object[] { value }, async (fixture, ids) =>
        {
            var query = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")) })
                .Where(g => g.Total == g.Key).OrderBy(g => g.Total).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(value, Assert.Single(result.Result.Providers.Single().Rows).Total);
        });
    }

    [Fact]
    public Task NativeDoubleGroupedRoundedMeansFilterOrderAndPage()
        => WithDecimalNumericRows(new object[] { double.Epsilon, 2d * double.Epsilon, 1d, 2d }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")) })
                .Where(g => g.Total >= 3d * double.Epsilon && g.Mean >= 2d * double.Epsilon)
                .OrderByDescending(g => g.Mean).ThenBy(g => g.Key).Skip(1).Take(1).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result.Providers.Single().Rows);
            Assert.Equal("tiny", row.Key); Assert.Equal(3d * double.Epsilon, row.Total); Assert.Equal(2d * double.Epsilon, row.Mean);
        }, groups: new[] { "tiny", "tiny", "whole", "whole" });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeDoubleGroupedOverflowCannotHideOffPage(bool filtered)
        => WithDecimalNumericRows(new object[] { double.MaxValue, double.MaxValue }, async (fixture, ids) =>
        {
            var selected = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { Total = g.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")) });
            var query = filtered ? selected.Where(g => g.Total == 0d).Result : selected.Take(0).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.Equal("CQL_SCALAR_OVERFLOW", result.ErrorCode); Assert.Null(result.Result);
        });

    [Fact]
    public Task NativeDoubleGroupedNullableZeroAndNullRetainDistinctComparisons()
        => WithDecimalNumericRows(new object[] { null!, double.MaxValue }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<double?>(h, "decimalNumeric")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<double?>(h, "decimalNumeric")) })
                .Where(g => g.Total == 0d && g.Mean == null).OrderBy(g => g.Mean).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result.Providers.Single().Rows);
            Assert.Equal("empty", row.Key); Assert.Equal((double?)0d, row.Total); Assert.Null(row.Mean);
        }, groups: new[] { "empty", "full" });

    [Fact]
    public Task NativeDoubleGroupedMixedPacketsKeepTheirOwnDomains()
        => WithDecimalNumericRows(new object[] { 1.25m, 1.25m }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { Integer = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                    Decimal = g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")),
                    Total = g.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")),
                    Minimum = g.Min(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")) }).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result.Providers.Single().Rows);
            Assert.Equal(14L, row.Integer); Assert.Equal(2.5m, row.Decimal); Assert.Equal(2.5d, row.Total);
            Assert.Equal(1.25d, row.Mean); Assert.Equal(1.25d, row.Minimum);
        });

    [Fact]
    public Task NativeDoubleGroupedSourceWindowExcludesMalformedLaterRows()
        => WithDecimalNumericRows(new object[] { 1.25d, "outside window" }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).OrderBy(h => h.Name).Take(1)
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric"))).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception); Assert.Equal(1.25d, Assert.Single(result.Result.Providers.Single().Rows));
        });

    [Fact]
    public Task NativeDoubleGroupedExactCancellationPreservesSubnormal()
        => WithDecimalNumericRows(new object[] { double.MaxValue, double.MaxValue, double.MinValue, double.MinValue, double.Epsilon }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { Total = g.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")) })
                .Where(g => g.Total == double.Epsilon).OrderBy(g => g.Total).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception); Assert.Equal(double.Epsilon, Assert.Single(result.Result.Providers.Single().Rows).Total);
        });

    [Fact]
    public Task NativeDoubleGroupedHalfwayMeanMatchesSelectedPredicate()
        => WithDecimalNumericRows(new object[] { double.Epsilon, 0d }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { Mean = g.Average(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric")) })
                .Where(g => g.Mean == 0d).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception); Assert.Equal(0d, Assert.Single(result.Result.Providers.Single().Rows).Mean);
        });

    [Fact]
    public Task NativeDoubleGroupedCancellationLeavesOwnerReusable()
        => WithDecimalNumericRows(new object[] { 1.25d }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<double>(h, "decimalNumeric"))).Result;
            var plan = GroupedPlan(query);
            using var cancellation = new System.Threading.CancellationTokenSource(); cancellation.Cancel();
            var cancelled = await fixture.Provider.ExecuteCosmicGroupedAsync(plan, cancellationToken: cancellation.Token);
            Assert.Equal("CQL_QUERY_CANCELLED", cancelled.ErrorCode);
            var fresh = await fixture.Provider.ExecuteCosmicGroupedAsync(plan);
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception);
        });

    [Fact]
    public Task NativeDoubleGroupedPrivacyExcludesMalformedHiddenInputs()
    {
        var actor = Guid.NewGuid(); var foreign = Guid.NewGuid();
        return WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root }.Concat(fixture.Root.Children).Select(row => row.Id).ToArray();
            var query = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
                .Where(h => ids.Contains(h.Id)).GroupBy(h => CosmicQueryMetadata.Value<string>(h, "floatingGroup"))
                .Select(g => new { Count = g.Count(), Mean = g.Average(h => CosmicQueryMetadata.Value<double>(h, "floatingNumeric")) }).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result.Providers.Single().Rows); Assert.Equal(2, row.Count); Assert.Equal(1.875d, row.Mean);
        }, manifest: false, configure: root =>
        {
            var rows = new[] { root }.Concat(root.Children).ToArray();
            for (var index = 0; index < rows.Length; index++)
            {
                rows[index].CreatedByAvatarId = index == 0 ? actor : foreign;
                rows[index].IsPublic = index == 1;
                rows[index].MetaData["floatingGroup"] = "one";
                rows[index].MetaData["floatingNumeric"] = index == 0 ? (object)1.25d : index == 1 ? 2.5d : "hidden malformed";
            }
        });
    }
}
