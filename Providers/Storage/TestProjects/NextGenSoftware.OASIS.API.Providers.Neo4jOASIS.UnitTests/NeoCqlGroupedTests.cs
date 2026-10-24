using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlGroupedTests
{
    [TestMethod]
    public void SourceAndOutputWindowsHaveIndependentSentinels()
    {
        var selection = CosmicQuery<Holon>.Create().OrderBy(h => h.Name).Skip(3).Take(137)
            .GroupBy(h => h.Version).Select(g => new { g.Key, Total = g.Sum(h => h.Version) }).Result;
        var command = Neo4jOASIS.CompileCqlGrouped(selection, 1);
        Assert.AreEqual(3L, command.Parameters["groupOffset"]);
        Assert.AreEqual(137L, command.Parameters["groupTake"]);
        Assert.AreEqual(2, command.Parameters["groupLimit"]);
        Assert.IsTrue(command.Query.IndexOf("LIMIT $groupTake", StringComparison.Ordinal)
            < command.Query.IndexOf("count(*) AS inputs", StringComparison.Ordinal));
        Assert.IsTrue(command.Query.Contains(" / 4294967296") && command.Query.Contains(" % 4294967296"));
        Assert.IsFalse(command.Query.Contains("toFloat"));
    }

    [TestMethod]
    public void UnselectedCompositeIdentityRemainsCompleteAndWorkIsBounded()
    {
        var selection = CosmicQuery<Holon>.Create().GroupBy(h => new { h.Name, h.Version })
            .Select(g => new { Rows = g.LongCount(), Min = g.Min(h => h.Version), Mean = g.Average(h => h.Version) }).Result;
        var command = Neo4jOASIS.CompileCqlGrouped(selection, 1000);
        Assert.AreEqual((long)int.MaxValue + 1, command.Parameters["groupTake"]);
        Assert.IsTrue(command.Query.Contains("AS k0") && command.Query.Contains("AS k1"));
        Assert.IsTrue(command.Query.Contains("keys[0].v ASC") && command.Query.Contains("keys[1].v ASC"));
        Assert.IsTrue(command.Query.Contains("CASE WHEN keys[0].v IS NULL THEN 0 ELSE 1 END ASC"));
        Assert.IsTrue(command.Query.Contains("min(") && command.Query.Contains("present:a2n,high:a2h,low:a2l"));
    }

    [TestMethod]
    public void EmptyWindowCannotEliminateProtectedFailureBranches()
    {
        var selection = CosmicQuery<Holon>.Create()
            .WhereAnyDescendant(h => h.Name == "visible", CosmicQueryTraversalOptions.Create(2, 10).Result)
            .Take(0).GroupBy(h => h.Name).Select(g => g.LongCount()).Result;
        var command = Neo4jOASIS.CompileCqlGrouped(selection, 0);
        Assert.AreEqual(0L, command.Parameters["groupTake"]);
        Assert.IsTrue(command.Query.IndexOf("AS traversalLimit LIMIT 1", StringComparison.Ordinal)
            < command.Query.IndexOf("LIMIT $groupTake", StringComparison.Ordinal));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Neo4jOASIS.CompileCqlGrouped(selection, -1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Neo4jOASIS.CompileCqlGrouped(selection, 1001));
    }

    [TestMethod]
    public void MetadataPathTextCannotBecomeExecutableCypher()
    {
        const string hostile = "x'} CALL db.labels() //";
        var selection = CosmicQuery<Holon>.Create()
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, hostile))
            .Select(g => g.LongCount()).Result;
        var command = Neo4jOASIS.CompileCqlGrouped(selection, 1);
        Assert.IsFalse(command.Query.Contains(hostile));
        Assert.IsTrue(command.Parameters.Values.OfType<string>().Any());
    }

    [TestMethod]
    public void NativeResultPageRanksOnlyTheBoundedOrderedProof()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => g.LongCount()).Skip(2).Take(1).Result;
        var command = Neo4jOASIS.CompileCqlGrouped(query, 10);
        Assert.IsTrue(command.Query.IndexOf("LIMIT $groupLimit", StringComparison.Ordinal)
            < command.Query.IndexOf("WITH collect(", StringComparison.Ordinal));
        Assert.IsTrue(command.Query.Contains("AS resultMember"));
        Assert.AreEqual(2, command.Parameters["resultOffset"]);
        Assert.AreEqual(1, command.Parameters["resultTake"]);
        Assert.AreEqual(11, command.Parameters["groupLimit"]);
    }
}
