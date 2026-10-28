using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoCqlEntitySelectedCompositionTests
{
    private static MongoDBOASIS Provider()=>new("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","combined-no-io");
    private static readonly CosmicProjectionCompositionExecutionLimits Limits=new(100,1048576,10000,1048576,100,1048576);
    private static CosmicProjectionCompositionPlan<Holon,string> Plan()
    {
        var provider=Provider();
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
            .OrderBy(row=>row.Name).Skip(2).Take(4).Result.Compose().Result
            .OrderByDescending(row=>row.Version).Result.Reverse().Result.Take(2).Result;
        var query=source.Select(row=>row.Name).Result.Take(1).Result;
        var plan=CosmicProjectionCompositionPlanner.PlanEntity(query,ProviderType.MongoDBOASIS,
            new CosmicProjectionCompositionTarget(ProviderType.MongoDBOASIS,MongoDBOASIS.CqlCapabilities,
                provider.ProjectionCapabilities,provider.CompositionCapabilities),provider.EntityCompositionCapabilities);
        Assert.IsFalse(plan.IsError,plan.Message);return plan.Result;
    }
    [TestMethod]
    public void CombinedSourceRetainsEntityProofAndRanksUsingFinalEntityOrder()
    {
        var command=new MongoCqlTranslator().CompileComposition(Plan(),Limits);
        Assert.IsNotNull(command.Source.EntityComposition);Assert.IsTrue(command.Source.RequiresSnapshot);
        Assert.AreEqual(1,command.Source.Pipeline.Count(stage=>stage.Contains("$skip")));
        Assert.AreEqual(3,command.Source.EntityComposition.Stages.Length);
        Assert.AreEqual(1,command.Source.EntityOrderingKeyCount);
        var window=command.Source.Pipeline.Last(stage=>stage.Contains("$setWindowFields"))["$setWindowFields"].AsBsonDocument;
        Assert.AreEqual(1,window["sortBy"]["entityOrder0.value"].AsInt32,"Descending entity Version reversed becomes ascending.");
        Assert.AreEqual(-1,window["sortBy"]["id"].AsInt32,"Reversal also reverses identity ties.");
        Assert.IsFalse(window["sortBy"].AsBsonDocument.Contains("o0.value"),"Original Name ordering cannot replace the final entity order.");
        Assert.AreEqual(1,command.Stages.Length);
        Assert.IsTrue(command.Stages[0].Length>command.Source.Pipeline.Length);
    }
    [TestMethod]
    public async Task CombinedAndFlatEntryPointsHaveIndependentAdmissionBeforeIo()
    {
        var provider=Provider();var plan=Plan();
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",(await provider.ExecuteCosmicProjectionCompositionAsync(plan,Limits)).ErrorCode);
        Assert.AreEqual("CQL_PROVIDER_NOT_READY",(await provider.ExecuteCosmicEntityProjectionCompositionAsync(plan,Limits)).ErrorCode);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED",(await provider.ExecuteCosmicEntityProjectionCompositionAsync(plan,Limits,cancellation.Token)).ErrorCode);
        Assert.AreEqual("CQL_PLAN_INVALID",(await provider.ExecuteCosmicEntityProjectionCompositionAsync<Holon,string>(null!,Limits)).ErrorCode);
    }
}
