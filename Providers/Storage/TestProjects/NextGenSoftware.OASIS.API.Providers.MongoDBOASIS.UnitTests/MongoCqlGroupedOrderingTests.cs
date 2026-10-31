using System;
using System.Linq;
using MongoDB.Bson;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlGroupedOrderingTests
{
    [TestMethod]
    public void SortUsesExactNumericReductionAndRemovesPrivateSortFields()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) })
            .OrderByDescending(row => row.Total).Result;
        var command = new MongoCqlTranslator().CompileGrouped(query, 10);
        var stages = command.Pipeline;
        var sort = stages.Single(stage => stage.Contains("$sort") && stage["$sort"].AsBsonDocument.Contains("cqlGroupSort0"))["$sort"].AsBsonDocument;
        CollectionAssert.AreEqual(new[] { "cqlGroupSort0", "_id.k0" }, sort.Names.ToArray());
        Assert.AreEqual(-1, sort["cqlGroupSort0"].AsInt32);
        Assert.IsTrue(stages.Last().Contains("$unset"));
        Assert.IsTrue(stages.Any(stage => stage.Contains("$setWindowFields")));
        Assert.IsFalse(new BsonArray(stages).ToJson().Contains("$toDouble"));
    }

    [TestMethod]
    public void ActualProfileRequiresIndependentAverageOrderingPermission()
    {
        var source = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { Total = g.Sum(h => h.Version), Mean = g.Average(h => h.Version) }).Result;
        Assert.IsFalse(MongoDBOASIS.CqlGroupedCapabilities.Admit(source.OrderBy(g => g.Total).Result).IsError);
        var capability = MongoDBOASIS.CqlGroupedCapabilities;
        Assert.IsFalse(capability.Admit(source.OrderBy(g => g.Mean).Result).IsError);
        var sumOnly = new CosmicGroupedCapabilities(capability.KeyCapabilities, capability.AggregateCapabilities,
            new CosmicGroupedOrderingCapabilities(capability.OrderingCapabilities.KeyTypes,
                capability.OrderingCapabilities.Aggregates.Where(value => value.Operation != CosmicScalarOperation.Average).ToArray()));
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", sumOnly.Admit(source.OrderBy(g => g.Mean).Result).ErrorCode);
    }
}
