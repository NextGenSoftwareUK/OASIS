using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaMirrorBalanceTests
{
    [DataTestMethod]
    [DataRow("https://mainnet-public.mirrornode.hedera.com")]
    [DataRow("https://mainnet-public.mirrornode.hedera.com/api/v1")]
    public async Task PublicSystemAccountBalanceUsesRealMirrorResponse(string endpoint)
    {
        // Isolate the read operation; this is not evidence of SDK activation or signing.
        var provider = new HashgraphOASIS(endpoint) { IsProviderActivated = true };
        var result = await provider.GetBalanceAsync(new GetWeb3WalletBalanceRequest
        {
            WalletAddress = "0.0.98"
        }).WaitAsync(TimeSpan.FromSeconds(30));
        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(double.IsFinite(result.Result));
        Assert.IsTrue(result.Result > 0, "The public Hedera system account must have a real positive balance.");
    }
}
