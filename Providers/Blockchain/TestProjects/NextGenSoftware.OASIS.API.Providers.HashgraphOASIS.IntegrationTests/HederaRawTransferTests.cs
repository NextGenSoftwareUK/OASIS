using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class HederaRawTransferTests
{
    [TestMethod]
    public void RawSignerDnaRoundTripsOnlyAccountAndEnvironmentVariableName()
    {
        var settings = new NextGenSoftware.OASIS.API.DNA.HashgraphOASISProviderSettings {
            RawTransferAccountId = "0.0.1001", RawTransferSigningKeyEnvironmentVariable = "OWNED_TEST_HEDERA_RAW_KEY" };
        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        var restored = System.Text.Json.JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.DNA.HashgraphOASISProviderSettings>(json)!;
        Assert.AreEqual(settings.RawTransferAccountId, restored.RawTransferAccountId);
        Assert.AreEqual(settings.RawTransferSigningKeyEnvironmentVariable, restored.RawTransferSigningKeyEnvironmentVariable);
        var defaults = new NextGenSoftware.OASIS.API.DNA.HashgraphOASISProviderSettings();
        Assert.IsNull(defaults.RawTransferAccountId);
        Assert.AreEqual("OASIS_HEDERA_RAW_TRANSFER_PRIVATE_KEY", defaults.RawTransferSigningKeyEnvironmentVariable);
    }

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
            var helper = new HashgraphClient(accountId: "0.0.1001", privateKey: "fixture-transfer-key", transferNetwork: "testnet");
            var data = new HashgraphTransactionData { FromAddress = "0.0.1001", ToAddress = "0.0.1002", Amount = 1m, Memo = "" };
            var helperAsync = await helper.SendTransactionAsync(data);
            StringAssert.StartsWith(helperAsync.TransactionId, "0.0.1001@");
            var helperSync = helper.SendTransaction(data);
            Assert.AreNotEqual(helperAsync.TransactionId, helperSync.TransactionId);
            var localHelper = new HashgraphClient(accountId: "0.0.1001", privateKey: "fixture-transfer-key", transferNetwork: "local",
                transferConsensusNodes: new Dictionary<string, string> { ["127.0.0.1:50211"] = "0.0.3" });
            StringAssert.StartsWith((await localHelper.SendTransactionAsync(data)).TransactionId, "0.0.1001@");
            var failed = await provider.SendTransactionAsync("0.0.1001", "0.0.1002", 1m, "controlled-receipt-failure");
            Assert.IsTrue(failed.IsError); Assert.IsNull(failed.Result);
            StringAssert.Contains(failed.Message, "0.0.1001@");
            Assert.IsFalse(failed.Message.Contains("fixture-transfer-key"));
            var helperFailure = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => helper.SendTransactionAsync(
                new HashgraphTransactionData { FromAddress = "0.0.1001", ToAddress = "0.0.1002", Amount = 1m, Memo = "controlled-receipt-failure" }));
            StringAssert.Contains(helperFailure.Message, "0.0.1001@");
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

    [TestMethod]
    public async Task HelperLocalNetworkRequiresExplicitLoopbackConsensusNodes()
    {
        var helper = new HashgraphClient(accountId: "0.0.1001", privateKey: "unused", transferNetwork: "local");
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => helper.SendTransactionAsync(new HashgraphTransactionData {
            FromAddress = "0.0.1001", ToAddress = "0.0.1002", Amount = 1m }));
    }
}
