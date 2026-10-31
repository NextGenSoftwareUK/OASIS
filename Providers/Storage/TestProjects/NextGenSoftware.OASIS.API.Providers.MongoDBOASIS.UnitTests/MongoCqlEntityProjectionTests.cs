using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlEntityProjectionTests
{
    private static MongoDBOASIS Provider() => new("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1", "entity-projection-no-io");
    private static CosmicEntityProjectionPlan<Holon,TResult> Plan<TResult>(CosmicEntityProjectionQuery<Holon,TResult> query)
    {
        var provider=Provider();
        var result=CosmicEntityProjectionPlanner.Plan(query,ProviderType.MongoDBOASIS,
            new CosmicProjectionTarget(ProviderType.MongoDBOASIS,MongoDBOASIS.CqlCapabilities,provider.ProjectionCapabilities),
            provider.EntityCompositionCapabilities);
        Assert.IsFalse(result.IsError,result.Message);return result.Result;
    }

    [TestMethod]
    public void SelectionFollowsCompleteEntityPipelineWithoutReapplyingSourcePage()
    {
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
            .OrderBy(row=>row.Name).Skip(2).Take(4).Result.Compose().Result;
        var query=source.Where(row=>row.Version>1).Result.OrderByDescending(row=>row.Version).Result.Take(1).Result
            .Select(row=>new {row.Name,row.Version}).Result;
        var command=new MongoCqlTranslator().CompileEntityProjection(Plan(query),10);
        Assert.IsTrue(command.RequiresSnapshot);
        Assert.IsNotNull(command.EntityComposition);
        Assert.AreEqual(3,command.EntityComposition.Stages.Length);
        Assert.AreEqual(1,command.Pipeline.Count(stage=>stage.Contains("$skip")));
        Assert.AreEqual(4,command.EntityComposition.Source.Pipeline.Last(stage=>stage.Contains("$limit"))["$limit"].AsInt32);
        Assert.AreEqual(11,command.Pipeline[^2]["$limit"].AsInt32);
        Assert.AreEqual(2,command.Pipeline[^1]["$project"]["cells"].AsBsonArray.Count);
    }

    [TestMethod]
    public void ProtectedSourceAndStageProofRemainBeforeZeroProjectionOutput()
    {
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
            .OrderBy(row=>row.Id).Take(4).Result.Compose().Result;
        var traversal=CosmicQueryTraversalOptions.Create(maximumDepth:3,maximumNodes:1).Result;
        var query=source.Where(row=>CosmicQueryRelations.AnyDescendant(row,child=>true,traversal)).Result.Take(0).Result
            .Select(row=>row.Name).Result;
        var command=new MongoCqlTranslator().CompileEntityProjection(Plan(query),0);
        Assert.IsFalse(command.IsEmpty,"An empty publication does not skip protected proof.");
        Assert.IsTrue(command.RequiresMembership);
        Assert.IsTrue(command.EntityComposition.Stages[0].HasTraversalEnvelope);
        Assert.AreEqual(1,command.Pipeline[^2]["$limit"].AsInt32);
    }

    [TestMethod]
    public async Task EntityProjectionHasIndependentAdmissionBeforeActivationOrIo()
    {
        var provider=Provider();
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
            .OrderBy(row=>row.Id).Take(4).Result.Compose().Result.Where(row=>true).Result;
        var plan=Plan(source.Select(row=>row.Name).Result);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED",(await provider.ExecuteCosmicEntityProjectionAsync(plan,cancellationToken:cancellation.Token)).ErrorCode);
        Assert.AreEqual("CQL_PROVIDER_NOT_READY",(await provider.ExecuteCosmicEntityProjectionAsync(plan)).ErrorCode);
        Assert.AreEqual("CQL_QUERY_LIMIT",(await provider.ExecuteCosmicEntityProjectionAsync(plan,maximumResults:-1)).ErrorCode);
        Assert.AreEqual("CQL_PLAN_INVALID",(await provider.ExecuteCosmicEntityProjectionAsync<Holon,string>(null!)).ErrorCode);
    }
}
