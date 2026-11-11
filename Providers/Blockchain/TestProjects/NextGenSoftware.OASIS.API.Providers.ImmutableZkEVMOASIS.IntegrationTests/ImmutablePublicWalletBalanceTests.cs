using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.ImmutableZkEVMOASIS.IntegrationTests;

[TestClass]
public sealed class ImmutablePublicWalletBalanceTests
{
    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task PublicWalletBalanceRequiresImmutableMainnet(bool synchronous)
    {
        if (Environment.GetEnvironmentVariable("OASIS_IMMUTABLE_PUBLIC_WALLET_READ") != "1")
            Assert.Inconclusive("Enable the public Immutable mainnet balance check explicitly.");

        var request = new GetWeb3WalletBalanceRequest
        {
            WalletAddress = "0x0000000000000000000000000000000000000000"
        };

        var provider = new ImmutableZkEVMOASIS();
        var balance = synchronous ? provider.GetBalance(request) : await provider.GetBalanceAsync(request);
        Assert.IsFalse(balance.IsError, balance.Message);
        Assert.IsTrue(balance.IsLoaded && double.IsFinite(balance.Result) && balance.Result > 0);

        Assert.IsFalse(provider.IsProviderActivated);

        var otherChain = new ImmutableZkEVMOASIS("https://mainnet.optimism.io");
        var rejected = synchronous ? otherChain.GetBalance(request) : await otherChain.GetBalanceAsync(request);
        Assert.IsTrue(rejected.IsError);
        StringAssert.Contains(rejected.Message, "chain ID does not match");
    }
}
