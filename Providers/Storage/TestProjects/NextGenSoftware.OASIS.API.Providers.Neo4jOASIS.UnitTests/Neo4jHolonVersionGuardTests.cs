using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jHolonVersionGuardTests
{
    [DataTestMethod]
    [DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NegativeRootVersionRejectsBeforeNativeRead(bool synchronous,bool byKey)
    {
        var provider=new Provider("bolt://127.0.0.1:17687","neo4j","owned",
            ()=>Neo4jLookupOutcomeTests.ReadProbe.Make<IDriver>(0));
        provider.IsProviderActivated=true;
        var id=Guid.NewGuid();
        var result=byKey
            ? synchronous?provider.LoadHolon(id.ToString(),version:-1):await provider.LoadHolonAsync(id.ToString(),version:-1)
            : synchronous?provider.LoadHolon(id,version:-1):await provider.LoadHolonAsync(id,version:-1);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
        Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
    }
}
