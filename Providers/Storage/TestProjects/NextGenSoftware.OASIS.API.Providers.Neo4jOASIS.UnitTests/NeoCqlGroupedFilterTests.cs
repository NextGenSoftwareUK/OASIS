using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlGroupedFilterTests
{
    [TestMethod]
    public void MatchingRanksUseBoundedCompleteProofAndIntegralSums()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { Rows = g.LongCount(), Total = g.Sum(h => h.Version) })
            .Where(row => row.Total > row.Rows).Skip(1).Take(1).Result;
        var command = Neo4jOASIS.CompileCqlGrouped(query, 10);
        Assert.IsTrue(command.Query.Contains("AS filteredRank"));
        Assert.IsTrue(command.Query.Contains("packet.filterMember AND filteredRank"));
        Assert.IsTrue(command.Query.Contains("aggregates[0].low % 4294967296"));
        Assert.IsFalse(command.Query.Contains("toFloat"));
        Assert.IsTrue(command.Query.IndexOf("LIMIT $groupLimit", System.StringComparison.Ordinal)
            < command.Query.IndexOf("WITH collect(", System.StringComparison.Ordinal));
    }
    [TestMethod]
    public void MixedDecimalBecomesExactIntegerBoundaryBeforeDriverBinding()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => g.Sum(h => h.Version)).Where(total => total >= 1.5m).Result;
        var command = Neo4jOASIS.CompileCqlGrouped(query, 10);
        Assert.IsFalse(command.Parameters.Values.Any(value => value is decimal || value is double));
        Assert.IsTrue(command.Parameters.Values.Any(value => value is long number && number == 2));
    }
    [TestMethod]
    public void ProfileKeepsNormalizedAverageAndUnenrolledDateFilteringExplicit()
    {
        var average = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Average(h => h.Version)).Where(mean => mean > 1m).Result;
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", Neo4jOASIS.CqlGroupedCapabilities.Admit(average).ErrorCode);
        var utc = new System.DateTime(2026, 10, 24, 0, 0, 0, System.DateTimeKind.Utc);
        var date = CosmicQuery<Holon>.Create().GroupBy(h => CosmicQueryMetadata.Value<System.DateTime>(h, "date")).Select(g => g.Key).Where(value => value == utc).Result;
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", Neo4jOASIS.CqlGroupedCapabilities.Admit(date).ErrorCode);
    }
}
