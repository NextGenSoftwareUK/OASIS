using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition;
using CoreHolon = NextGenSoftware.OASIS.API.Core.Holons.Holon;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using SQLiteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

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

    [DataTestMethod]
    [DataRow(false,false)]
    [DataRow(true,false)]
    [DataRow(false,true)]
    [DataRow(true,true)]
    public async Task BulkNativeRejectionRetainsCommittedEvidence(bool synchronous, bool throughV2)
    {
        var fixturePath = Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON")
            ?? throw new InvalidOperationException("Compile the retained local rejection fixture first.");
        using var fixture = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(fixturePath));
        var account = new Account(PrivateKey,ChainId);
        var web3 = new Web3(account,Host);
        var deployed = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(
            fixture.RootElement.GetProperty("abi").GetRawText(),fixture.RootElement.GetProperty("bytecode").GetString(),
            account.Address,new Nethereum.Hex.HexTypes.HexBigInteger(4000000));
        Assert.AreEqual(BigInteger.One,deployed.Status.Value);
        var provider = new EthereumOASIS(Host,PrivateKey,ChainId,deployed.ContractAddress);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var first = new CoreHolon { Id = Guid.NewGuid(), Name = "owned committed first holon" };
        var second = new CoreHolon { Id = Guid.NewGuid(), Name = "owned rejected second holon" };
        var batch = new[] { first,second };
        SQLiteProvider? backup = null;
        var backupPath = Path.Combine(Path.GetDirectoryName(fixturePath)!,"v2-backup.db");
        try
        {
        OASISResult<IEnumerable<IHolon>> result;
        ProviderManager? manager = null;
        if (throughV2)
        {
            if (File.Exists(backupPath)) throw new InvalidOperationException("Existing backup fixture requires inspection; refusing overwrite.");
            backup = new SQLiteProvider($"Data Source={backupPath};Pooling=False");
            Assert.IsFalse((await backup.ActivateProviderAsync()).IsError);
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            manager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = true };
            manager.RegisterProvider(provider);
            manager.RegisterProvider(backup);
            var stores = new[] { new EnumValue<ProviderType>(ProviderType.EthereumOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(stores).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(stores).IsError);
            var router = new OASISHyperDrive(manager);
            var request = new StorageOperationRequest { Operation = "SaveHolons", Payload = batch,
                PreferredProvider = ProviderType.EthereumOASIS, SaveChildren = false };
            result = synchronous ? router.RouteRequest<IEnumerable<IHolon>>(request) : await router.RouteRequestAsync<IEnumerable<IHolon>>(request);
        }
        else result = synchronous ? provider.SaveHolons(batch,saveChildren:false) : await provider.SaveHolonsAsync(batch,saveChildren:false);
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.IsSaved);
        Assert.IsInstanceOfType(result.Exception,typeof(EthereumPartialStorageMutationException));
        var evidence = (EthereumPartialStorageMutationException)result.Exception;
        Assert.AreEqual(1,evidence.CommittedTransactions.Count);
        Assert.IsNotNull(evidence.InnerException);
        var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(evidence.OriginalTransactionId);
        Assert.AreEqual(BigInteger.One,receipt.Status.Value);
        var firstRead = await provider.LoadHolonAsync(first.Id);
        Assert.IsFalse(firstRead.IsError,firstRead.Message);
        Assert.AreEqual(first.Name,firstRead.Result.Name);
        var rejectedRead = await provider.LoadHolonAsync(second.Id);
        Assert.IsNull(rejectedRead.Result);
        if (throughV2)
        {
            Assert.AreEqual(0L,manager!.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            Assert.AreEqual(1L,manager.PerformanceMonitor.GetMetrics(ProviderType.EthereumOASIS).TotalRequests);
            foreach (var node in batch) Assert.IsNull((await backup!.LoadHolonAsync(node.Id,loadChildren:false)).Result);
        }
        }
        finally
        {
            if (backup != null)
            {
                backup.Dispose();
                File.Delete(backupPath);
            }
        }
        await provider.DeActivateProviderAsync();
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
