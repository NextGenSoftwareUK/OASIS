using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaMirrorBalanceTests
{
    [DataTestMethod]
    [DataRow("")]
    [DataRow("bad")]
    [DataRow("0.0.0")]
    public async Task InvalidAccountCannotBecomeSuccessfulZeroBalance(string account)
    {
        var provider = new HashgraphOASIS(network:"testnet");
        var exact = await provider.GetAccountBalanceAsync(account);
        var shared = await provider.GetBalanceAsync(new GetWeb3WalletBalanceRequest { WalletAddress=account });
        Assert.IsTrue(exact.IsError); Assert.IsTrue(shared.IsError);
    }

    [TestMethod]
    public async Task CancellationAndNullRequestAreExplicitFailuresBeforeSdkLaunch()
    {
        var provider = new HashgraphOASIS(network:"testnet");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.IsTrue((await provider.GetAccountBalanceAsync("0.0.3",cancellation.Token)).IsError);
        Assert.IsTrue((await provider.GetBalanceAsync(null!)).IsError);
    }

    [TestMethod]
    public async Task HederaTestnetPublicNodeBalanceUsesSupportedMirrorWithoutOperatorFunding()
    {
        var provider = new HashgraphOASIS(rpcEndpoint:"https://testnet.mirrornode.hedera.com/api/v1",network:"testnet");
        var balance = await provider.GetAccountBalanceAsync("0.0.3");
        Assert.IsFalse(balance.IsError,balance.Message);
        Assert.IsTrue(balance.Result>0,"Known testnet node account should have a real positive ledger balance.");
        Assert.AreEqual(decimal.Truncate(balance.Result*100000000m),balance.Result*100000000m);
        StringAssert.Contains(balance.Message,"mirror");
    }
}
