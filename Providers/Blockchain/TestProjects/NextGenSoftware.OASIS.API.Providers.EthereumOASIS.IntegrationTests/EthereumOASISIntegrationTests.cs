using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition;
using CoreHolon = NextGenSoftware.OASIS.API.Core.Holons.Holon;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
public class EthereumOASISIntegrationTests
{
    private const string DefaultHost = "http://127.0.0.1:8545";
    // Ganache's documented deterministic development key. It has no value outside the disposable chain.
    private const string DefaultPrivateKey = "0x4f3edf983ac636a65a842ce7c78d9aa706d3b113bce9c46f30d7d21715b23b1d";
    private static readonly BigInteger DefaultChainId = 31337;

    private static string Host => Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_RPC") ?? DefaultHost;
    private static string PrivateKey => Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY") ?? DefaultPrivateKey;
    private static BigInteger ChainId => BigInteger.Parse(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_CHAIN_ID") ?? DefaultChainId.ToString());

    private static async Task<(EthereumOASIS Provider, string Contract)> CreateActivatedProviderAsync()
    {
        var account = new Account(PrivateKey, ChainId);
        var web3 = new Web3(account, Host);
        var receipt = await NextGenSoftwareOASISService.DeployContractAndWaitForReceiptAsync(web3, new NextGenSoftwareOASISDeployment());
        Assert.IsFalse(receipt.HasErrors(), "The disposable EVM storage contract deployment failed.");

        var provider = new EthereumOASIS(Host, PrivateKey, ChainId, receipt.ContractAddress);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);
        Assert.IsTrue(activation.Result);
        Assert.IsTrue(provider.IsProviderActivated);
        return (provider, receipt.ContractAddress);
    }

    [TestMethod]
    public async Task Activation_ValidatesRpcChainAndDeployedContract()
    {
        var (provider, contract) = await CreateActivatedProviderAsync();
        Assert.IsFalse(string.IsNullOrWhiteSpace(contract));
        Assert.AreEqual(ProviderType.EthereumOASIS, provider.ProviderType.Value);

        var deactivation = await provider.DeActivateProviderAsync();
        Assert.IsFalse(deactivation.IsError, deactivation.Message);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task Holon_SaveLoadRoundTrip_UsesDisposableEvmChain()
    {
        var (provider, _) = await CreateActivatedProviderAsync();
        var holon = new CoreHolon
        {
            Id = Guid.NewGuid(),
            Name = $"ethereum-integration-{Guid.NewGuid():N}",
            Description = "Disposable Ganache round-trip"
        };

        var saved = await provider.SaveHolonAsync(holon);
        Assert.IsFalse(saved.IsError, saved.Message);
        Assert.IsTrue(saved.IsSaved);

        var loaded = await provider.LoadHolonAsync(holon.Id);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.IsNotNull(loaded.Result);
        Assert.AreEqual(holon.Id, loaded.Result.Id);
        Assert.AreEqual(holon.Name, loaded.Result.Name);
    }

    [TestMethod]
    public async Task Activation_RejectsWrongChainId()
    {
        var provider = new EthereumOASIS(Host, PrivateKey, ChainId + 1, "0x0000000000000000000000000000000000000001");

        var activation = await provider.ActivateProviderAsync();
        Assert.IsTrue(activation.IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        StringAssert.Contains(activation.Message, "does not match configured chain ID");
    }
}
