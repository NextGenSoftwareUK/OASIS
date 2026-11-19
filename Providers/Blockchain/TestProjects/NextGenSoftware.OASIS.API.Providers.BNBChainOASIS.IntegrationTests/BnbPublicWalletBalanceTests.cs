using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.BNBChainOASIS.IntegrationTests;

public sealed class BnbPublicWalletBalanceTests
{
    [BnbPublicWalletTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadsNativeBalanceWithoutStorageActivation(bool synchronous)
    {
        const string rpc = "https://bsc-dataseed.bnbchain.org";
        var request = new GetWeb3WalletBalanceRequest
        {
            WalletAddress = "0xbb4cdb9cbd36b01bd1cbaebf2de08d9173bc095c"
        };
        IOASISBlockchainStorageProvider provider = new BNBChainOASIS(rpc, "", "", 56);
        var result = synchronous ? provider.GetBalance(request) : await provider.GetBalanceAsync(request);
        Assert.False(result.IsError, result.Message);
        Assert.True(result.IsLoaded && double.IsFinite(result.Result) && result.Result > 0);
        Assert.False(provider.IsProviderActivated);

        IOASISBlockchainStorageProvider wrongChain = new BNBChainOASIS(rpc, "", "", 97);
        var rejected = synchronous ? wrongChain.GetBalance(request) : await wrongChain.GetBalanceAsync(request);
        Assert.True(rejected.IsError);
        Assert.Contains("chain ID", rejected.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(wrongChain.IsProviderActivated);
    }
}

public sealed class BnbPublicWalletTheoryAttribute : TheoryAttribute
{
    public BnbPublicWalletTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("OASIS_BNB_PUBLIC_WALLET_READ") != "1")
            Skip = "Requires explicit opt-in to read the public BNB Smart Chain RPC.";
    }
}
