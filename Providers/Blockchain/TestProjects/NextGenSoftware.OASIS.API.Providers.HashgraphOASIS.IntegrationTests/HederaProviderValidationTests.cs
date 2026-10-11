using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaProviderValidationTests
{
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
