using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

public sealed partial class NeoCqlEntityCompositionTests
{
    [TestMethod]
    public void NativeReversalKeepsProtectedStateAndChargesEveryStageInput()
    {
        var query=CosmicQuery<Holon>.Create().Take(3).Result.Compose().Result.Reverse().Result.Take(2).Result.Reverse().Result;
        var command=Neo4jOASIS.CompileCqlEntityComposition(Plan(query),10);
        Assert.AreEqual(2,command.Text.Split("reverse(selected)").Length-1);
        Assert.AreEqual(3,command.Text.Split("entityWork+size(selected)").Length-1);
        StringAssert.Contains(command.Text,"reverse(selected) AS selected,entityCycle,entityLimit,entityWork");
        StringAssert.Contains(command.Text,"item IN entitySource");
    }
}
