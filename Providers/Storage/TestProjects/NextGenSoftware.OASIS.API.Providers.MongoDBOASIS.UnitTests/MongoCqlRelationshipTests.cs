using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoCqlRelationshipTests
{
    [TestMethod]
    public void IndependentNativeLookupsAreBoundedAndPrecedeRootWindow()
    {
        var query = CosmicQuery<Holon>.Create()
            .WhereAnyChild(child => child.Name == "$literal-user-value")
            .WhereAnyChild(child => child.Version > 3).OrderBy(root => root.Name).Skip(1).Take(2);
        Assert.IsFalse(query.IsError, query.Message);
        var command = new MongoCqlTranslator().CompileExecution(query.Result, 100);
        Assert.IsTrue(command.RequiresMembership);
        var lookups = command.Pipeline.Where(stage => stage.Contains("$lookup")).ToArray();
        Assert.AreEqual(2, lookups.Length);
        foreach (var lookup in lookups)
        {
            var body = lookup["$lookup"].AsBsonDocument;
            Assert.AreEqual("Holon", body["from"].AsString);
            Assert.IsTrue(body["pipeline"].AsBsonArray.Any(stage => stage.AsBsonDocument.Contains("$limit") && stage["$limit"] == 1));
        }
        Assert.IsTrue(Array.FindLastIndex(command.Pipeline, stage => stage.Contains("$lookup")) <
            Array.FindIndex(command.Pipeline, stage => stage.Contains("$skip")));
        StringAssert.Contains(lookups[0].ToJson(), "\"$literal\" : \"$literal-user-value\"");
        Assert.AreEqual("$row", command.Pipeline.First(stage => stage.Contains("$replaceRoot") &&
            stage["$replaceRoot"]["newRoot"].IsString)["$replaceRoot"]["newRoot"].AsString);
    }

    [TestMethod]
    public void ActorPolicyIsCompiledInsideTheLookupAndAdmissionIsActual()
    {
        var actor = Guid.NewGuid();
        var query = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
            .WhereAnyChild(child => child.Name == "match");
        Assert.IsFalse(query.IsError, query.Message);
        var command = new MongoCqlTranslator().CompileExecution(query.Result, 100);
        var lookup = command.Pipeline.Single(stage => stage.Contains("$lookup")).ToJson();
        StringAssert.Contains(lookup, "CreatedByAvatarId");
        StringAssert.Contains(lookup, "IsPublic");
        StringAssert.Contains(lookup, actor.ToString("D"));
    }

    [TestMethod]
    public void OrdinaryLeafQueriesDoNotRequestRelationshipValidation()
    {
        var query = CosmicQuery<Holon>.Create().Where(root => root.Name == "leaf");
        var command = new MongoCqlTranslator().CompileExecution(query.Result, 100);
        Assert.IsFalse(command.RequiresMembership);
        Assert.IsFalse(command.Pipeline.Any(stage => stage.Contains("$lookup")));
    }
}
