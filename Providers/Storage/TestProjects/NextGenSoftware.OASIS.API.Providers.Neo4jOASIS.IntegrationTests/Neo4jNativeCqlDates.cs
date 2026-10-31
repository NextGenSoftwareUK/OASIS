using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private static readonly DateTime DateAnchor = new(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);
    private static void SetDateRows(Holon root, params object?[] dates)
    {
        var nodes = new List<IHolon> { root };
        for (var index = 0; index < nodes.Count; index++) nodes.AddRange(nodes[index].AllChildren);
        Assert.AreEqual(dates.Length, nodes.Count);
        for (var index = 0; index < nodes.Count; index++) nodes[index].MetaData["stamp"] = dates[index]!;
    }

    [TestMethod]
    public Task NativeDateRangeMembershipOrderAndProjectionRetainAdjacentTicks()
        => WithNativeGraph(async fixture =>
        {
            var start = DateAnchor; var end = start.AddTicks(2);
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => h.Name.StartsWith("owned CQL", StringComparison.Ordinal)).Result;
            var selected = source.Where(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp") >= start
                && CosmicQueryMetadata.Value<DateTime>(h, "stamp") < end)
                .OrderBy(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp"))
                .Select(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, selected), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var rows = result.Result.Providers.Single().Rows;
            CollectionAssert.AreEqual(new[] { start, start.AddTicks(1) }, rows.ToArray());
            Assert.IsTrue(rows.All(value => value.Kind == DateTimeKind.Utc));
            var wanted = new[] { start, start.AddTicks(1) };
            var membership = source.Where(h => wanted.Contains(CosmicQueryMetadata.Value<DateTime>(h, "stamp")))
                .OrderBy(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp"))
                .Select(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp")).Result;
            var members = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, membership), new[] { fixture.Provider });
            Assert.IsFalse(members.IsError, members.Message + members.Exception);
            CollectionAssert.AreEqual(wanted, members.Result.Providers.Single().Rows.ToArray());
        }, configure: root => SetDateRows(root, DateAnchor.AddTicks(2), DateAnchor, DateAnchor.AddTicks(1), "malformed"));

    [TestMethod]
    public Task NativeDateGroupingNormalizesFractionWidthsAndNullableExtrema()
        => WithNativeGraph(async fixture =>
        {
            var start = DateAnchor;
            var query = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<DateTime?>(h, "stamp"))
                .Select(g => new { Stamp = g.Key, Rows = g.LongCount(),
                    Min = g.Min(h => CosmicQueryMetadata.Value<DateTime?>(h, "stamp")),
                    Max = g.Max(h => CosmicQueryMetadata.Value<DateTime?>(h, "stamp")) })
                .Where(row => row.Stamp >= start).OrderBy(row => row.Stamp).Result;
            var rows = await RunGroupedFilter(fixture, query);
            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(start, rows[0].Stamp); Assert.AreEqual(2L, rows[0].Rows);
            Assert.AreEqual(start, rows[0].Min); Assert.AreEqual(start, rows[0].Max);
            Assert.AreEqual(start.AddTicks(1), rows[1].Stamp);
        }, configure: root => SetDateRows(root, DateAnchor, "2026-10-24T00:00:00.0000000Z", DateAnchor.AddTicks(1), null));

    [TestMethod]
    public Task NativeDateExtremaKeepFullCalendarRangeAndLeapDay()
        => WithNativeGraph(async fixture =>
        {
            var source = GroupFilterSource();
            var min = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp")).Result));
            var max = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Max(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp")).Result));
            Assert.IsFalse(min.IsError, min.Message + min.Exception); Assert.IsFalse(max.IsError, max.Message + max.Exception);
            Assert.AreEqual(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), min.Result);
            Assert.AreEqual(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), max.Result);
        }, configure: root => SetDateRows(root, DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
            DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), new DateTime(2000, 2, 29, 12, 0, 0, DateTimeKind.Utc), DateAnchor));

    [TestMethod]
    public Task NativeDateFieldComparisonsNormalizeBothOperands()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().Where(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp")
                    == CosmicQueryMetadata.Value<DateTime>(h, "otherStamp"))
                .OrderBy(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp"))
                .Select(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEqual(new[] { DateAnchor, DateAnchor.AddTicks(2) }, result.Result.Providers.Single().Rows.ToArray());
        }, configure: root =>
        {
            SetDateRows(root, DateAnchor, DateAnchor.AddTicks(1), DateAnchor.AddTicks(2), DateAnchor.AddTicks(3));
            root.MetaData["otherStamp"] = "2026-10-24T00:00:00Z";
            root.Children.First().MetaData["otherStamp"] = DateAnchor;
            root.Children.ElementAt(1).MetaData["otherStamp"] = DateAnchor.AddTicks(2);
            root.Children.First().Children.First().MetaData["otherStamp"] = DateAnchor;
        });

    [TestMethod]
    public Task NativeDateNullableProjectionRetainsMissingAndNullLeaves()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().Select(h => CosmicQueryMetadata.Value<DateTime?>(h, "stamp")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(2, result.Result.Providers.Single().Rows.Count(value => value == null));
        }, configure: root =>
        {
            SetDateRows(root, null, null, DateAnchor, DateAnchor.AddTicks(1));
            root.Children.First().MetaData.Remove("stamp");
        });

    [DataTestMethod]
    [DataRow(42)] [DataRow(true)] [DataRow(null)] [DataRow("")]
    [DataRow("2026-02-29T00:00:00Z")] [DataRow("1900-02-29T00:00:00Z")]
    [DataRow("2026-04-31T00:00:00Z")] [DataRow("0000-01-01T00:00:00Z")]
    [DataRow("2026-10-24T24:00:00Z")] [DataRow("2026-10-24T00:00:60Z")]
    [DataRow("2026-10-24T00:00:00+00:00")] [DataRow("2026-10-24T00:00:00z")]
    [DataRow("2026-10-24T00:00:00.12345678Z")] [DataRow("2026-10-24T00:00:00Z\n")]
    public Task NativeDateMalformedValuesDoNotThrowOrBecomeSelectedDates(object? malformed)
        => WithNativeGraph(async fixture =>
        {
            var start = DateAnchor;
            var selected = GroupFilterSource().Select(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp")).Result;
            var invalid = await fixture.Provider.ExecuteCosmicProjectionAsync(ProjectionPlan(fixture.Provider, selected));
            Assert.IsTrue(invalid.IsError); Assert.AreEqual("CQL_DATA_INVALID", invalid.ErrorCode);
            var filtered = GroupFilterSource().Where(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp") == start)
                .Select(h => h.Id).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, filtered), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(3, result.Result.Providers.Single().Rows.Count);
        }, configure: root => SetDateRows(root, malformed, DateAnchor, DateAnchor, DateAnchor));

    [TestMethod]
    public Task NativeDateGroupResultTakeZeroCannotHideInvalidCalendarData()
        => WithNativeGraph(async fixture =>
        {
            var start = DateAnchor;
            var query = GroupFilterSource().GroupBy(h => h.Name)
                .Select(g => g.Min(h => CosmicQueryMetadata.Value<DateTime>(h, "stamp")))
                .Where(value => value < start).Take(0).Result;
            var result = await fixture.Provider.ExecuteCosmicGroupedAsync(GroupedPlan(query));
            Assert.IsTrue(result.IsError); Assert.AreEqual("CQL_DATA_INVALID", result.ErrorCode);
        }, configure: root => SetDateRows(root, "2026-02-29T00:00:00Z", DateAnchor, DateAnchor, DateAnchor));
}
