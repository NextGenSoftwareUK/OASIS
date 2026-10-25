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
    private static void SetSingleRows(Holon root, params object?[] values)
    {
        var nodes = new List<IHolon> { root };
        for (var index = 0; index < nodes.Count; index++) nodes.AddRange(nodes[index].AllChildren);
        Assert.AreEqual(values.Length, nodes.Count);
        for (var index = 0; index < nodes.Count; index++) nodes[index].MetaData["counter"] = values[index]!;
    }

    [TestMethod]
    public Task NativeSingleRangeMembershipAndOrderPreserveAdjacentMaxima()
        => WithNativeGraph(async fixture =>
        {
            var members = new[] { 1.0000001f, 1.0000002f };
            var query = GroupFilterSource().Where(h => CosmicQueryMetadata.Value<float>(h, "counter") > 1f
                && members.Contains(CosmicQueryMetadata.Value<float>(h, "counter")))
                .OrderBy(h => CosmicQueryMetadata.Value<float>(h, "counter"))
                .Select(h => CosmicQueryMetadata.Value<float>(h, "counter")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEqual(members, result.Result.Providers.Single().Rows.ToArray());
        }, configure: root => SetSingleRows(root, 0f, 1.0000002f, 1.0000001f, null));

    [TestMethod]
    public Task NativeSingleNullableGroupedKeysAndExtremaShareTheExactDomain()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<float?>(h, "counter"))
                .Select(g => new { Value = g.Key, Items = g.LongCount() })
                .Where(row => row.Value >= 1.0000001f).OrderBy(row => row.Value).Take(2).Result;
            var groups = await RunGroupedFilter(fixture, query);
            CollectionAssert.AreEqual(new float?[] { 1.0000001f, 1.0000002f }, groups.Select(g => g.Value).ToArray());
            Assert.IsTrue(groups.All(g => g.Items == 1));
            var source = GroupFilterSource();
            var min = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<float?>(h, "counter")).Result));
            var max = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Max(h => CosmicQueryMetadata.Value<float?>(h, "counter")).Result));
            Assert.IsFalse(min.IsError, min.Message + min.Exception); Assert.IsFalse(max.IsError, max.Message + max.Exception);
            Assert.AreEqual((float?)0, min.Result); Assert.AreEqual((float?)1.0000002f, max.Result);
        }, configure: root => SetSingleRows(root, 0f, 1.0000001f, 1.0000002f, null));

    [TestMethod]
    public Task NativeSingleFieldComparisonRetainsBothOperands()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().Where(h => CosmicQueryMetadata.Value<float>(h, "counter")
                > CosmicQueryMetadata.Value<float>(h, "other"))
                .Select(h => CosmicQueryMetadata.Value<float>(h, "counter")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEqual(new[] { 1.0000002f }, result.Result.Providers.Single().Rows.ToArray());
        }, configure: root =>
        {
            SetSingleRows(root, 1.0000002f, 0f, 0f, 0f);
            var nodes = new List<IHolon> { root };
            for (var index = 0; index < nodes.Count; index++) nodes.AddRange(nodes[index].AllChildren);
            foreach (var node in nodes) node.MetaData["other"] = 1.0000001f;
        });

    [DataTestMethod]
    [DataRow("underflow")] [DataRow("overflow")] [DataRow("string")]
    [DataRow("boolean")] [DataRow("null")] [DataRow("array")]

    public Task NativeSingleInvalidDomainsCannotCoerceOrThrow(string kind)
    {
        object? value = kind switch
        {
            "underflow" => System.Text.Json.JsonDocument.Parse("1e-1000").RootElement.Clone(),
            "overflow" => System.Text.Json.JsonDocument.Parse("1e1000").RootElement.Clone(),
            "string" => "18446744073709551615",
            "boolean" => true, "null" => null, "array" => new[] { 1 },
            _ => -18446744073709551616d
        };
        return WithNativeGraph(async fixture =>
        {
            var selected = GroupFilterSource().Select(h => CosmicQueryMetadata.Value<float>(h, "counter")).Result;
            var invalid = await fixture.Provider.ExecuteCosmicProjectionAsync(ProjectionPlan(fixture.Provider, selected));
            Assert.IsTrue(invalid.IsError); Assert.AreEqual("CQL_DATA_INVALID", invalid.ErrorCode);
            var matching = GroupFilterSource().Where(h => CosmicQueryMetadata.Value<float>(h, "counter") == 1.0000002f)
                .Select(h => h.Id).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, matching), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(3, result.Result.Providers.Single().Rows.Count);
        }, configure: root => SetSingleRows(root, value, 1.0000002f, 1.0000002f, 1.0000002f));
    }
    [DataTestMethod]
    [DataRow("-3.4028235E38")] [DataRow("-1E-45")]
    [DataRow("0")] [DataRow("1E-45")] [DataRow("3.4028235E38")]
    [DataRow("9007199254740993")]
    public Task NativeSingleBoundaryConversionUsesDeclaredBinary32(string text)
    {
        var expected = float.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        using var document = System.Text.Json.JsonDocument.Parse(text);
        var token = document.RootElement.Clone();
        return WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource()
                .Where(h => CosmicQueryMetadata.Value<float>(h, "counter") == expected)
                .OrderBy(h => CosmicQueryMetadata.Value<float>(h, "counter"))
                .Select(h => CosmicQueryMetadata.Value<float>(h, "counter")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(
                ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var rows = result.Result.Providers.Single().Rows;
            Assert.AreEqual(4, rows.Count);
            Assert.IsTrue(rows.All(number => number == expected));
        }, configure: root => SetSingleRows(root, token, token, token, token));
    }

    [TestMethod]
    public Task NativeSinglePromotedDoubleLiteralsKeepTheirPrecision()
        => WithNativeGraph(async fixture =>
        {
            var members = new[] { 0.1d };
            var source = GroupFilterSource();

            // A Single value widened to Double differs from the Double literal
            // 0.1. Neither membership nor grouped filters may narrow the literal.
            var selected = source.Where(h => members.Contains(
                    (double)CosmicQueryMetadata.Value<float>(h, "counter")))
                .Select(h => h.Id);
            Assert.IsFalse(selected.IsError, selected.Message);
            var rows = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(
                ProjectionPlan(fixture.Provider, selected.Result), new[] { fixture.Provider });
            Assert.IsFalse(rows.IsError, rows.Message + rows.Exception);
            Assert.AreEqual(0, rows.Result.Providers.Single().Rows.Count);

            var grouped = source.GroupBy(h => CosmicQueryMetadata.Value<float>(h, "counter"))
                .Select(g => new { Value = g.Key, Items = g.LongCount() })
                .Where(g => (double)g.Value == 0.1d);
            Assert.IsFalse(grouped.IsError, grouped.Message);
            var groups = await RunGroupedFilter(fixture, grouped.Result);
            Assert.AreEqual(0, groups.Count);
        }, configure: root => SetSingleRows(root, 0.1f, 0f, 1f, 2f));

}
