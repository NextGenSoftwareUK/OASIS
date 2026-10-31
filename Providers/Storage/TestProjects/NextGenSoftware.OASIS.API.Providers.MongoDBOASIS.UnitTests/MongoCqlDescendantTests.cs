using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoCqlDescendantTests
{
    [TestMethod]
    public void MaximumDepthRemainsFlatAndEachLookupHasABoundedFrontier()
    {
        var query=CosmicQuery<Holon>.Create().WhereAnyDescendant(child=>child.Name=="$user-value",
            CosmicQueryTraversalOptions.Create(32,10000).Result).OrderBy(root=>root.Name).Skip(1).Take(2);
        Assert.IsFalse(query.IsError,query.Message);
        var command=new MongoCqlTranslator().CompileExecution(query.Result,100);
        Assert.IsTrue(command.RequiresMembership);Assert.IsTrue(command.HasTraversalEnvelope);
        var lookups=command.Pipeline.Where(stage=>stage.Contains("$lookup")).ToArray();
        Assert.AreEqual(32,lookups.Length);
        foreach(var lookup in lookups)
        {
            var body=lookup["$lookup"]["pipeline"].AsBsonArray;
            Assert.IsFalse(body.Any(stage=>stage.AsBsonDocument.Contains("$lookup")));
            Assert.AreEqual(10001,body.Single(stage=>stage.AsBsonDocument.Contains("$limit"))["$limit"].AsInt32);
            StringAssert.Contains(lookup.ToJson(),"\"$literal\" : \"$user-value\"");
        }
        var facet=command.Pipeline.Single(stage=>stage.Contains("$facet"))["$facet"];
        Assert.IsFalse(facet["invalid"].AsBsonArray.Any(stage=>stage.AsBsonDocument.Contains("$skip")));
        Assert.IsTrue(facet["roots"].AsBsonArray.Any(stage=>stage.AsBsonDocument.Contains("$skip")));
        Assert.AreEqual(32,MongoDBOASIS.CqlCapabilities.Traversal.MaximumDepth);
    }

    [TestMethod]
    public void ComposedNativeStageOverflowIsRejectedBeforeDispatch()
    {
        var bounds=CosmicQueryTraversalOptions.Create(32,10000).Result;
        var query=CosmicQuery<Holon>.Create().WhereAnyDescendant(child=>true,bounds);
        for(var index=1;index<11;index++)query=query.WhereAnyDescendant(child=>true,bounds);
        Assert.IsFalse(query.IsError,query.Message);
        Assert.ThrowsException<MongoCqlLimitException>(()=>new MongoCqlTranslator().CompileExecution(query.Result,100));
    }

    [TestMethod]
    public void EveryDescendantScopePrecedesTheBusinessConjunction()
    {
        var bounds=CosmicQueryTraversalOptions.Create(2,3).Result;
        var query=CosmicQuery<Holon>.Create()
            .WhereAnyDescendant(child=>false,bounds)
            .WhereAnyDescendant(child=>true,bounds);
        var command=new MongoCqlTranslator().CompileExecution(query.Result,100);
        var first=Array.FindIndex(command.Pipeline,stage=>stage.Contains("$lookup"));
        var last=Array.FindLastIndex(command.Pipeline,stage=>stage.Contains("$lookup"));
        Assert.IsFalse(command.Pipeline.Skip(first).Take(last-first).Any(stage=>stage.Contains("$match")),
            "An unmatched earlier condition must not suppress validation of a later scope.");
        var conjunction=command.Pipeline.Skip(last+1).Single(stage=>stage.Contains("$match"));
        StringAssert.Contains(conjunction.ToJson(),"$allMatched");
    }

    [TestMethod]
    public void TrustedChildAccessIsAppliedInsideEveryBreadthLevel()
    {
        var actor=Guid.NewGuid();
        var query=CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
            .WhereAnyDescendant(child=>child.Version>0,CosmicQueryTraversalOptions.Create(3,7).Result);
        var command=new MongoCqlTranslator().CompileExecution(query.Result,100);
        foreach(var stage in command.Pipeline.Where(stage=>stage.Contains("$lookup")))
        {
            var match=stage["$lookup"]["pipeline"][0].ToJson();
            StringAssert.Contains(match,"CreatedByAvatarId");StringAssert.Contains(match,"IsPublic");
            StringAssert.Contains(match,actor.ToString("D"));
            StringAssert.Contains(match,"$$parent.members");
        }
    }
}
