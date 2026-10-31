using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Fact]
    public Task NativeGroupedNullableCompositeIdentityRetainsEmptyAggregateSemantics()
        => WithMongoGraph(async fixture =>
        {
            var selection = CosmicQuery<Holon>.Create()
                .Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal))
                .GroupBy(h => new { Category = CosmicQueryMetadata.Value<string>(h, "absentCategory"), h.Version })
                .Select(g => new { g.Key.Category, g.Key.Version, Rows = g.LongCount(),
                    Total = g.Sum(h => CosmicQueryMetadata.Value<long?>(h, "absentStock")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<long?>(h, "absentStock")),
                    Min = g.Min(h => CosmicQueryMetadata.Value<long?>(h, "absentStock")),
                    Max = g.Max(h => CosmicQueryMetadata.Value<long?>(h, "absentStock")) }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selection));
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result);
            Assert.Null(row.Keys[0]); Assert.Equal(1, row.Keys[1]);
            Assert.Equal(4L, row.Values[2]); Assert.Equal(0L, row.Values[3]); Assert.Null(row.Values[4]);
            Assert.Null(row.Values[5]); Assert.Null(row.Values[6]);
        }, configure: root =>
        {
            // Explicit NULL and absent paths share one nullable identity.
            root.MetaData["absentCategory"] = null!;
            root.MetaData["absentStock"] = null!;
        });

    [Fact]
    public Task NativeGroupedExactCancellationAvoidsIntermediateInt64Overflow()
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id, fixture.Root.Children.Last().Id };
            var selection = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).GroupBy(h => h.Version)
                .Select(g => new { Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                    Mean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selection));
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result);
            Assert.Equal(long.MaxValue, row.Values[0]);
            Assert.Equal(decimal.Divide(long.MaxValue, 3m), row.Values[1]);
        }, configure: root =>
        {
            root.MetaData["stock"] = long.MaxValue;
            root.Children.First().MetaData["stock"] = 1L;
            root.Children.Last().MetaData["stock"] = -1L;
        });

    [Fact]
    public Task NativeGroupedTrueFinalOverflowIsExplicit()
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var selection = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).GroupBy(h => h.Version)
                .Select(g => g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock"))).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selection));
            Assert.True(result.IsError); Assert.Equal("CQL_SCALAR_OVERFLOW", result.ErrorCode); Assert.Null(result.Result);
        }, configure: root =>
        {
            root.MetaData["stock"] = long.MaxValue;
            root.Children.First().MetaData["stock"] = long.MaxValue;
        });

    [Fact]
    public Task NativeGroupedOutputBudgetDoesNotLimitInputParticipation()
        => WithMongoGraph(async fixture =>
        {
            var selection = CosmicQuery<Holon>.Create()
                .Where(h => h.Name.StartsWith("owned CQL graph child", StringComparison.Ordinal))
                .GroupBy(h => h.Version).Select(g => new { Rows = g.LongCount(), Total = g.Sum(h => h.Version) }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selection), maximumGroups: 1);
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result);
            Assert.Equal(137L, row.Values[0]); Assert.Equal(137, row.Values[1]);
        }, width: 137);

    [Fact]
    public Task NativeGroupedAccessExcludesHiddenMalformedInputsBeforeReduction()
    {
        var owner = Guid.NewGuid(); var reader = Guid.NewGuid();
        return WithMongoGraph(async fixture =>
        {
            var selection = CosmicQuery<Holon>.Create()
                .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(reader).Result)
                .Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal))
                .GroupBy(h => h.Version).Select(g => new { Rows = g.LongCount(),
                    Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selection));
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result);
            Assert.Equal(1L, row.Values[0]); Assert.Equal(7L, row.Values[1]);
        }, actor: owner, configure: root =>
        {
            root.IsPublic = true; root.MetaData["stock"] = 7L;
            foreach (var child in root.Children) child.MetaData["stock"] = "hidden invalid integer";
        });
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeGroupedInvalidParticipatingKeyOrValueNeverPublishes(bool invalidKey)
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var selection = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
                .GroupBy(h => CosmicQueryMetadata.Value<long>(h, "key"))
                .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selection));
            Assert.True(result.IsError); Assert.Equal("CQL_DATA_INVALID", result.ErrorCode); Assert.Null(result.Result);
        }, configure: root =>
        {
            root.MetaData["key"] = invalidKey ? (object)"invalid" : 1L;
            root.MetaData["stock"] = invalidKey ? (object)1L : "invalid";
        });

    [Fact]
    public Task NativeGroupedUnicodeOrderingMatchesCoreScalarOrder()
        => WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var selection = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
                .Select(g => new { g.Key, Rows = g.LongCount() }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selection));
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(new object[] { "\uE000", "\U00010000" }, result.Result.Select(r => r.Keys[0]));
        }, configure: root =>
        {
            root.MetaData["category"] = "\U00010000";
            root.Children.First().MetaData["category"] = "\uE000";
        });

    [Fact]
    public Task NativeGroupedGuidKeysUseLogicalIdentityAcrossStringCase()
    {
        var key = Guid.NewGuid();
        return WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id };
            var selection = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<Guid>(h, "groupId"))
                .Select(g => new { g.Key, Rows = g.LongCount() }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selection));
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = Assert.Single(result.Result);
            Assert.Equal(key, row.Keys[0]); Assert.Equal(2L, row.Values[1]);
        }, configure: root =>
        {
            root.MetaData["groupId"] = key.ToString("D").ToLowerInvariant();
            root.Children.First().MetaData["groupId"] = key.ToString("D").ToUpperInvariant();
        });
    }

    [Fact]
    public Task NativeGroupedTimestampIdentityRetainsSubMillisecondTicks()
    {
        var instant = new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);
        return WithMongoGraph(async fixture =>
        {
            var ids = new[] { fixture.Root.Id, fixture.Root.Children.First().Id, fixture.Root.Children.Last().Id };
            var selection = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<DateTime>(h, "groupDate"))
                .Select(g => new { g.Key, Rows = g.LongCount() }).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(selection));
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(2, result.Result.Count);
            Assert.Equal(instant.AddTicks(1), result.Result[0].Keys[0]);
            Assert.Equal(2L, result.Result[0].Values[1]);
            Assert.Equal(instant.AddTicks(2), result.Result[1].Keys[0]);
        }, configure: root =>
        {
            root.MetaData["groupDate"] = instant.AddTicks(1);
            root.Children.First().MetaData["groupDate"] = instant.AddTicks(2);
            root.Children.Last().MetaData["groupDate"] = instant.AddTicks(1);
        });
    }

    [Fact]
    public Task NativeGroupedEmptyWindowCannotHideProtectedTraversalLimits()
        => WithMongoGraph(async fixture =>
        {
            var invalid = DescendantPlan(fixture, 2, 2).Query.Take(0)
                .GroupBy(h => h.Version).Select(g => g.LongCount()).Result;
            var rejected = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(invalid));
            Assert.True(rejected.IsError); Assert.Equal("CQL_QUERY_LIMIT", rejected.ErrorCode);
            var valid = DescendantPlan(fixture, 2, 3).Query.Take(0)
                .GroupBy(h => h.Version).Select(g => g.LongCount()).Result;
            var empty = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(valid));
            Assert.False(empty.IsError, empty.Message + empty.Exception); Assert.Empty(empty.Result);
        });

    [Fact]
    public Task NativeGroupedCancellationLeavesTheProviderReusable()
        => WithMongoGraph(async fixture =>
        {
            var selection = CosmicQuery<Holon>.Create()
                .Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal))
                .GroupBy(h => h.Version).Select(g => g.LongCount()).Result;
            var plan = GroupedPlan(selection);
            using var cancellation = new System.Threading.CancellationTokenSource();
            cancellation.Cancel();
            var cancelled = await fixture.Provider.ExecuteCosmicGroupedAsync(plan, cancellationToken: cancellation.Token);
            Assert.True(cancelled.IsError); Assert.Equal("CQL_QUERY_CANCELLED", cancelled.ErrorCode);
            var fresh = await fixture.Provider.ExecuteCosmicGroupedAsync(plan);
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception);
            Assert.Equal(4L, Assert.Single(fresh.Result).Values[0]);
        });

    [Fact]
    public Task NativeGroupedRemainingBudgetsRejectWithoutSkippingNonemptyResults()
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var selection = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
                .GroupBy(h => h.Version).Select(g => g.LongCount()).Result;
            var plan = GroupedPlan(selection);
            var payload = await fixture.Provider.ExecuteCosmicGroupedAsync(plan, maximumPayloadBytes: 1);
            Assert.Equal("CQL_QUERY_LIMIT", payload.ErrorCode); Assert.Null(payload.Result);
            var groups = await fixture.Provider.ExecuteCosmicGroupedAsync(plan, maximumGroups: 0);
            Assert.Equal("CQL_QUERY_LIMIT", groups.ErrorCode); Assert.Null(groups.Result);
            var emptySelection = CosmicQuery<Holon>.Create().Take(0)
                .GroupBy(h => h.Version).Select(g => g.LongCount()).Result;
            var empty = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(emptySelection),
                maximumGroups: 0, maximumPayloadBytes: 0);
            Assert.False(empty.IsError, empty.Message + empty.Exception); Assert.Empty(empty.Result);
        });
}
