using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.RoninOASIS.IntegrationTests;

[TestClass]
public sealed class RoninPublicWalletBalanceTests
{
    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task PublicWalletBalanceRequiresRoninMainnet(bool synchronous)
    {
        if (Environment.GetEnvironmentVariable("OASIS_RONIN_PUBLIC_WALLET_READ") != "1")
            Assert.Inconclusive("Enable the public Ronin mainnet balance check explicitly.");

        var request = new GetWeb3WalletBalanceRequest
        {
            WalletAddress = "0xe514d9deb7966c8be0ca922de8a064264ea6bcd4"
        };

        var provider = new RoninOASIS();
        var balance = synchronous ? provider.GetBalance(request) : await provider.GetBalanceAsync(request);
        Assert.IsFalse(balance.IsError, balance.Message);
        Assert.IsTrue(balance.IsLoaded && double.IsFinite(balance.Result) && balance.Result > 0);

        var otherChain = new RoninOASIS("https://mainnet.optimism.io");
        var rejected = synchronous ? otherChain.GetBalance(request) : await otherChain.GetBalanceAsync(request);
        Assert.IsTrue(rejected.IsError);
        StringAssert.Contains(rejected.Message, "chain ID does not match");
    }
}
