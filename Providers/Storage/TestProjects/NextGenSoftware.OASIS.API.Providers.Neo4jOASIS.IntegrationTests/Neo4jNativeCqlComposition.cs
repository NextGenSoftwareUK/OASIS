using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private static CosmicProjectionCompositionPlan<Holon, TResult> CompositionPlan<TResult>(CosmicProjectionQuery<Holon, TResult> query)
    {
        var plan = CosmicProjectionCompositionPlanner.Plan(query, ProviderType.Neo4jOASIS,
            new[] { new CosmicProjectionCompositionTarget(ProviderType.Neo4jOASIS, Provider.CqlCapabilities,
                Provider.CqlProjectionCapabilities, Provider.CqlCompositionCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public Task NativeCompositionPreservesSourceWindowAndSelectedStageOrder(bool filterFirst)
        => WithNativeGraph(async fixture =>
        {
            var source = CosmicQuery<Holon>.Create().OrderBy(h => h.Name).Skip(1).Take(3)
                .Select(h => new { h.Name, h.Version });
            var query = (filterFirst
                ? source.Where(row => row.Name != "B").Take(1)
                : source.Take(1).Where(row => row.Name != "B")).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(
                CompositionPlan(query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var rows = result.Result.Providers.Single().Rows;
            Assert.AreEqual(filterFirst ? 1 : 0, rows.Count);
            if (filterFirst) Assert.AreEqual("C", rows.Single().Name);
        }, configure: root =>
        {
            root.Name = "A";
            var children = root.Children.ToArray();
            children[0].Name = "B"; children[1].Name = "C";
            children[0].Children.Single().Name = "D";
        });

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)]
    [DataRow(3)] [DataRow(4)] [DataRow(5)]
    public Task NativeCompositionEnforcesSixRemainingBudgetsAndFreshReuse(int exhausted)
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
                .Select(h => h.Name).Take(1).Result;
            var plan = CompositionPlan(query);
            var limits = new[] { 10, 10000, 100, 10000, 10, 10000 };
            limits[exhausted] = 0;
            var failed = await fixture.Provider.ExecuteCosmicProjectionCompositionAsync(plan,
                new CosmicProjectionCompositionExecutionLimits(limits[0], limits[1], limits[2], limits[3], limits[4], limits[5]));
            Assert.IsTrue(failed.IsError, failed.Message);
            Assert.AreEqual("CQL_QUERY_LIMIT", failed.ErrorCode);
            Assert.IsNull(failed.Result);
            var fresh = await GraphManager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(plan, new[] { fixture.Provider });
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception);
            Assert.AreEqual(1, fresh.Result.Providers.Single().Rows.Count);
        });

    [TestMethod]
    public Task NativeCompositionSortRetainsPriorRankOnRepeatedKeys()
        => WithNativeGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().OrderByDescending(h => h.Name)
                .Select(h => new { h.Name, h.Version })
                .OrderBy(row => row.Version).ThenByDescending(row => row.Version).Skip(1).Take(2).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(
                CompositionPlan(query), new[] { fixture.Provider });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEqual(new[] { "C", "B" }, result.Result.Providers.Single().Rows.Select(row => row.Name).ToArray());
        }, configure: root =>
        {
            root.Name = "A";
            var children = root.Children.ToArray();
            children[0].Name = "B"; children[1].Name = "C";
            children[0].Children.Single().Name = "D";
        });

    [TestMethod]
    public Task NativeCompositionPreCancellationDoesNotPublishAndAllowsReuse()
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => h.Name).Take(1).Result;
            var plan = CompositionPlan(query);
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            var failed = await fixture.Provider.ExecuteCosmicProjectionCompositionAsync(plan,
                new CosmicProjectionCompositionExecutionLimits(10, 10000, 100, 10000, 10, 10000), cancellation.Token);
            Assert.IsTrue(failed.IsError); Assert.AreEqual("CQL_QUERY_CANCELLED", failed.ErrorCode); Assert.IsNull(failed.Result);
            var fresh = await GraphManager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(plan, new[] { fixture.Provider });
            Assert.IsFalse(fresh.IsError, fresh.Message + fresh.Exception);
        });

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)]
    [DataRow(4)] [DataRow(5)] [DataRow(6)] [DataRow(7)]
    [DataRow(8)] [DataRow(9)] [DataRow(10)] [DataRow(11)]
    public Task NativeCompositionPreservesExactSelectedDomains(int domain) => domain switch
    {
        0 => ExactCompositionCell(9007199254740993L),
        1 => ExactCompositionCell(ulong.MaxValue),
        2 => ExactCompositionCell(123456789.123456789123456789m),
        3 => ExactCompositionCell(1.0000000000000002d),
        4 => ExactCompositionCell(1.0000001f),
        5 => ExactCompositionCell(new DateTime(638900000000000001, DateTimeKind.Utc)),
        6 => ExactCompositionCell(Guid.Parse("a0000000-0000-0000-0000-000000000001")),
        7 => ExactCompositionCell(true),
        8 => ExactCompositionCell("coffee' OR 1=1 --"),
        9 => ExactCompositionCell(int.MaxValue),
        10 => ExactCompositionCell((long?)null),
        _ => ExactCompositionCell(ProviderType.Neo4jOASIS)
    };

    private Task ExactCompositionCell<T>(T value) => WithNativeGraph(async fixture =>
    {
        var id = fixture.Root.Id;
        var accepted = new[] { value };
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
            .Select(h => new { Value = CosmicQueryMetadata.Value<T>(h, "value") })
            .Where(row => accepted.Contains(row.Value)).OrderBy(row => row.Value).Take(1).Result;
        var result = await GraphManager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(
            CompositionPlan(query), new[] { fixture.Provider });
        Assert.IsFalse(result.IsError, result.Message + result.Exception);
        Assert.AreEqual(value, result.Result.Providers.Single().Rows.Single().Value);
    }, width: 0, configure: root => root.MetaData["value"] = value!);

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeCompositionZeroTakeCannotHideRequiredSourceErrors(bool malformed)
        => WithNativeGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
                .Select(h => CosmicQueryMetadata.Value<int>(h, "required")).Take(0).Result;
            var result = await GraphManager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(
                CompositionPlan(query), new[] { fixture.Provider });
            Assert.IsTrue(result.IsError); Assert.AreEqual("CQL_DATA_INVALID", result.ErrorCode); Assert.IsNull(result.Result);
        }, width: 0, configure: root => { if (malformed) root.MetaData["required"] = "wrong-domain"; });

}
