using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.ImmutableZkEVMOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class ImmutableZkEVMTestnetStorageTests
{
    private const int TestnetChainId = 13473;

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task SharedStorageUsesDedicatedImmutableTestnetContract(bool synchronous)
    {
        if (Environment.GetEnvironmentVariable("OASIS_IMMUTABLE_TESTNET_STORAGE_WRITES") != "1")
            Assert.Inconclusive("Enable writes only for a dedicated Immutable testnet test contract.");

        var rpc = Environment.GetEnvironmentVariable("OASIS_IMMUTABLE_CONFORMANCE_RPC_URL");
        var key = Environment.GetEnvironmentVariable("OASIS_IMMUTABLE_CONFORMANCE_PRIVATE_KEY");
        var contract = Environment.GetEnvironmentVariable("OASIS_IMMUTABLE_CONFORMANCE_CONTRACT");
        Assert.IsTrue(Uri.TryCreate(rpc, UriKind.Absolute, out var endpoint) &&
            endpoint.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(endpoint.UserInfo));
        Assert.IsFalse(string.IsNullOrWhiteSpace(key), "Configure the funded Immutable testnet signer.");
        Assert.IsTrue(!string.IsNullOrWhiteSpace(contract) &&
            AddressUtil.Current.IsValidEthereumAddressHexFormat(contract));

        var reader = new Web3(endpoint!.AbsoluteUri);
        var observedChain = await reader.Eth.ChainId.SendRequestAsync();
        Assert.AreEqual(new BigInteger(TestnetChainId), observedChain.Value);
        var account = new Account(key!, TestnetChainId);
        var balance = await reader.Eth.GetBalance.SendRequestAsync(account.Address);
        Assert.IsTrue(balance.Value > 0, "The Immutable testnet fixture signer needs testnet IMX for gas.");

        var provider = new ImmutableZkEVMOASIS(endpoint.AbsoluteUri, key!, contract!, TestnetChainId);
        var report = await CoreStorageConformance.RunAsync(provider, nameof(ImmutableZkEVMOASIS), "native",
            synchronous, _ => Task.CompletedTask, expectedDefaultDeletion: "absent");
        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
    }
}
