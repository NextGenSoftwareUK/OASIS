using System;
using System.Linq;
using MongoDB.Bson;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlGroupedTests
{
    [TestMethod]
    public void SourceWindowPrecedesReductionAndGroupCapDoesNotTruncateInputs()
    {
        var selection = CosmicQuery<Holon>.Create().OrderBy(h => h.Name).Skip(2).Take(137)
            .GroupBy(h => h.Name)
            .Select(g => new { g.Key, Count = g.LongCount(), Total = g.Sum(h => h.Version) }).Result;
        var command = new MongoCqlTranslator().CompileGrouped(selection, 1);
        var stages = command.Pipeline;
        var group = Array.FindIndex(stages, s => s.Contains("$group"));
        Assert.IsTrue(group > Array.FindIndex(stages, s => s.Contains("$skip")));
        var limits = stages.Where(s => s.Contains("$limit")).Select(s => s["$limit"].ToInt64()).ToArray();
        CollectionAssert.AreEqual(new long[] { 137, (long)int.MaxValue + 1, 2 }, limits);
        Assert.IsTrue(Array.FindIndex(stages, s => s.Contains("$setWindowFields")) > group);
        Assert.IsTrue(Array.FindIndex(stages, s => s.Contains("$setWindowFields")) < stages.Length - 1);
    }

    [TestMethod]
    public void CompositeIdentityIncludesKeysOmittedFromTheSelectedDto()
    {
        var selection = CosmicQuery<Holon>.Create()
            .GroupBy(h => new { h.Name, h.Version })
            .Select(g => new { Count = g.Count(), Min = g.Min(h => h.Version), Mean = g.Average(h => h.Version) }).Result;
        var command = new MongoCqlTranslator().CompileGrouped(selection, 1000);
        var group = command.Pipeline.Single(s => s.Contains("$group"))["$group"].AsBsonDocument;
        Assert.AreEqual(2, group["_id"].AsBsonDocument.ElementCount);
        Assert.IsTrue(group.Contains("k0") && group.Contains("k1"));
        Assert.IsTrue(group.Contains("invalid") && group.Contains("inputs"));
        Assert.IsTrue(group.Contains("a2high") && group.Contains("a2low") && group.Contains("a2present"));
    }

    [TestMethod]
    public void EmptySourceIntentAndInvalidRemainingBoundsRemainExplicit()
    {
        var selection = CosmicQuery<Holon>.Create().Take(0).GroupBy(h => h.Name)
            .Select(g => g.LongCount()).Result;
        var translator = new MongoCqlTranslator();
        Assert.IsTrue(translator.CompileGrouped(selection, 0).IsEmpty);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => translator.CompileGrouped(selection, -1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => translator.CompileGrouped(selection, 1001));
    }
    [TestMethod]
    public void EmptySelectedWindowRetainsProtectedTraversalOutcomeBranch()
    {
        var selection = CosmicQuery<Holon>.Create()
            .WhereAnyDescendant(h => h.Name == "visible", CosmicQueryTraversalOptions.Create(2, 10).Result)
            .Take(0).GroupBy(h => h.Name).Select(g => g.LongCount()).Result;
        var command = new MongoCqlTranslator().CompileGrouped(selection, 1);
        Assert.IsTrue(command.IsEmpty);
        Assert.IsTrue(command.RequiresMembership && command.RequiresSnapshot && command.HasTraversalEnvelope);
        var json = command.Pipeline.ToJson();
        Assert.IsTrue(json.Contains("$facet") && json.Contains("$invalid"));
        Assert.IsTrue(json.Contains("$expr") && json.Contains("false"));
    }

    [TestMethod]
    public void NativePageMarksRanksWithoutTruncatingTheCompleteGroupProof()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { g.Key, Rows = g.LongCount() }).OrderByDescending(g => g.Rows).Skip(2).Take(1).Result;
        var stages = new MongoCqlTranslator().CompileGrouped(query, 10).Pipeline;
        var rank = stages.Single(stage => stage.Contains("$setWindowFields") && stage["$setWindowFields"].AsBsonDocument.Contains("sortBy"));
        Assert.IsTrue(rank.ToJson().Contains("cqlGroupPosition"));
        Assert.IsTrue(stages.Any(stage => stage.Contains("$set") && stage["$set"].AsBsonDocument.Contains("resultMember")));
        Assert.AreEqual(11L, stages.Last(stage => stage.Contains("$limit"))["$limit"].ToInt64());
        Assert.IsFalse(stages.Any(stage => stage.Contains("$skip")));
    }
}
