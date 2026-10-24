using System.Linq;
using MongoDB.Bson;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlGroupedFilterTests
{
    [TestMethod]
    public void FilterMarksMatchingRanksWithoutDroppingBoundedProof()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.LongCount()).Where(rows => rows > 1).Skip(1).Take(1).Result;
        var stages = new MongoCqlTranslator().CompileGrouped(query, 10).Pipeline;
        var group = System.Array.FindIndex(stages, stage => stage.Contains("$group"));
        Assert.IsFalse(stages.Skip(group + 1).Any(stage => stage.Contains("$match")));
        var window = stages.Single(stage => stage.Contains("$setWindowFields") && stage["$setWindowFields"].AsBsonDocument.Contains("sortBy"));
        Assert.IsTrue(window.ToJson().Contains("cqlGroupFilter"));
        Assert.AreEqual(11L, stages.Last(stage => stage.Contains("$limit"))["$limit"].ToInt64());
        Assert.IsTrue(stages.Last()["$unset"].AsBsonArray.Contains("cqlGroupFilter"));
    }
    [TestMethod]
    public void SumFilterKeepsExactDecimal128AndLiteralData()
    {
        var selected = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")) }).Result;
        var query = selected.Where(row => row.Key == "$hostile.'" && row.Total > 9007199254740992m).Result;
        var stages = new MongoCqlTranslator().CompileGrouped(query, 10).Pipeline;
        var filter = stages.Single(stage => stage.Contains("$set") && stage["$set"].AsBsonDocument.Contains("cqlGroupFilter"));
        Assert.IsTrue(filter.ToJson().Contains("$literal")); Assert.IsTrue(filter.ToJson().Contains("$hostile.'"));
        Assert.IsFalse(filter.ToJson().Contains("$toDouble")); Assert.IsTrue(filter.ToJson().Contains("$numberDecimal"));
    }
    [TestMethod]
    public void ProfileSeparatesEnrolledSumsFromNormalizedAverage()
    {
        var selected = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { Total = g.Sum(h => h.Version), Mean = g.Average(h => h.Version) }).Result;
        Assert.IsFalse(MongoDBOASIS.CqlGroupedCapabilities.Admit(selected.Where(row => row.Total > 1).Result).IsError);
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", MongoDBOASIS.CqlGroupedCapabilities.Admit(selected.Where(row => row.Mean > 1m).Result).ErrorCode);
    }
}
