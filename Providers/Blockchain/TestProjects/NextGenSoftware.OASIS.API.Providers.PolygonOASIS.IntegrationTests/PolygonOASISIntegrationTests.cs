using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PolygonOASIS.IntegrationTests;

[TestClass]
public class PolygonOASISIntegrationTests
{
    private const string DevelopmentPrivateKey = "0x0000000000000000000000000000000000000000000000000000000000000001";

    [TestMethod]
    public async Task Activation_ValidatesLivePolygonChainAndContract()
    {
        string? rpcUrl = Environment.GetEnvironmentVariable("OASIS_POLYGON_RPC_URL");
        string? privateKey = Environment.GetEnvironmentVariable("OASIS_POLYGON_PRIVATE_KEY");
        string? contractAddress = Environment.GetEnvironmentVariable("OASIS_POLYGON_CONTRACT_ADDRESS");
        if (string.IsNullOrWhiteSpace(rpcUrl) || string.IsNullOrWhiteSpace(privateKey) || string.IsNullOrWhiteSpace(contractAddress))
            Assert.Inconclusive("Set OASIS_POLYGON_RPC_URL, OASIS_POLYGON_PRIVATE_KEY and OASIS_POLYGON_CONTRACT_ADDRESS to run the real-runtime test.");

        var provider = new PolygonOASIS(rpcUrl!, privateKey!, contractAddress!);
        var result = await provider.ActivateProviderAsync();

        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(result.Result);
        Assert.IsTrue(provider.IsProviderActivated);
        Assert.IsTrue((await provider.DeActivateProviderAsync()).Result);
    }

    [TestMethod]
    public async Task Activation_RejectsUnreachableRpc()
    {
        var provider = new PolygonOASIS("http://127.0.0.1:1", DevelopmentPrivateKey,
            "0x0000000000000000000000000000000000000000");
        var result = await provider.ActivateProviderAsync();

        Assert.IsTrue(result.IsError);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
