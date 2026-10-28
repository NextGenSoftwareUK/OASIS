using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

public sealed partial class MongoCqlEntityCompositionTests
{
    [TestMethod]
    public void NativeEntityCountReducesAfterStagesWithoutRowPublicationCap()
    {
        var query=Source().Compose().Result.Where(row=>row.Name=="coffee").Result.Take(2).Result;
        var count=new MongoCqlTranslator().CompileEntityCount(Plan(query));
        Assert.IsTrue(count.RequiresSnapshot); Assert.IsNotNull(count.EntityComposition);
        Assert.AreEqual(2,count.EntityComposition.Stages.Length);
        Assert.AreEqual(3,count.EntityComposition.Source.FetchCount);
        Assert.AreEqual("count",count.Pipeline[^2]["$count"].AsString);
        Assert.AreEqual(2,count.Pipeline.Last(stage=>stage.Contains("$limit"))["$limit"].AsInt32);
        Assert.AreEqual(0,count.Pipeline[^1]["$project"]["error"]["$literal"].AsInt32);
        Assert.IsFalse(count.HasTraversalEnvelope);
    }
    [TestMethod]
    public void EmptyEntityCountCannotBypassProtectedStageProof()
    {
        var limits=CosmicQueryTraversalOptions.Create(3,1).Result;
        var query=Source().Compose().Result.Where(row=>row.Name=="coffee" ||
            CosmicQueryRelations.AnyDescendant(row,child=>true,limits)).Result.Take(0).Result;
        var count=new MongoCqlTranslator().CompileEntityCount(Plan(query));
        Assert.IsFalse(count.IsEmpty,"The snapshot owner must validate every protected stage even when output is empty.");
        Assert.IsTrue(count.RequiresMembership); Assert.IsTrue(count.RequiresSnapshot);
        Assert.IsTrue(count.EntityComposition.Stages[0].HasTraversalEnvelope);
        Assert.IsTrue(count.EntityComposition.Stages[0].Pipeline.Any(stage=>stage.Contains("$lookup")));
        Assert.IsFalse(count.EntityComposition.Stages[1].Pipeline[^1]["$match"]["$expr"]["$literal"].AsBoolean);
    }
    [TestMethod]
    public async Task NativeEntityCountAdmissionAndCancellationPrecedeIo()
    {
        var provider=new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","entity-count-no-io");
        var plan=Plan(Source().Compose().Result.Where(row=>true).Result);
        Assert.IsTrue(provider.SupportsEntityCount);
        using var cancel=new CancellationTokenSource(); cancel.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED",(await provider.ExecuteCosmicEntityCountAsync(plan,cancel.Token)).ErrorCode);
        Assert.AreEqual("CQL_PROVIDER_NOT_READY",(await provider.ExecuteCosmicEntityCountAsync(plan)).ErrorCode);
        Assert.AreEqual("CQL_PLAN_INVALID",(await provider.ExecuteCosmicEntityCountAsync<Holon>(null!)).ErrorCode);
    }
}
