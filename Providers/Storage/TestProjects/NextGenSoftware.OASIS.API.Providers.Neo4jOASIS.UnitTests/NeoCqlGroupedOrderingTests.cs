using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;
[TestClass]
public sealed class NeoCqlGroupedOrderingTests
{
    [TestMethod]
    public void SumOrderingUsesIntegralFloorLimbsAndCompleteIdentityTies()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { g.Key, Total = g.Sum(h => h.Version) }).OrderBy(g => g.Total).Result;
        var command = Neo4jOASIS.CompileCqlGrouped(query, 10);
        Assert.IsTrue(command.Query.Contains("aggregates[0].low % 4294967296"));
        Assert.IsTrue(command.Query.Contains("aggregates[0].high + aggregates[0].low / 4294967296"));
        Assert.IsTrue(command.Query.Contains("keys[0].v ASC"));
        Assert.IsFalse(command.Query.Contains("toFloat"));
    }

    [TestMethod]
    public void ProfileDoesNotBorrowSumPermissionForRoundedAverageOrdering()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { Total = g.Sum(h => h.Version), Mean = g.Average(h => h.Version) }).Result;
        Assert.IsFalse(Neo4jOASIS.CqlGroupedCapabilities.Admit(query.OrderBy(g => g.Total).Result).IsError);
        var capability = Neo4jOASIS.CqlGroupedCapabilities;
        Assert.IsFalse(capability.Admit(query.OrderBy(g => g.Mean).Result).IsError);
        var sumOnly = new CosmicGroupedCapabilities(capability.KeyCapabilities, capability.AggregateCapabilities,
            new CosmicGroupedOrderingCapabilities(capability.OrderingCapabilities.KeyTypes,
                capability.OrderingCapabilities.Aggregates.Where(value => value.Operation != CosmicScalarOperation.Average).ToArray()));
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", sumOnly.Admit(query.OrderBy(g => g.Mean).Result).ErrorCode);
    }
}
