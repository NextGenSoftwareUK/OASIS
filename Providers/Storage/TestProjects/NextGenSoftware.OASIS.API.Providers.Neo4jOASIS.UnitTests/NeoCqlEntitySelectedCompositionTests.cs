using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlEntitySelectedCompositionTests
{
    private static Neo4jOASIS Provider()=>new("bolt://127.0.0.1:1","unused","unused");
    private static readonly CosmicProjectionCompositionExecutionLimits Limits=new(100,1048576,10000,1048576,100,1048576);
    private static CosmicProjectionCompositionPlan<Holon,string> Plan()
    {
        var provider=Provider();
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.Neo4jOASIS).Result)
            .OrderBy(row=>row.Name).Skip(2).Take(4).Result.Compose().Result.Reverse().Result.Take(2).Result;
        var query=source.Select(row=>row.Name).Result.Take(0).Result;
        var plan=CosmicProjectionCompositionPlanner.PlanEntity(query,ProviderType.Neo4jOASIS,
            new CosmicProjectionCompositionTarget(ProviderType.Neo4jOASIS,Neo4jOASIS.CqlCapabilities,
                provider.ProjectionCapabilities,provider.CompositionCapabilities),provider.EntityCompositionCapabilities);
        Assert.IsFalse(plan.IsError,plan.Message);return plan.Result;
    }
    [TestMethod]
    public void CombinedStatementPreservesCanonicalProofEntityOrderAndSelectedInputs()
    {
        var command=Neo4jOASIS.CompileCqlComposition(Plan(),Limits);
        Assert.AreEqual(2,command.Parameters["entityOffset"]);
        Assert.AreEqual(4,command.Parameters["entityTake"]);
        StringAssert.Contains(command.Text,"reverse(selected)");
        StringAssert.Contains(command.Text,"entityProofs[pos] AS proof");
        StringAssert.Contains(command.Text,"RETURN -3 AS phase");
        StringAssert.Contains(command.Text,"entry.h.ProviderRevisionId");
        StringAssert.Contains(command.Text,"entry.h.SnapshotJson");
        StringAssert.Contains(command.Text,"outcome.selected[position]");
        StringAssert.Contains(command.Text,"domainValid:");
        Assert.IsFalse(command.Text.Contains("RETURN collect(input) AS input } WITH input,CASE",StringComparison.Ordinal),"The combined path cannot substitute the original flat source.");
        Assert.IsTrue(command.Text.EndsWith("ORDER BY phase ASC,position ASC",StringComparison.Ordinal));
    }
    [TestMethod]
    public async Task ActualCombinedAdmissionAndCancellationPrecedeNativeIo()
    {
        var provider=Provider();var plan=Plan();
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",(await provider.ExecuteCosmicProjectionCompositionAsync(plan,Limits)).ErrorCode);
        Assert.AreEqual("CQL_PROVIDER_NOT_READY",(await provider.ExecuteCosmicEntityProjectionCompositionAsync(plan,Limits)).ErrorCode);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED",(await provider.ExecuteCosmicEntityProjectionCompositionAsync(plan,Limits,cancellation.Token)).ErrorCode);
        Assert.AreEqual("CQL_PLAN_INVALID",(await provider.ExecuteCosmicEntityProjectionCompositionAsync<Holon,string>(null!,Limits)).ErrorCode);
    }
}
