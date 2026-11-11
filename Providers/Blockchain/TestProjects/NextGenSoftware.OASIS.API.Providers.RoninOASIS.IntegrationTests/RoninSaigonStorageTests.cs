using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.RoninOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class RoninSaigonStorageTests
{
    private const int SaigonChainId = 202601;

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task SharedStorageUsesDedicatedSaigonContract(bool synchronous)
    {
        if (Environment.GetEnvironmentVariable("OASIS_RONIN_SAIGON_STORAGE_WRITES") != "1")
            Assert.Inconclusive("Enable writes only for a dedicated Saigon test contract.");

        var rpc = Environment.GetEnvironmentVariable("OASIS_RONIN_CONFORMANCE_RPC_URL");
        var key = Environment.GetEnvironmentVariable("OASIS_RONIN_CONFORMANCE_PRIVATE_KEY");
        var contract = Environment.GetEnvironmentVariable("OASIS_RONIN_CONFORMANCE_CONTRACT");
        Assert.IsTrue(Uri.TryCreate(rpc, UriKind.Absolute, out var endpoint) &&
            endpoint.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(endpoint.UserInfo));
        Assert.IsFalse(string.IsNullOrWhiteSpace(key), "Configure the funded Saigon signer.");
        Assert.IsTrue(!string.IsNullOrWhiteSpace(contract) &&
            AddressUtil.Current.IsValidEthereumAddressHexFormat(contract));

        var reader = new Web3(endpoint!.AbsoluteUri);
        var observedChain = await reader.Eth.ChainId.SendRequestAsync();
        Assert.AreEqual(new BigInteger(SaigonChainId), observedChain.Value);
        var account = new Account(key!, SaigonChainId);
        var balance = await reader.Eth.GetBalance.SendRequestAsync(account.Address);
        Assert.IsTrue(balance.Value > 0, "The Saigon fixture signer needs testnet RON for gas.");

        await EvmStorageFixture.RequireEmptyAsync(reader, contract!);

        var provider = new RoninOASIS(endpoint.AbsoluteUri, key!, contract!, SaigonChainId);
        var report = await CoreStorageConformance.RunAsync(provider, nameof(RoninOASIS), "native",
            synchronous, _ => Task.CompletedTask, expectedDefaultDeletion: "absent");
        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
        await EvmStorageFixture.RequireEmptyAsync(reader, contract!);
    }
}
