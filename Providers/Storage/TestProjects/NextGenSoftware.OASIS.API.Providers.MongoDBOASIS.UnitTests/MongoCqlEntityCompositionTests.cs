using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed partial class MongoCqlEntityCompositionTests
{
    private static CosmicEntityCompositionPlan<Holon> Plan(CosmicEntityCompositionQuery<Holon> query)
    {
        var provider = new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1", "entity-stage-no-io");
        var planned = CosmicEntityCompositionPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new CosmicQueryTarget(ProviderType.MongoDBOASIS, MongoDBOASIS.CqlCapabilities), provider.EntityCompositionCapabilities);
        Assert.IsFalse(planned.IsError, planned.Message); return planned.Result;
    }
    private static CosmicQuery<Holon> Source() => CosmicQuery<Holon>
        .Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result).OrderBy(row => row.Name).Result.Take(3).Result;

    [TestMethod]
    public void SourceAndEachStageRemainIndependentNativeProofCommands()
    {
        var query = Source().Compose().Result.Where(row => row.Name == "coffee").Result
            .OrderByDescending(row => row.Version).Result.ThenBy(row => row.Name).Result.Take(1).Result;
        var command = new MongoCqlTranslator().CompileEntityComposition(Plan(query), 7);
        var proof = command.EntityComposition;
        Assert.IsNotNull(proof); Assert.AreEqual(3, proof.Stages.Length);
        Assert.AreEqual(3, proof.Source.Pipeline.Last(stage => stage.Contains("$limit"))["$limit"].AsInt32);
        Assert.IsTrue(proof.Stages[0].HasTraversalEnvelope);
        Assert.IsFalse(proof.Stages[1].HasTraversalEnvelope);
        var sort = proof.Stages[1].Pipeline.Last(stage => stage.Contains("$sort"))["$sort"].AsBsonDocument;
        Assert.AreEqual(-1, sort["entitySort0.value"].AsInt32);
        Assert.AreEqual(1, sort["entitySort1.value"].AsInt32);
        Assert.AreEqual(1, sort["row.HolonId"].AsInt32);
        Assert.AreEqual(1, proof.Stages[2].Pipeline.Last(stage => stage.Contains("$limit"))["$limit"].AsInt32);
        Assert.AreEqual(8, command.Pipeline.Last()["$limit"].AsInt32);
    }

    [TestMethod]
    public void ProtectedStageProofPrecedesLaterEmptyWindowAndNeverFiltersAwayErrors()
    {
        var traversal = CosmicQueryTraversalOptions.Create(3, 1).Result;
        var query = Source().Compose().Result.Where(row => row.Name == "coffee" ||
            CosmicQueryRelations.AnyDescendant(row, child => true, traversal)).Result.Take(0).Result;
        var command = new MongoCqlTranslator().CompileEntityComposition(Plan(query), 10);
        var stages = command.EntityComposition.Stages;
        Assert.IsTrue(stages[0].RequiresMembership); Assert.IsTrue(stages[0].HasTraversalEnvelope);
        Assert.IsTrue(stages[0].Pipeline.Any(stage => stage.Contains("$lookup")));
        StringAssert.Contains(stages[0].Pipeline.Last().ToJson(), "$error");
        Assert.IsFalse(stages[0].Pipeline.Any(stage => stage.Contains("$match") && stage["$match"].ToJson().Contains("false")));
        Assert.IsTrue(stages[1].Pipeline.Last()["$match"]["$expr"]["$literal"].IsBoolean);
        Assert.IsFalse(stages[1].Pipeline.Last()["$match"]["$expr"]["$literal"].AsBoolean);
    }

    [TestMethod]
    public void ProtectedSourceEnvelopeIsValidatedBeforeAnyStageUnwrapsIt()
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
            .WhereAnyDescendant(row => true, CosmicQueryTraversalOptions.Create(3, 1).Result).Take(3).Result;
        var query = source.Compose().Result.Take(0).Result;
        var command = new MongoCqlTranslator().CompileEntityComposition(Plan(query), 10);
        Assert.IsTrue(command.EntityComposition.Source.HasTraversalEnvelope);
        Assert.IsTrue(command.EntityComposition.Source.RequiresMembership);
        Assert.IsTrue(command.EntityComposition.Source.Pipeline.Any(stage => stage.Contains("$facet")));
        Assert.IsFalse(command.EntityComposition.Stages[0].HasTraversalEnvelope);
    }

    [TestMethod]
    public void FinalGraphNodeBudgetCannotTruncateTheCompositionSource()
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
            .WithChildren(CosmicQueryGraphOptions.Create(maximumNodes: 1).Result).Take(8).Result;
        var query = source.Compose().Result.Where(row => row.Name == "last").Result;
        var compiled = new MongoCqlTranslator().CompileEntityComposition(Plan(query), 10);
        Assert.AreEqual(8, compiled.EntityComposition.Source.FetchCount);
        Assert.AreEqual(8, compiled.EntityComposition.Source.Pipeline.Last(stage => stage.Contains("$limit"))["$limit"].AsInt32);
    }

    [TestMethod]
    public async Task EntityStageAdmissionAndCancellationPrecedeProviderIo()
    {
        var provider = new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1", "entity-stage-no-io");
        var plan = Plan(Source().Compose().Result.Where(row => true).Result);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var cancelled = await provider.ExecuteCosmicEntityCompositionAsync(plan, cancellationToken: cancellation.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", cancelled.ErrorCode);
        var inactive = await provider.ExecuteCosmicEntityCompositionAsync(plan);
        Assert.AreEqual("CQL_PROVIDER_NOT_READY", inactive.ErrorCode);
        var invalid = await provider.ExecuteCosmicEntityCompositionAsync<Holon>(null!);
        Assert.AreEqual("CQL_PLAN_INVALID", invalid.ErrorCode);
    }
}


