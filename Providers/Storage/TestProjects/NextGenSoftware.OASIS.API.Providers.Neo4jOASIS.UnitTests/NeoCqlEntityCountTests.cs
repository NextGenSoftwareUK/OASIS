using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

public sealed partial class NeoCqlEntityCompositionTests
{
    [TestMethod]
    public void NativeCountReducesCompleteStagesAndStreamsIndependentSourceProof()
    {
        var source=CosmicQuery<Holon>.Create().OrderBy(row=>row.Name).Take(4).Result;
        var query=source.Compose().Result.Where(row=>row.Name=="coffee").Result.Skip(1).Result.Take(2).Result;
        var command=Neo4jOASIS.CompileCqlEntityCount(Plan(query));
        StringAssert.Contains(command.Text,"count:size(selected)");
        StringAssert.Contains(command.Text,"entry.count AS entityCount");
        StringAssert.Contains(command.Text,"item IN entitySource");
        Assert.IsFalse(command.Text.Contains("item IN selected[..$entityOutputTake]"));
        StringAssert.Contains(command.Text,"entityLimit OR entityWork>$entityWorkBound");
        StringAssert.Contains(command.Text,"false AS entityInput,null AS entityCount LIMIT 1");
        Assert.AreEqual(4,command.Parameters["entityTake"]);
    }
    [TestMethod]
    public void EmptyCountStillCapturesProtectedStagesAndOneZeroCountAcknowledgement()
    {
        var bounds=CosmicQueryTraversalOptions.Create(3,1).Result;
        var source=CosmicQuery<Holon>.Create().WhereAnyDescendant(row=>true,bounds).Take(0).Result;
        var query=source.Compose().Result.Where(row=>row.Name=="coffee" ||
            CosmicQueryRelations.AnyDescendant(row,child=>true,bounds)).Result.Take(0).Result;
        var command=Neo4jOASIS.CompileCqlEntityCount(Plan(query));
        StringAssert.Contains(command.Text,"entityCycle OR stageCycle");
        StringAssert.Contains(command.Text,"entityLimit OR stageLimit");
        StringAssert.Contains(command.Text,"[{h:null,p:null,input:false,count:size(selected)}]");
        StringAssert.Contains(command.Text,"entityWork+size(selected)");
        StringAssert.Contains(command.Text,"AS traversalLimit,false AS entityInput,null AS entityCount");
        Assert.AreEqual(0,command.Parameters["entityTake"]);
    }
    [TestMethod]
    public async Task CountAdmissionAndCancellationPrecedeNeoIo()
    {
        var provider=new Neo4jOASIS("bolt://127.0.0.1:1","unused","unused");
        Assert.IsTrue(provider.SupportsEntityCount);
        var plan=Plan(CosmicQuery<Holon>.Create().Take(1).Result.Compose().Result.Where(row=>true).Result);
        using var cancel=new CancellationTokenSource();cancel.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED",(await provider.ExecuteCosmicEntityCountAsync(plan,cancel.Token)).ErrorCode);
        Assert.AreEqual("CQL_PLAN_INVALID",(await provider.ExecuteCosmicEntityCountAsync<Holon>(null!)).ErrorCode);
    }
}
