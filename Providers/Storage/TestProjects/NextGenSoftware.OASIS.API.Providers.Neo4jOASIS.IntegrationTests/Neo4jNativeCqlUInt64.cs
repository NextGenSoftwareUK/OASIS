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
    private static void SetUnsignedRows(Holon root, params object?[] values)
    {
        var nodes = new List<IHolon> { root };
        for (var index = 0; index < nodes.Count; index++) nodes.AddRange(nodes[index].AllChildren);
        Assert.AreEqual(values.Length, nodes.Count);
        for (var index = 0; index < nodes.Count; index++) nodes[index].MetaData["counter"] = values[index]!;
    }

    [TestMethod]
    public Task NativeUnsignedRangeMembershipAndOrderPreserveAdjacentMaxima()
        => WithNativeGraph(async fixture =>
        {
            var members = new[] { ulong.MaxValue - 1UL, ulong.MaxValue };
            var query = GroupFilterSource().Where(h => CosmicQueryMetadata.Value<ulong>(h, "counter") > (ulong)long.MaxValue
                && members.Contains(CosmicQueryMetadata.Value<ulong>(h, "counter")))
                .OrderBy(h => CosmicQueryMetadata.Value<ulong>(h, "counter"))
                .Select(h => CosmicQueryMetadata.Value<ulong>(h, "counter")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEqual(members, result.Result.Providers.Single().Rows.ToArray());
        }, configure: root => SetUnsignedRows(root, 0UL, ulong.MaxValue, ulong.MaxValue - 1UL, null));

    [TestMethod]
    public Task NativeUnsignedNullableGroupedKeysAndExtremaShareTheExactDomain()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().GroupBy(h => CosmicQueryMetadata.Value<ulong?>(h, "counter"))
                .Select(g => new { Value = g.Key, Items = g.LongCount() })
                .Where(row => row.Value >= ulong.MaxValue - 1UL).OrderBy(row => row.Value).Take(2).Result;
            var groups = await RunGroupedFilter(fixture, query);
            CollectionAssert.AreEqual(new ulong?[] { ulong.MaxValue - 1UL, ulong.MaxValue }, groups.Select(g => g.Value).ToArray());
            Assert.IsTrue(groups.All(g => g.Items == 1));
            var source = GroupFilterSource();
            var min = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<ulong?>(h, "counter")).Result));
            var max = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Max(h => CosmicQueryMetadata.Value<ulong?>(h, "counter")).Result));
            Assert.IsFalse(min.IsError, min.Message + min.Exception); Assert.IsFalse(max.IsError, max.Message + max.Exception);
            Assert.AreEqual((ulong?)0, min.Result); Assert.AreEqual((ulong?)ulong.MaxValue, max.Result);
        }, configure: root => SetUnsignedRows(root, 0UL, ulong.MaxValue - 1UL, ulong.MaxValue, null));

    [TestMethod]
    public Task NativeUnsignedFieldComparisonRetainsBothOperands()
        => WithNativeGraph(async fixture =>
        {
            var query = GroupFilterSource().Where(h => CosmicQueryMetadata.Value<ulong>(h, "counter")
                > CosmicQueryMetadata.Value<ulong>(h, "other"))
                .Select(h => CosmicQueryMetadata.Value<ulong>(h, "counter")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEqual(new[] { ulong.MaxValue }, result.Result.Providers.Single().Rows.ToArray());
        }, configure: root =>
        {
            SetUnsignedRows(root, ulong.MaxValue, 0UL, 0UL, 0UL);
            var nodes = new List<IHolon> { root };
            for (var index = 0; index < nodes.Count; index++) nodes.AddRange(nodes[index].AllChildren);
            foreach (var node in nodes) node.MetaData["other"] = ulong.MaxValue - 1UL;
        });

    [DataTestMethod]
    [DataRow("negative")] [DataRow("fraction")] [DataRow("overflow")] [DataRow("string")]
    [DataRow("exponent")] [DataRow("boolean")] [DataRow("null")] [DataRow("array")]
    [DataRow("decimal")] [DataRow("largeNegative")]
    public Task NativeUnsignedInvalidDomainsCannotCoerceOrThrow(string kind)
    {
        object? value = kind switch
        {
            "negative" => -1L, "fraction" => 1.5m, "overflow" => 18446744073709551616m,
            "string" => "18446744073709551615", "exponent" => 1e100,
            "boolean" => true, "null" => null, "array" => new[] { 1 }, "decimal" => 1.0m,
            _ => -18446744073709551616m
        };
        return WithNativeGraph(async fixture =>
        {
            var selected = GroupFilterSource().Select(h => CosmicQueryMetadata.Value<ulong>(h, "counter")).Result;
            var invalid = await fixture.Provider.ExecuteCosmicProjectionAsync(ProjectionPlan(fixture.Provider, selected));
            Assert.IsTrue(invalid.IsError); Assert.AreEqual("CQL_DATA_INVALID", invalid.ErrorCode);
            var matching = GroupFilterSource().Where(h => CosmicQueryMetadata.Value<ulong>(h, "counter") == ulong.MaxValue)
                .Select(h => h.Id).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, matching), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(3, result.Result.Providers.Single().Rows.Count);
        }, configure: root => SetUnsignedRows(root, value, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue));
    }
}
