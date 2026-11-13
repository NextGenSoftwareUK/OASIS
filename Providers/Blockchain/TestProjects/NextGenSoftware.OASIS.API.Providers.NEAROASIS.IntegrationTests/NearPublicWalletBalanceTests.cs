using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass]
public sealed class NearPublicWalletBalanceTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task WalletReadsFinalizedMainnetBalanceWithoutStorageActivation(bool synchronous)
    {
        if (Environment.GetEnvironmentVariable("OASIS_NEAR_PUBLIC_WALLET_READ") != "1")
            Assert.Inconclusive("Explicitly enable the public NEAR wallet read.");

        var provider = new NEAROASIS("https://rpc.mainnet.near.org", "mainnet", "mainnet",
            contractAddress: "", accountId: "", privateKey: "");
        var request = new GetWeb3WalletBalanceRequest { WalletAddress = "near" };
        var result = synchronous ? provider.GetBalance(request) : await provider.GetBalanceAsync(request);

        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(result.Result > 0d);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
