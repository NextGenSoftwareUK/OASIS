using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.OptimismOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class OptimismOASISIntegrationTests
{
    private const int SepoliaChainId = 11155420;

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task PublicWalletUsesOptimismMainnetWithoutStorageActivation(bool synchronous)
    {
        if (Environment.GetEnvironmentVariable("OASIS_OPTIMISM_PUBLIC_WALLET_READ") != "1")
            Assert.Inconclusive("Enable the public Optimism RPC read explicitly.");

        const string rpc = "https://mainnet.optimism.io";
        var request = new GetWeb3WalletBalanceRequest
        {
            WalletAddress = "0x4200000000000000000000000000000000000006"
        };
        var provider = new OptimismOASIS(rpc);
        var result = synchronous ? provider.GetBalance(request) : await provider.GetBalanceAsync(request);
        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(result.IsLoaded && double.IsFinite(result.Result) && result.Result > 0);
        Assert.IsFalse(provider.IsProviderActivated);

        var wrongChain = new OptimismOASIS(rpc, "", "", SepoliaChainId);
        var rejected = synchronous ? wrongChain.GetBalance(request) : await wrongChain.GetBalanceAsync(request);
        Assert.IsTrue(rejected.IsError);
        Assert.IsTrue(rejected.Message.Contains("chain ID", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(wrongChain.IsProviderActivated);
    }

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task SharedStorageUsesDedicatedOptimismSepoliaContract(bool synchronous)
    {
        var rpc = Environment.GetEnvironmentVariable("OASIS_OPTIMISM_CONFORMANCE_RPC_URL");
        if (string.IsNullOrWhiteSpace(rpc))
            Assert.Inconclusive("Configure the dedicated Optimism Sepolia fixture.");

        var key = Environment.GetEnvironmentVariable("OASIS_OPTIMISM_CONFORMANCE_PRIVATE_KEY");
        var contract = Environment.GetEnvironmentVariable("OASIS_OPTIMISM_CONFORMANCE_CONTRACT");
        Assert.IsFalse(string.IsNullOrWhiteSpace(key), "Configure the dedicated test signer.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(contract), "Configure the dedicated Web3Core test contract.");
        Assert.IsTrue(Uri.TryCreate(rpc, UriKind.Absolute, out var endpoint) &&
            endpoint.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(endpoint.UserInfo));

        var readOnly = new Nethereum.Web3.Web3(endpoint!.AbsoluteUri);
        var observed = await readOnly.Eth.ChainId.SendRequestAsync();
        Assert.AreEqual(new BigInteger(SepoliaChainId), observed.Value);

        var provider = new OptimismOASIS(endpoint.AbsoluteUri, key!, contract!, SepoliaChainId);
        var report = await CoreStorageConformance.RunAsync(provider, nameof(OptimismOASIS), "native",
            synchronous, _ => Task.CompletedTask, expectedDefaultDeletion: "absent");
        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
    }
}
