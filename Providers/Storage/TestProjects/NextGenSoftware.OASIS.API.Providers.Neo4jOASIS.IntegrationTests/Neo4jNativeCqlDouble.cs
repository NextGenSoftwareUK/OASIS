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
    private static void SetDoubleRows(Holon root, params object?[] values)
    {
        var nodes = new List<IHolon> { root };
        for (var index = 0; index < nodes.Count; index++) nodes.AddRange(nodes[index].AllChildren);
        Assert.AreEqual(values.Length, nodes.Count);
        for (var index = 0; index < nodes.Count; index++) nodes[index].MetaData["counter"] = values[index]!;
    }

    [TestMethod]
    public Task NativeDoubleRangeMembershipAndOrderPreserveAdjacentMaxima()
        => WithNativeGraph(async fixture =>
        {
            var members = new[] { 1.0000000000000002d, 1.0000000000000004d };
            var query = GroupFilterSource().Where(h => CosmicQueryMetadata.Value<double>(h, "counter") > 1d
                && members.Contains(CosmicQueryMetadata.Value<double>(h, "counter")))
                .OrderBy(h => CosmicQueryMetadata.Value<double>(h, "counter"))
                .Select(h => CosmicQueryMetadata.Value<double>(h, "counter")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEqual(members, result.Result.Providers.Single().Rows.ToArray());
        }, configure: root => SetDoubleRows(root, 0d, 1.0000000000000004d, 1.0000000000000002d, null));

    [TestMethod]
    public Task NativeDoubleNullableGroupedKeysAndExtremaShareTheExactDomain()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<double?>(h, "counter"))
                .Select(g => new { Value = g.Key, Items = g.LongCount() })
                .Where(row => row.Value >= 1.0000000000000002d).OrderBy(row => row.Value).Take(2).Result;
            var groups = await RunGroupedFilter(fixture, query);
            CollectionAssert.AreEqual(new double?[] { 1.0000000000000002d, 1.0000000000000004d }, groups.Select(g => g.Value).ToArray());
            Assert.IsTrue(groups.All(g => g.Items == 1));
            var source = GroupFilterSource();
            var min = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<double?>(h, "counter")).Result));
            var max = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Max(h => CosmicQueryMetadata.Value<double?>(h, "counter")).Result));
            Assert.IsFalse(min.IsError, min.Message + min.Exception); Assert.IsFalse(max.IsError, max.Message + max.Exception);
            Assert.AreEqual((double?)0, min.Result); Assert.AreEqual((double?)1.0000000000000004d, max.Result);
        }, configure: root => SetDoubleRows(root, 0d, 1.0000000000000002d, 1.0000000000000004d, null));

    [TestMethod]
    public Task NativeDoubleFieldComparisonRetainsBothOperands()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().Where(h => CosmicQueryMetadata.Value<double>(h, "counter")
                > CosmicQueryMetadata.Value<double>(h, "other"))
                .Select(h => CosmicQueryMetadata.Value<double>(h, "counter")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEqual(new[] { 1.0000000000000004d }, result.Result.Providers.Single().Rows.ToArray());
        }, configure: root =>
        {
            SetDoubleRows(root, 1.0000000000000004d, 0d, 0d, 0d);
            var nodes = new List<IHolon> { root };
            for (var index = 0; index < nodes.Count; index++) nodes.AddRange(nodes[index].AllChildren);
            foreach (var node in nodes) node.MetaData["other"] = 1.0000000000000002d;
        });

    [DataTestMethod]
    [DataRow("underflow")] [DataRow("overflow")] [DataRow("string")]
    [DataRow("boolean")] [DataRow("null")] [DataRow("array")]
    
    public Task NativeDoubleInvalidDomainsCannotCoerceOrThrow(string kind)
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
            var selected = GroupFilterSource().Select(h => CosmicQueryMetadata.Value<double>(h, "counter")).Result;
            var invalid = await fixture.Provider.ExecuteCosmicProjectionAsync(ProjectionPlan(fixture.Provider, selected));
            Assert.IsTrue(invalid.IsError); Assert.AreEqual("CQL_DATA_INVALID", invalid.ErrorCode);
            var matching = GroupFilterSource().Where(h => CosmicQueryMetadata.Value<double>(h, "counter") == 1.0000000000000004d)
                .Select(h => h.Id).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, matching), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(3, result.Result.Providers.Single().Rows.Count);
        }, configure: root => SetDoubleRows(root, value, 1.0000000000000004d, 1.0000000000000004d, 1.0000000000000004d));
    }
    [DataTestMethod]
    [DataRow("-1.7976931348623157E308")] [DataRow("-5E-324")]
    [DataRow("0")] [DataRow("5E-324")] [DataRow("1.7976931348623157E308")]
    [DataRow("9007199254740993")]
    public Task NativeDoubleBoundaryConversionUsesDeclaredBinary64(string text)
    {
        var expected = double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        using var document = System.Text.Json.JsonDocument.Parse(text);
        var token = document.RootElement.Clone();
        return WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource()
                .Where(h => CosmicQueryMetadata.Value<double>(h, "counter") == expected)
                .OrderBy(h => CosmicQueryMetadata.Value<double>(h, "counter"))
                .Select(h => CosmicQueryMetadata.Value<double>(h, "counter")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(
                ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var rows = result.Result.Providers.Single().Rows;
            Assert.AreEqual(4, rows.Count);
            Assert.IsTrue(rows.All(number => number == expected));
        }, configure: root => SetDoubleRows(root, token, token, token, token));
    }

}
