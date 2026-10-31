using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlPostWindowElementTests
{
    private static CosmicElementPlan<Holon> Plan(CosmicElementQuery<Holon> query)
    {
        var plan = CosmicElementPlanner.Plan(query, ProviderType.Neo4jOASIS,
            new[] { new CosmicQueryTarget(ProviderType.Neo4jOASIS, Neo4jOASIS.CqlCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void TerminalOutcomesAndReferencesAreCapturedBeforeElementWindow(bool tail)
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.Neo4jOASIS).Result)
            .OrderBy(row => row.Name).Skip(1).Take(3).Result;
        var bounds = CosmicQueryTraversalOptions.Create(2, 1).Result;
        var query = tail ? source.Last(row => row.Name == "winner" || CosmicQueryRelations.AnyDescendant(row, child => true, bounds)).Result
            : source.First(row => row.Name == "winner" || CosmicQueryRelations.AnyDescendant(row, child => true, bounds)).Result;
        var command = Neo4jOASIS.CompileCqlPostWindowElement(Plan(query).Rows);
        StringAssert.Contains(command.Text, "SKIP $elementOffset LIMIT $elementTake");
        StringAssert.Contains(command.Text, "collect({h:h,p:p,matched:");
        Assert.IsTrue(command.Text.IndexOf("AS candidates", System.StringComparison.Ordinal)
            < command.Text.IndexOf("[0..$elementLimit]", System.StringComparison.Ordinal));
        StringAssert.Contains(command.Text, tail ? "reverse(matches)" : "UNWIND (matches)");
        // The terminal traversal state must survive the ordering WITH before
        // its matched/cycle/limit expressions are reduced.
        var orderCarry = command.Text.Substring(command.Text.LastIndexOf("WITH h,p,elementSort0", System.StringComparison.Ordinal));
        StringAssert.Contains(orderCarry.Split(" ORDER BY ")[0], ",desc");
        Assert.AreEqual(1, command.Parameters["elementOffset"]);
        Assert.AreEqual(3, command.Parameters["elementTake"]);
    }

    [TestMethod]
    public void OffsetOnlyInputUsesOneWorkSentinelAndNeverPublishesTruncatedSuccess()
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.Neo4jOASIS).Result).Skip(1).Result;
        var command = Neo4jOASIS.CompileCqlPostWindowElement(Plan(source.Single(row => true).Result).Rows);
        Assert.AreEqual(10001, command.Parameters["elementTake"]);
        Assert.AreEqual(10000, command.Parameters["elementMaximumWork"]);
        Assert.AreEqual(2, command.Parameters["elementLimit"]);
        StringAssert.Contains(command.Text, "work>$elementMaximumWork AS traversalLimit");
        StringAssert.Contains(command.Text, "work<=$elementMaximumWork");
    }

    [TestMethod]
    public void PagingPermissionDoesNotImplyTerminalFilteringOrSnapshotConsistency()
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.Neo4jOASIS).Result).Take(1).Result;
        var plan = Plan(source.First(row => true).Result);
        var current = Neo4jOASIS.CqlCapabilities;
        var legacy = new CosmicQueryCapabilities(current.Features & ~CosmicQueryFeatures.PostWindowElementPredicate,
            current.CanonicalFields.ToArray(), current.MetadataTypes.ToArray(), current.Traversal);
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", legacy.AdmitPlan(plan.Rows).ErrorCode);
        Assert.IsFalse(current.Features.HasFlag(CosmicQueryFeatures.GraphSnapshot));
    }
}
