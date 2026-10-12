using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class HederaRawTransferTests
{
    [TestMethod]
    public async Task BoundSenderUsesConfiguredNetworkAndExactSdkTransferRequest()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/hedera-burn-wallet-fixture.mjs"))) directory = directory.Parent;
        Assert.IsNotNull(directory);
        var previous = Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE");
        try
        {
            Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE", Path.Combine(directory.FullName, "Scripts/TestHosts/hedera-burn-wallet-fixture.mjs"));
            var calls = 0;
            var provider = new HashgraphOASIS(network: "testnet", chainId: "296", rawTransferAccountId: "0.0.1001", rawTransferSigningKey: () => { calls++; return "fixture-transfer-key"; });
            var sent = await provider.SendTransactionAsync("0.0.1001", "0.0.1002", 1m, "");
            Assert.IsFalse(sent.IsError, sent.Message); Assert.AreEqual(1, calls);
            StringAssert.StartsWith(sent.Result.TransactionResult, "0.0.1001@");
            var sync = provider.SendTransaction("0.0.1001", "0.0.1002", 1m, "");
            Assert.IsFalse(sync.IsError, sync.Message); Assert.AreEqual(2, calls);
            Assert.AreNotEqual(sent.Result.TransactionResult, sync.Result.TransactionResult);
        }
        finally { Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE", previous); }
    }

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
