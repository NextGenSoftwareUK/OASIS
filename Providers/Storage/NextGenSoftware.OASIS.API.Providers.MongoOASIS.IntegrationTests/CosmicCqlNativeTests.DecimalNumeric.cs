using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    private Task WithDecimalNumericRows(object[] values, Func<MongoGraphFixture, Guid[], Task> verify, string[]? groups = null)
        => WithMongoGraph(fixture => verify(fixture, new[] { fixture.Root }.Concat(fixture.Root.Children)
                .Take(values.Length).Select(row => row.Id).ToArray()), width: Math.Max(0, values.Length - 1), configure: root =>
        {
            var rows = new[] { root }.Concat(root.Children).Take(values.Length).ToArray();
            for (var index = 0; index < rows.Length; index++)
            {
                rows[index].Name = "owned CQL graph decimal " + index.ToString("D2", CultureInfo.InvariantCulture);
                rows[index].MetaData["decimalNumeric"] = values[index];
                rows[index].MetaData["decimalGroup"] = groups?[index] ?? "one";
                rows[index].MetaData["stock"] = 7L;
            }
        });

    [Theory]
    [InlineData("0")] [InlineData("1.25")]
    [InlineData("0.0000000000000000000000000001")]
    [InlineData("79228162514264337593543950335")]
    [InlineData("-79228162514264337593543950335")]
    public Task NativeDecimalNumericScalarRetainsEveryBoundary(string text)
    {
        var value = decimal.Parse(text, CultureInfo.InvariantCulture);
        return WithDecimalNumericRows(new object[] { value }, async (fixture, ids) =>
        {
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => ids.Contains(h.Id)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")).Result));
            Assert.False(sum.IsError, sum.Message + sum.Exception); Assert.Equal(value, sum.Result);
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Average(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")).Result));
            Assert.False(mean.IsError, mean.Message + mean.Exception); Assert.Equal(value, mean.Result);
        });
    }

    [Fact]
    public Task NativeDecimalNumericCancellationPreservesTheSmallestFraction()
        => WithDecimalNumericRows(new object[] { decimal.MaxValue, decimal.MaxValue, decimal.MinValue, decimal.MinValue, 0.0000000000000000000000000001m },
            async (fixture, ids) =>
            {
                var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
                var result = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")).Result));
                Assert.False(result.IsError, result.Message + result.Exception); Assert.Equal(0.0000000000000000000000000001m, result.Result);
            });

    [Fact]
    public Task NativeDecimalNumericOverflowStillAllowsARepresentableAverage()
        => WithDecimalNumericRows(new object[] { decimal.MaxValue, decimal.MaxValue }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")).Result));
            Assert.Equal("CQL_SCALAR_OVERFLOW", sum.ErrorCode);
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Average(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")).Result));
            Assert.False(mean.IsError, mean.Message + mean.Exception); Assert.Equal(decimal.MaxValue, mean.Result);
        });

    [Fact]
    public Task NativeDecimalNumericNullableAndEmptyInputsKeepTheirContracts()
        => WithDecimalNumericRows(new object[] { null!, 1.25m }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var mean = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Average(h => CosmicQueryMetadata.Value<decimal?>(h, "decimalNumeric")).Result));
            Assert.False(mean.IsError, mean.Message + mean.Exception); Assert.Equal((decimal?)1.25m, mean.Result);
            var empty = source.Take(0).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(empty.Sum(h => CosmicQueryMetadata.Value<decimal?>(h, "decimalNumeric")).Result));
            Assert.False(sum.IsError, sum.Message + sum.Exception); Assert.Equal((decimal?)0m, sum.Result);
            var absent = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(empty.Average(h => CosmicQueryMetadata.Value<decimal?>(h, "decimalNumeric")).Result));
            Assert.False(absent.IsError, absent.Message + absent.Exception); Assert.Null(absent.Result);
        });

    [Theory]
    [InlineData("1.25")] [InlineData("invalid")] [InlineData(true)] [InlineData(null)]
    public Task NativeDecimalNumericRejectsMalformedRequiredInputs(object value)
        => WithDecimalNumericRows(new[] { value }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")).Result));
            Assert.Equal("CQL_DATA_INVALID", result.ErrorCode); Assert.False(result.IsLoaded);
        });

    [Fact]
    public Task NativeDecimalNumericGroupedMeansAndTotalsFilterOrderAndPageExactly()
        => WithDecimalNumericRows(new object[] { 0.0000000000000000000000000001m, 0.0000000000000000000000000002m, 1m, 2m }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")) })
                .Where(g => g.Total >= 0.0000000000000000000000000003m && g.Mean >= 0.0000000000000000000000000002m)
                .OrderByDescending(g => g.Mean).ThenBy(g => g.Key).Skip(1).Take(1).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result.Providers.Single().Rows);
            Assert.Equal("tiny", row.Key); Assert.Equal(0.0000000000000000000000000003m, row.Total);
            Assert.Equal(0.0000000000000000000000000002m, row.Mean);
        }, groups: new[] { "tiny", "tiny", "whole", "whole" });

    [Fact]
    public Task NativeDecimalNumericGroupedOverflowCannotHideBehindAnEmptyPage()
        => WithDecimalNumericRows(new object[] { decimal.MaxValue, decimal.MaxValue }, async (fixture, ids) =>
        {
            var groups = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"));
            var sum = groups.Select(g => g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric"))).Take(0).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(sum), new[] { fixture.Provider });
            Assert.Equal("CQL_SCALAR_OVERFLOW", result.ErrorCode); Assert.Null(result.Result);
        });

    [Fact]
    public Task NativeDecimalNumericMixedGroupedPacketsRetainEachDomain()
        => WithDecimalNumericRows(new object[] { 1.25m, 1.25m }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { Integer = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                    Decimal = g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")),
                    Minimum = g.Min(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")) }).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result.Providers.Single().Rows);
            Assert.Equal(14L, row.Integer); Assert.Equal(2.5m, row.Decimal); Assert.Equal(1.25m, row.Mean); Assert.Equal(1.25m, row.Minimum);
        });

    [Fact]
    public Task NativeDecimalNumericGroupedSumMatchesItsExactKey()
        => WithDecimalNumericRows(new object[] { decimal.MaxValue }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).GroupBy(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")) }).Where(g => g.Total == g.Key).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception); Assert.Equal(decimal.MaxValue, Assert.Single(result.Result.Providers.Single().Rows).Total);
        });

    [Fact]
    public Task NativeDecimalNumericSourceWindowExcludesMalformedLaterValues()
        => WithDecimalNumericRows(new object[] { 1.25m, "outside window" }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).OrderBy(h => h.Name).Take(1).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")).Result));
            Assert.False(result.IsError, result.Message + result.Exception); Assert.Equal(1.25m, result.Result);
        });
    [Fact]
    public Task NativeDecimalNumericNullableGroupsPublishZeroAndNull()
        => WithDecimalNumericRows(new object[] { null!, decimal.MaxValue }, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<decimal?>(h, "decimalNumeric")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<decimal?>(h, "decimalNumeric")) })
                .Where(g => g.Total == 0m && g.Mean == null).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result.Providers.Single().Rows);
            Assert.Equal("empty", row.Key); Assert.Equal((decimal?)0m, row.Total); Assert.Null(row.Mean);
        }, groups: new[] { "empty", "full" });

    [Fact]
    public Task NativeDecimalNumericGroupedMeansAvoidDoubleRoundingAtCoefficientOverflow()
    {
        var values = Enumerable.Repeat((object)7.922816251426433759354395035m, 7)
            .Concat(Enumerable.Repeat((object)7.922816251426433759354395036m, 6)).ToArray();
        return WithDecimalNumericRows(values, async (fixture, ids) =>
        {
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { Mean = g.Average(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")) })
                .Where(g => g.Mean == 7.922816251426433759354395035m).OrderBy(g => g.Mean).Take(1).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(7.922816251426433759354395035m, Assert.Single(result.Result.Providers.Single().Rows).Mean);
        });
    }

    [Fact]
    public Task NativeDecimalNumericInputIsIndependentOfGraphPublicationBudget()
        => WithDecimalNumericRows(Enumerable.Repeat((object)0.0000000000000000000000000001m, 137).ToArray(), async (fixture, ids) =>
        {
            var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot, maximumNodes: 1).Result;
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().WithChildren(graph)
                .Where(h => h.Name.StartsWith("owned CQL graph decimal ", StringComparison.Ordinal)).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")).Result));
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(137m * 0.0000000000000000000000000001m, result.Result);
        });

    [Fact]
    public Task NativeDecimalNumericPreCancellationLeavesTheProviderReusable()
        => WithDecimalNumericRows(new object[] { 1.25m, 1.25m }, async (fixture, ids) =>
        {
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var plan = ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")).Result);
            using var cancellation = new System.Threading.CancellationTokenSource(); cancellation.Cancel();
            var cancelled = await fixture.Provider.ExecuteCosmicScalarAsync(plan, cancellation.Token);
            Assert.Equal("CQL_QUERY_CANCELLED", cancelled.ErrorCode);
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(plan);
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception); Assert.Equal(2.5m, fresh.Result);
        });

    [Fact]
    public Task NativeDecimalNumericRejectsUnrepresentableOriginalDecimal128()
        => WithDecimalNumericRows(new object[] { 1.25m }, async (fixture, ids) =>
        {
            fixture.KnownOutcome = false;
            var changed = await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId", ids[0].ToString("D")),
                Builders<BsonDocument>.Update.Set("MetaData.decimalNumeric", new BsonDecimal128(Decimal128.Parse("1.000000000000000000000000000000001"))));
            Assert.True(changed.IsAcknowledged); fixture.KnownOutcome = true; Assert.Equal(1, changed.MatchedCount);
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")).Result));
            Assert.Equal("CQL_DATA_INVALID", result.ErrorCode); Assert.False(result.IsLoaded);
        });

    [Fact]
    public Task NativeDecimalNumericPrivacyExcludesMalformedHiddenInputs()
    {
        var actor = Guid.NewGuid(); var foreign = Guid.NewGuid();
        return WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root }.Concat(fixture.Root.Children).Select(row => row.Id).ToArray();
            var source = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
                .Where(h => ids.Contains(h.Id)).Result;
            var sum = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")).Result));
            Assert.False(sum.IsError, sum.Message + sum.Exception); Assert.Equal(3.75m, sum.Result);
            var query = source.GroupBy(h => CosmicQueryMetadata.Value<string>(h, "decimalGroup"))
                .Select(g => new { Count = g.Count(), Mean = g.Average(h => CosmicQueryMetadata.Value<decimal>(h, "decimalNumeric")) }).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result.Providers.Single().Rows); Assert.Equal(2, row.Count); Assert.Equal(1.875m, row.Mean);
        }, manifest: false, configure: root =>
        {
            var rows = new[] { root }.Concat(root.Children).ToArray();
            for (var index = 0; index < rows.Length; index++)
            {
                rows[index].CreatedByAvatarId = index == 0 ? actor : foreign;
                rows[index].IsPublic = index == 1;
                rows[index].MetaData["decimalGroup"] = "one";
                rows[index].MetaData["decimalNumeric"] = index == 0 ? (object)1.25m : index == 1 ? 2.5m : "hidden malformed";
            }
        });
    }

}
