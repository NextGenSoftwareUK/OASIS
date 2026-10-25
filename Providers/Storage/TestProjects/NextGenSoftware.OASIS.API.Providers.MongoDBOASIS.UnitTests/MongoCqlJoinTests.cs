using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlJoinTests
{
    private static CosmicJoinExecutionLimits Limits(int left=10,int right=10,long pairs=100,int output=100)
        =>new(left,right,pairs,output,10000,10000,10000);

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void MatchingOccursAfterBothSourceWindowsAndPreservesJoinKind(bool outer)
    {
        var left=CosmicQuery<Holon>.Create().Where(h=>h.Description=="L").OrderBy(h=>h.Name).Skip(2).Take(3);
        var right=CosmicQuery<Holon>.Create().Where(h=>h.Description=="R").OrderByDescending(h=>h.Name).Skip(4).Take(5);
        var selected=(outer?left.LeftJoin(right,h=>h.Name,h=>h.Name):left.Join(right,h=>h.Name,h=>h.Name))
            .Select((l,r)=>new {l.Name,Present=r!=null}).Result;
        var command=new MongoCqlTranslator().CompileJoin(selected,Limits());
        Assert.IsTrue(command.Left.RequiresSnapshot);Assert.IsTrue(command.Right.RequiresSnapshot);
        Assert.AreEqual(3,command.Left.Pipeline.Single(stage=>stage.Contains("$limit"))["$limit"].ToInt32());
        Assert.AreEqual(5,command.Right.Pipeline.Single(stage=>stage.Contains("$limit"))["$limit"].ToInt32());
        var lookup=command.Pairs.Single(stage=>stage.Contains("$lookup"))["$lookup"].AsBsonDocument;
        var pipeline=lookup["pipeline"].AsBsonArray.Select(value=>value.AsBsonDocument).ToArray();
        var firstLimit=Array.FindIndex(pipeline,stage=>stage.Contains("$limit"));
        var joinFilter=Array.FindLastIndex(pipeline,stage=>stage.Contains("$match"));
        Assert.IsTrue(firstLimit<joinFilter,"The selected right window must precede key matching.");
        Assert.AreEqual(outer,command.Pairs.Single(stage=>stage.Contains("$unwind"))["$unwind"]["preserveNullAndEmptyArrays"].AsBoolean);
        Assert.AreEqual("Holon",lookup["from"].AsString);
        var finalOrder=command.Pairs.Last()["$sort"].AsBsonDocument;
        Assert.AreEqual(1,finalOrder["left"].ToInt32());
        Assert.AreEqual(-1,finalOrder["r0.rank"].ToInt32());
    }

    [DataTestMethod]
    [DataRow(0,0,1)] [DataRow(1,1,2)]
    public void RemainingInputAndPairBudgetsRetainDetectionRows(int rows,int pairs,int sentinel)
    {
        var source=CosmicQuery<Holon>.Create();
        var selected=source.Join(source,h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;
        var command=new MongoCqlTranslator().CompileJoin(selected,Limits(rows,rows,pairs));
        Assert.AreEqual(sentinel,command.Left.Pipeline.Single(stage=>stage.Contains("$limit"))["$limit"].ToInt32());
        Assert.AreEqual(sentinel,command.Right.Pipeline.Single(stage=>stage.Contains("$limit"))["$limit"].ToInt32());
        Assert.AreEqual((long)sentinel,command.Pairs.Last(stage=>stage.Contains("$limit"))["$limit"].ToInt64());
        Assert.IsTrue(command.Pairs.Any(stage=>stage.Contains("$setWindowFields")));
    }

    [TestMethod]
    public void PairPipelineOwnsCopiesOfProofStages()
    {
        var source=CosmicQuery<Holon>.Create();
        var selected=source.Join(source,h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;
        var command=new MongoCqlTranslator().CompileJoin(selected,Limits());
        var original=command.Left.Pipeline[0].ToJson();
        command.Pairs[0].Clear();
        Assert.AreEqual(original,command.Left.Pipeline[0].ToJson());
        var lookup=command.Pairs.Single(stage=>stage.Contains("$lookup"))["$lookup"]["pipeline"].AsBsonArray;
        var right=command.Right.Pipeline[0].ToJson();
        lookup[0].AsBsonDocument.Clear();
        Assert.AreEqual(right,command.Right.Pipeline[0].ToJson());
    }

    [TestMethod]
    public void CompositeKeysAreNativeAndHostileMetadataPathsRemainLiteral()
    {
        var source=CosmicQuery<Holon>.Create();
        var selected=source.Join(source,
            h=>new {h.Name,Region=CosmicQueryMetadata.Value<long>(h,"$region.dot")},
            h=>new {h.Name,Region=CosmicQueryMetadata.Value<long>(h,"$region.dot")})
            .Select((l,r)=>l.Name).Result;
        var command=new MongoCqlTranslator().CompileJoin(selected,Limits());
        var lookup=command.Pairs.Single(stage=>stage.Contains("$lookup"))["$lookup"].AsBsonDocument;
        Assert.AreEqual(2,lookup["let"].AsBsonDocument.ElementCount);
        var json=new BsonArray(command.Right.Pipeline).ToJson();
        StringAssert.Contains(json,"\"$literal\" : \"$region.dot\"");
        Assert.IsFalse(json.Contains("\"$MetaData.$region.dot\""));
    }

    [TestMethod]
    public void DescendantErrorProofsRemainOutsideMatchingAndSourcePages()
    {
        var left=CosmicQuery<Holon>.Create().WhereAnyDescendant(h=>h.Name=="child",
            CosmicQueryTraversalOptions.Create(2,3).Result).Skip(1).Take(2).Result;
        var selected=left.Join(CosmicQuery<Holon>.Create(),h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;
        var command=new MongoCqlTranslator().CompileJoin(selected,Limits());
        Assert.IsTrue(command.Left.HasTraversalEnvelope);Assert.IsTrue(command.Left.RequiresMembership);
        var facet=command.Left.Pipeline.Single(stage=>stage.Contains("$facet"))["$facet"].AsBsonDocument;
        Assert.IsFalse(facet["invalid"].AsBsonArray.Any(value=>value.AsBsonDocument.Contains("$skip")));
        Assert.IsTrue(command.Pairs.Any(stage=>stage.Contains("$match")&&stage["$match"].AsBsonDocument.Contains("error")));
    }

    [TestMethod]
    public void InvalidRemainingBoundsCannotCompileAProviderCommand()
    {
        var source=CosmicQuery<Holon>.Create();
        var selected=source.Join(source,h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;
        Assert.ThrowsException<ArgumentException>(()=>new MongoCqlTranslator().CompileJoin(selected,Limits(left:1001)));
        Assert.ThrowsException<ArgumentException>(()=>new MongoCqlTranslator().CompileJoin(selected,null!));
    }
}
