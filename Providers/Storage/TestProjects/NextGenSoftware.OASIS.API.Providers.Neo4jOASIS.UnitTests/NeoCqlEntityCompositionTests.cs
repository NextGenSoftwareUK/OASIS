using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class NeoCqlEntityCompositionTests
{
    private static CosmicEntityCompositionPlan<Holon> Plan(CosmicEntityCompositionQuery<Holon> query,
        CosmicEntityCompositionOptions? options=null)
    {
        var provider = new Neo4jOASIS("bolt://127.0.0.1:1", "unused", "unused");
        var plan = CosmicEntityCompositionPlanner.Plan(query, ProviderType.Neo4jOASIS,
            new CosmicQueryTarget(ProviderType.Neo4jOASIS, Neo4jOASIS.CqlCapabilities), provider.EntityCompositionCapabilities, options);
        Assert.IsFalse(plan.IsError, plan.Message); return plan.Result;
    }
    [TestMethod]
    public void StageOrderAndInputProofRemainIndependentOfFinalEmptyPage()
    {
        var source = CosmicQuery<Holon>.Create().OrderBy(row => row.Name).Skip(1).Take(4).Result;
        var query = source.Compose().Result.Where(row => row.Name == "coffee").Result
            .OrderByDescending(row => row.Version).Result.ThenBy(row => row.Name).Result.Take(0).Result;
        var command = Neo4jOASIS.CompileCqlEntityComposition(Plan(query), 10);
        Assert.AreEqual(1, command.Parameters["entityOffset"]); Assert.AreEqual(4, command.Parameters["entityTake"]);
        StringAssert.Contains(command.Text, "AS entitySource");
        StringAssert.Contains(command.Text, "entityWork+size(selected)");
        StringAssert.Contains(command.Text, "entityLimit OR entityWork>$entityWorkBound");
        StringAssert.Contains(command.Text, "ORDER BY entityPosition");
        StringAssert.Contains(command.Text, "WITH item.h AS h,item.p AS p WITH h,p,");
        StringAssert.Contains(command.Text, "stageSort0 DESC,stageSort1 DESC,stageSort2 ASC,stageSort3 ASC,p.f_Id ASC");
        StringAssert.Contains(command.Text, "item IN entitySource");
        StringAssert.Contains(command.Text, "entry.input AS entityInput");
    }
    [TestMethod]
    public void ProtectedScopesKeepFailureStateAndBoundedReferenceCapture()
    {
        var bounds = CosmicQueryTraversalOptions.Create(3, 1).Result;
        var source = CosmicQuery<Holon>.Create().Skip(1).Result;
        var query = source.Compose().Result.Where(row => row.Name == "coffee" ||
            CosmicQueryRelations.AnyDescendant(row, child => true, bounds)).Result.Take(0).Result;
        var command = Neo4jOASIS.CompileCqlEntityComposition(Plan(query, new CosmicEntityCompositionOptions(maximumInputRows: 9)), 10);
        Assert.AreEqual(10, command.Parameters["entityTake"]);
        StringAssert.Contains(command.Text, "entityCycle OR stageCycle");
        StringAssert.Contains(command.Text, "entityLimit OR stageLimit");
        StringAssert.Contains(command.Text, "collect({h:h,p:p})");
        StringAssert.Contains(command.Text, "WITH h,p,entityPosition,desc");
        StringAssert.Contains(command.Text, "CASE WHEN entityCycle OR entityLimit");
    }
    [TestMethod]
    public void SnapshotCapabilityIsNeverInferredFromEntityStages()
    {
        var source = CosmicQuery<Holon>.Create().WithChildren(CosmicQueryGraphOptions
            .Create(CosmicQueryGraphConsistency.ProviderSnapshot).Result).Result;
        var provider = new Neo4jOASIS("bolt://127.0.0.1:1", "unused", "unused");
        var plan = CosmicEntityCompositionPlanner.Plan(source.Compose().Result.Where(row => true).Result,
            ProviderType.Neo4jOASIS, new CosmicQueryTarget(ProviderType.Neo4jOASIS, Neo4jOASIS.CqlCapabilities), provider.EntityCompositionCapabilities);
        Assert.AreEqual("CQL_CONSISTENCY_UNSUPPORTED", plan.ErrorCode);
    }
}
