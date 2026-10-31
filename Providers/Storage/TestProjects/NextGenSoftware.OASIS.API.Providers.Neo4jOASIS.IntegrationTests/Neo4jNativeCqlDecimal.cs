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
    private static void SetDecimalRows(Holon root, params object?[] values)
    {
        var nodes = new List<IHolon> { root };
        for (var index = 0; index < nodes.Count; index++) nodes.AddRange(nodes[index].AllChildren);
        Assert.AreEqual(values.Length, nodes.Count);
        for (var index = 0; index < nodes.Count; index++) nodes[index].MetaData["counter"] = values[index]!;
    }

    [TestMethod]
    public Task NativeDecimalRangeMembershipAndOrderPreserveAdjacentMaxima()
        => WithNativeGraph(async fixture =>
        {
            var members = new[] { 1.0000000000000000000000000001m, 1.0000000000000000000000000002m };
            var query = GroupFilterSource().Where(h => CosmicQueryMetadata.Value<decimal>(h, "counter") > 1m
                && members.Contains(CosmicQueryMetadata.Value<decimal>(h, "counter")))
                .OrderBy(h => CosmicQueryMetadata.Value<decimal>(h, "counter"))
                .Select(h => CosmicQueryMetadata.Value<decimal>(h, "counter")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEqual(members, result.Result.Providers.Single().Rows.ToArray());
        }, configure: root => SetDecimalRows(root, 0m, 1.0000000000000000000000000002m, 1.0000000000000000000000000001m, null));

    [TestMethod]
    public Task NativeDecimalNullableGroupedKeysAndExtremaShareTheExactDomain()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<decimal?>(h, "counter"))
                .Select(g => new { Value = g.Key, Items = g.LongCount() })
                .Where(row => row.Value >= 1.0000000000000000000000000001m).OrderBy(row => row.Value).Take(2).Result;
            var groups = await RunGroupedFilter(fixture, query);
            CollectionAssert.AreEqual(new decimal?[] { 1.0000000000000000000000000001m, 1.0000000000000000000000000002m }, groups.Select(g => g.Value).ToArray());
            Assert.IsTrue(groups.All(g => g.Items == 1));
            var source = GroupFilterSource();
            var min = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<decimal?>(h, "counter")).Result));
            var max = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Max(h => CosmicQueryMetadata.Value<decimal?>(h, "counter")).Result));
            Assert.IsFalse(min.IsError, min.Message + min.Exception); Assert.IsFalse(max.IsError, max.Message + max.Exception);
            Assert.AreEqual((decimal?)0, min.Result); Assert.AreEqual((decimal?)1.0000000000000000000000000002m, max.Result);
        }, configure: root => SetDecimalRows(root, 0m, 1.0000000000000000000000000001m, 1.0000000000000000000000000002m, null));

    [TestMethod]
    public Task NativeDecimalFieldComparisonRetainsBothOperands()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().Where(h => CosmicQueryMetadata.Value<decimal>(h, "counter")
                > CosmicQueryMetadata.Value<decimal>(h, "other"))
                .Select(h => CosmicQueryMetadata.Value<decimal>(h, "counter")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEqual(new[] { 1.0000000000000000000000000002m }, result.Result.Providers.Single().Rows.ToArray());
        }, configure: root =>
        {
            SetDecimalRows(root, 1.0000000000000000000000000002m, 0m, 0m, 0m);
            var nodes = new List<IHolon> { root };
            for (var index = 0; index < nodes.Count; index++) nodes.AddRange(nodes[index].AllChildren);
            foreach (var node in nodes) node.MetaData["other"] = 1.0000000000000000000000000001m;
        });

    [DataTestMethod]
    [DataRow("precision")] [DataRow("underflow")] [DataRow("overflow")] [DataRow("string")]
    [DataRow("exponent")] [DataRow("boolean")] [DataRow("null")] [DataRow("array")]
    
    public Task NativeDecimalInvalidDomainsCannotCoerceOrThrow(string kind)
    {
        object? value = kind switch
        {
            "precision" => System.Text.Json.JsonDocument.Parse("1.00000000000000000000000000001").RootElement.Clone(),
            "underflow" => System.Text.Json.JsonDocument.Parse("1e-1000").RootElement.Clone(),
            "overflow" => System.Text.Json.JsonDocument.Parse("79228162514264337593543950336").RootElement.Clone(),
            "string" => "18446744073709551615", "exponent" => 1e100,
            "boolean" => true, "null" => null, "array" => new[] { 1 },
            _ => -18446744073709551616m
        };
        return WithNativeGraph(async fixture =>
        {
            var selected = GroupFilterSource().Select(h => CosmicQueryMetadata.Value<decimal>(h, "counter")).Result;
            var invalid = await fixture.Provider.ExecuteCosmicProjectionAsync(ProjectionPlan(fixture.Provider, selected));
            Assert.IsTrue(invalid.IsError); Assert.AreEqual("CQL_DATA_INVALID", invalid.ErrorCode);
            var matching = GroupFilterSource().Where(h => CosmicQueryMetadata.Value<decimal>(h, "counter") == 1.0000000000000000000000000002m)
                .Select(h => h.Id).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, matching), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(3, result.Result.Providers.Single().Rows.Count);
        }, configure: root => SetDecimalRows(root, value, 1.0000000000000000000000000002m, 1.0000000000000000000000000002m, 1.0000000000000000000000000002m));
    }
}
