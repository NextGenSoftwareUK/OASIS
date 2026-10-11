using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaProviderValidationTests
{
    [TestMethod]
    public void LocalNetworkRequiresExplicitConsensusNodes()
    {
        Assert.ThrowsException<ArgumentException>(() => new HashgraphOASIS(network: "local"));
        Assert.ThrowsException<ArgumentException>(() => new HashgraphOASIS(network: "typo"));
    }

    [DataTestMethod]
    [DataRow("remote.example:50211", "0.0.3")]
    [DataRow("127.0.0.1:0", "0.0.3")]
    [DataRow("127.0.0.1:70000", "0.0.3")]
    [DataRow("127.0.0.1:50211", "invalid")]
    public void LocalConsensusConfigurationRejectsInvalidTargets(string endpoint, string account)
    {
        Assert.ThrowsException<ArgumentException>(() => new HashgraphOASIS(network: "local",
            consensusNodes: new Dictionary<string, string> { [endpoint] = account }));
    }

    [TestMethod]
    public void PublicNetworkCannotSilentlyUseCustomNodes()
    {
        Assert.ThrowsException<ArgumentException>(() => new HashgraphOASIS(network: "testnet",
            consensusNodes: new Dictionary<string, string> { ["127.0.0.1:50211"] = "0.0.3" }));
    }

    [TestMethod]
    public async Task NativeAmountPrecisionIsRejectedBeforeSdkSubmission()
    {
        var provider = new HashgraphOASIS(network: "testnet");
        var result = await provider.SendTokenAsync(new SendWeb3TokenRequest
        {
            FromWalletAddress = "0.0.1001", ToWalletAddress = "0.0.1002",
            Amount = 0.000000001m, OwnerPrivateKey = "synthetic-invalid-key"
        });
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message, "tinybars");
    }

    [TestMethod]
    public async Task MissingKeyMustNotReturnTransactionSuccess()
    {
        var result = await new HashgraphOASIS(network: "testnet").SendTokenAsync(new SendWeb3TokenRequest
        {
            FromWalletAddress = "0.0.1001", ToWalletAddress = "0.0.1002", Amount = 1m
        });
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task InvalidSdkKeyErrorsDoNotExposePrivateInput()
    {
        const string secret = "synthetic-private-key-input-never-log";
        var result = await new HashgraphOASIS(network: "testnet").SendTokenAsync(new SendWeb3TokenRequest
        {
            FromWalletAddress = "0.0.1001", ToWalletAddress = "0.0.1002", Amount = 1m, OwnerPrivateKey = secret
        });
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        Assert.IsFalse(result.Message.Contains(secret));
        Assert.IsFalse((result.Exception?.ToString() ?? "").Contains(secret));
        StringAssert.Contains(result.Message, "0.0.1001@");
    }
}
