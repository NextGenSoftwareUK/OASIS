using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlCompositionTests
{
    private static CosmicProjectionCompositionPlan<Holon, TResult> Plan<TResult>(CosmicProjectionQuery<Holon, TResult> query)
    {
        var grouped = Neo4jOASIS.CqlGroupedCapabilities;
        var profile = new CosmicProjectionCompositionCapabilities(
            CosmicSelectedStageFeatures.Filter | CosmicSelectedStageFeatures.Ordering | CosmicSelectedStageFeatures.Paging,
            grouped.FilteringCapabilities.Features, grouped.FilteringCapabilities.Keys, grouped.OrderingCapabilities.KeyTypes);
        var plan = CosmicProjectionCompositionPlanner.Plan(query, ProviderType.Neo4jOASIS,
            new[] { new CosmicProjectionCompositionTarget(ProviderType.Neo4jOASIS,
                Neo4jOASIS.CqlCapabilities, Neo4jOASIS.CqlProjectionCapabilities, profile) });
        Assert.IsFalse(plan.IsError, plan.Message); return plan.Result;
    }
    private static CosmicProjectionCompositionExecutionLimits Limits(int rows = 10) => new(rows, 10000, 100, 10000, 100, 10000);

    [TestMethod]
    public void SourceIsCapturedOnceBeforeSelectedWindows()
    {
        var query = CosmicQuery<Holon>.Create().OrderBy(h => h.Name).Skip(2).Take(3)
            .Select(h => new { h.Name, h.Version }).Take(1).Where(row => row.Version > 1).Result;
        var command = Neo4jOASIS.CompileCqlComposition(Plan(query), Limits());
        StringAssert.Contains(command.Text, "RETURN collect(input) AS input");
        StringAssert.Contains(command.Text, "collect(position) AS s1");
        Assert.IsTrue(command.Text.IndexOf("collect(position) AS s1", StringComparison.Ordinal)
            < command.Text.IndexOf("input[position].values[1].v", StringComparison.Ordinal));
        Assert.IsTrue(command.Parameters.Values.Contains(3));
        StringAssert.Contains(command.Text, "ORDER BY phase ASC,position ASC");
    }

    [DataTestMethod]
    [DataRow(0, 1)] [DataRow(1, 2)]
    public void SourceRetainsOverflowSentinelEvenWhenSelectedTakeIsZero(int remaining, int sentinel)
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(0).Result;
        var command = Neo4jOASIS.CompileCqlComposition(Plan(query), Limits(remaining));
        Assert.IsTrue(command.Parameters.Values.Contains(sentinel));
        StringAssert.Contains(command.Text, "THEN range(0,size(input)-1) ELSE [] END AS s0");
        StringAssert.Contains(command.Text, "-1 AS phase,pos AS position");
    }

    [TestMethod]
    public void SortUsesExplicitNullRanksAndPreviousStageRankAsTie()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => new { h.Name, h.Version })
            .OrderBy(row => row.Name).ThenByDescending(row => row.Version).Result;
        var command = Neo4jOASIS.CompileCqlComposition(Plan(query), Limits());
        StringAssert.Contains(command.Text, "values[0].v IS NULL THEN 0 ELSE 1 END ASC");
        StringAssert.Contains(command.Text, "values[1].v IS NULL THEN 0 ELSE 1 END DESC");
        StringAssert.Contains(command.Text, "values[1].v DESC,rank ASC");
    }

    [TestMethod]
    public void ClientAliasesAndValuesAreNeverPastedIntoCypher()
    {
        var value = "coffee' MATCH (private) RETURN private";
        var query = CosmicQuery<Holon>.Create().Select(h => new { HostileAlias = h.Name })
            .Where(row => row.HostileAlias == value).Result;
        var command = Neo4jOASIS.CompileCqlComposition(Plan(query), Limits());
        Assert.IsFalse(command.Text.Contains("HostileAlias")); Assert.IsFalse(command.Text.Contains(value));
        Assert.IsTrue(command.Parameters.Values.Contains(value));
        StringAssert.Contains(command.Text, "input[position].values[0].v");
    }

    [TestMethod]
    public void EmptyStageHeadersAndCumulativeWorkAreExplicit()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(0).Skip(0).Result;
        var command = Neo4jOASIS.CompileCqlComposition(Plan(query), Limits());
        StringAssert.Contains(command.Text, "work+size(s0) AS work");
        StringAssert.Contains(command.Text, "work+size(s1) AS work");
        StringAssert.Contains(command.Text, "proofBytes+16*size(s2)");
        StringAssert.Contains(command.Text, "0 AS phase,-1 AS position");
        StringAssert.Contains(command.Text, "1 AS phase,-1 AS position");
    }

    [TestMethod]
    public void RemainingBoundsCannotUpgradeAdmittedSourceWork()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(1).Result;
        Assert.ThrowsException<ArgumentException>(() => Neo4jOASIS.CompileCqlComposition(Plan(query), Limits(1001)));
    }
}
