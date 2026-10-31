using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests;

[TestClass]
public class CosmosFailureTests
{
    [TestMethod]
    public async Task UnreachableRpcCannotActivateStorage()
    {
        // Valid public development key; this test never contacts a public chain.
        var key = new string('0', 63) + "1";
        var provider = new CosmosBlockChainOASIS("http://127.0.0.1:1", "oasis-test", key,
            "wasm1invalidcontract", "wasm", "0.025stake");
        var result = await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.Result);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsNotNull(result.Exception);
        StringAssert.Contains(result.Message, "could not reach its store");
        // Missing SDK installation is a different failure, not RPC evidence.
        Assert.IsFalse(result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.Message.Contains("Install the locked", StringComparison.OrdinalIgnoreCase));
    }
}
