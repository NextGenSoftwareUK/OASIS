using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaRawTransferTests
{
    [TestMethod]
    public async Task MissingSignerFailsWithoutMirrorSubmission()
    {
        var result = await new HashgraphOASIS(network: "testnet", chainId: "296").SendTransactionAsync("0.0.1001", "0.0.1002", 1m, "");
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message, "explicitly configured sender");
    }
    [TestMethod]
    public void WrongSenderNeverInvokesSigningResolver()
    {
        var calls = 0;
        var provider = new HashgraphOASIS(network: "testnet", chainId: "296", rawTransferAccountId: "0.0.1001", rawTransferSigningKey: () => { calls++; return "fixture-key"; });
        var result = provider.SendTransaction("0.0.1003", "0.0.1002", 1m, "");
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); Assert.AreEqual(0, calls);
        StringAssert.Contains(result.Message, "does not match");
    }
}
