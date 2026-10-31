using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class CosmicQueryWindowTests
{
    [TestMethod]
    public void ExactMetadataAndCanonicalOrderingAdmitAComposedWindow()
    {
        var built=CosmicQuery<Holon>.Create().OrderBy(h=>CosmicQueryMetadata.Value<int?>(h,"score"))
            .ThenByDescending(h=>h.Name).Skip(1).Take(2);
        Assert.IsFalse(built.IsError,built.Message);
        var admission=Neo4jOASIS.CqlCapabilities.Admit(built.Result);
        Assert.IsFalse(admission.IsError,admission.Message);
    }

    [TestMethod]
    public async Task EmptyWindowCompletesWithoutActivationOrNativeSession()
    {
        var driver=DispatchProxy.Create<IDriver,CosmicQueryAdmissionTests.NoIo>();
        var provider=new Neo4jOASIS("bolt://owned.invalid","owned","owned",()=>driver);
        var built=CosmicQuery<Holon>.Create().OrderBy(h=>h.Name).Take(0);
        var plan=CosmicQueryPlanner.Plan(built.Result,ProviderType.Neo4jOASIS,
            new[]{new CosmicQueryTarget(ProviderType.Neo4jOASIS,Neo4jOASIS.CqlCapabilities)});
        Assert.IsFalse(plan.IsError,plan.Message);
        var result=await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(0,result.Result.Count);
        Assert.AreEqual(0,((CosmicQueryAdmissionTests.NoIo)driver).Calls);
    }
}
