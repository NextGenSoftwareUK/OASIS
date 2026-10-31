using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaMirrorNetworkBindingTests
{
    [DataTestMethod]
    [DataRow("mainnet")] [DataRow("testnet")] [DataRow("previewnet")]
    public void DefaultMirrorFollowsConfiguredConsensusNetwork(string network)
    {
        var provider=new HashgraphOASIS(network:network);
        var client=(HttpClient)typeof(HashgraphOASIS).GetField("_httpClient",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
        try{Assert.AreEqual(new Uri("https://"+network+".mirrornode.hedera.com/api/v1/"),client.BaseAddress);Assert.IsFalse(provider.IsProviderActivated);}
        finally{client.Dispose();}
    }
    [DataTestMethod]
    [DataRow("https://mainnet-public.mirrornode.hedera.com","testnet")]
    [DataRow("https://mainnet.mirrornode.hedera.com","previewnet")]
    [DataRow("https://testnet.mirrornode.hedera.com","mainnet")]
    [DataRow("https://previewnet.mirrornode.hedera.com","testnet")]
    public void KnownPublicMirrorCannotBeRelabelledAsAnotherNetwork(string endpoint,string network)
        =>Assert.ThrowsException<ArgumentException>(()=>new HashgraphOASIS(endpoint,network));

    [TestMethod]
    public void ExplicitCustomMirrorRemainsConfiguredTrustBoundary()
    {
        var provider=new HashgraphOASIS("http://127.0.0.1:12345","testnet");
        var client=(HttpClient)typeof(HashgraphOASIS).GetField("_httpClient",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
        try{Assert.AreEqual("http://127.0.0.1:12345/api/v1/",client.BaseAddress!.AbsoluteUri);}
        finally{client.Dispose();}
    }
}
