using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.PolygonOASIS.IntegrationTests;

[TestClass]
public sealed class PolygonPublicWalletBalanceTests
{
    [TestMethod]
    public void PolygonWalletReadsNativePolWithoutActivatingStorage()
    {
        if (Environment.GetEnvironmentVariable("OASIS_POLYGON_PUBLIC_WALLET_READ") != "1")
            Assert.Inconclusive("Explicitly enable the public Polygon PoS wallet check.");

        IOASISBlockchainStorageProvider provider = new PolygonOASIS(
            "https://polygon-bor-rpc.publicnode.com", chainPrivateKey: "", contractAddress: "");
        var request = new GetWeb3WalletBalanceRequest
        {
            // The wrapped native token contract holds its backing native balance.
            WalletAddress = "0x0d500B1d8E8eF31E21C99d1Db9A6444d3ADf1270"
        };

        var result = provider.GetBalance(request);
        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(result.IsLoaded && result.Result > 0d);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
