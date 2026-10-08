using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.IntegrationTests;

[TestClass]
public class ArbitrumOASISIntegrationTests
{
    private const string DevelopmentPrivateKey = "0x0000000000000000000000000000000000000000000000000000000000000001";

    [TestMethod]
    public async Task Web3CoreActivation_ValidatesLiveChainAndContract()
    {
        string? rpcUrl = Environment.GetEnvironmentVariable("OASIS_ARBITRUM_RPC_URL");
        string? privateKey = Environment.GetEnvironmentVariable("OASIS_ARBITRUM_PRIVATE_KEY");
        string? contractAddress = Environment.GetEnvironmentVariable("OASIS_ARBITRUM_CONTRACT_ADDRESS");
        if (string.IsNullOrWhiteSpace(rpcUrl) || string.IsNullOrWhiteSpace(privateKey) || string.IsNullOrWhiteSpace(contractAddress))
            Assert.Inconclusive("Set the OASIS_ARBITRUM_* variables to run the real-runtime test.");

        var chainId = BigInteger.Parse(Environment.GetEnvironmentVariable("OASIS_ARBITRUM_CHAIN_ID") ?? "42161");
        var provider = new ArbitrumOASIS_Web3Core(rpcUrl!, privateKey!, contractAddress!, chainId);
        var result = await provider.ActivateProviderAsync();

        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(result.Result);
        Assert.IsTrue(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task Web3CoreActivation_RejectsUnreachableRpc()
    {
        var provider = new ArbitrumOASIS_Web3Core("http://127.0.0.1:1", DevelopmentPrivateKey,
            "0x0000000000000000000000000000000000000000", 42161);
        var result = await provider.ActivateProviderAsync();

        Assert.IsTrue(result.IsError);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
