using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlEntityProjectionTests
{
    private static Neo4jOASIS Provider()=>new("bolt://127.0.0.1:1","unused","unused");
    private static CosmicEntityProjectionPlan<Holon,TResult> Plan<TResult>(CosmicEntityProjectionQuery<Holon,TResult> query)
    {
        var provider=Provider();
        var result=CosmicEntityProjectionPlanner.Plan(query,ProviderType.Neo4jOASIS,
            new CosmicProjectionTarget(ProviderType.Neo4jOASIS,Neo4jOASIS.CqlCapabilities,provider.ProjectionCapabilities),provider.EntityCompositionCapabilities);
        Assert.IsFalse(result.IsError,result.Message);return result.Result;
    }

    [TestMethod]
    public void NativeProjectionKeepsSourceProofAndStableStagePositionsBeforeCells()
    {
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.Neo4jOASIS).Result)
            .OrderBy(row=>row.Name).Skip(2).Take(4).Result.Compose().Result;
        var query=source.Reverse().Result.Take(2).Result.Select(row=>new {row.Name,row.Version}).Result;
        var command=Neo4jOASIS.CompileCqlEntityProjection(Plan(query),5,1234);
        Assert.AreEqual(2,command.Parameters["entityOffset"]);
        Assert.AreEqual(4,command.Parameters["entityTake"]);
        Assert.AreEqual(6,command.Parameters["entityOutputTake"]);
        Assert.AreEqual(1234,command.Parameters["entityProjectionBytes"]);
        StringAssert.Contains(command.Text,"reverse(selected)");
        StringAssert.Contains(command.Text,"item IN entitySource");
        StringAssert.Contains(command.Text,"entry.input AND size(entry.h.SnapshotJson)");
        StringAssert.Contains(command.Text,"CASE WHEN entry.input OR entityCycle OR entityLimit THEN");
        StringAssert.Contains(command.Text,"{s:entityState1,v:entityValue1}");
        Assert.IsTrue(command.Text.EndsWith("ORDER BY entityPosition",StringComparison.Ordinal));
    }

    [TestMethod]
    public void EmptyProjectionRetainsProtectedErrorsAndSourceWorkBudgets()
    {
        var traversal=CosmicQueryTraversalOptions.Create(3,1).Result;
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.Neo4jOASIS).Result).Take(1).Result
            .Compose().Result.Where(row=>row.Name=="coffee" || CosmicQueryRelations.AnyDescendant(row,child=>true,traversal)).Result
            .Take(0).Result.Select(row=>row.Name).Result;
        var command=Neo4jOASIS.CompileCqlEntityProjection(Plan(query),0,0);
        Assert.AreEqual(1,command.Parameters["entityOutputTake"]);
        StringAssert.Contains(command.Text,"entityCycle OR stageCycle");
        StringAssert.Contains(command.Text,"entityLimit OR stageLimit");
        StringAssert.Contains(command.Text,"entityLimit OR entityWork>$entityWorkBound");
        StringAssert.Contains(command.Text,"false AS entityInput,-1 AS entityPosition,false AS payloadLimit,[] AS cells");
    }

    [TestMethod]
    public async Task ProjectionAdmissionAndCancellationPrecedeNativeIo()
    {
        var provider=Provider();
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.Neo4jOASIS).Result).Take(1).Result.Compose().Result.Skip(0).Result;
        var plan=Plan(source.Select(row=>row.Name).Result);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED",(await provider.ExecuteCosmicEntityProjectionAsync(plan,cancellationToken:cancellation.Token)).ErrorCode);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE",(await provider.ExecuteCosmicEntityProjectionAsync(plan)).ErrorCode);
        Assert.AreEqual("CQL_QUERY_LIMIT",(await provider.ExecuteCosmicEntityProjectionAsync(plan,maximumResults:-1)).ErrorCode);
        Assert.AreEqual("CQL_PLAN_INVALID",(await provider.ExecuteCosmicEntityProjectionAsync<Holon,string>(null!)).ErrorCode);
    }
}
