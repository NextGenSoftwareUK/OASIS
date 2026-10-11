using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaCollectionTests
{
    [DataTestMethod]
    [DataRow("0.0.0", "Oak", "OAK", 10L, 100L)]
    [DataRow("0.0.1001", "", "OAK", 10L, 100L)]
    [DataRow("0.0.1001", "Oak", "", 10L, 100L)]
    [DataRow("0.0.1001", "Oak", "OAK", 0L, 100L)]
    [DataRow("0.0.1001", "Oak", "OAK", 10L, 0L)]
    public async Task InvalidCollectionInputFailsBeforeSdkAccess(string payer,string name,string symbol,long supply,long fee)
    {
        var provider=new HashgraphOASIS(network:"testnet");
        var result=await provider.CreateNftCollectionAsync(payer,"",name,symbol,supply,fee);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
    }
}
