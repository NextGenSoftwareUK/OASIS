using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass]
public sealed class PolkadotPublicWalletBalanceTests
{
    [TestMethod]
    public async Task SynchronousWalletBalanceReadsFinalizedAssetHubWithoutCallerPosts()
    {
        if (Environment.GetEnvironmentVariable("OASIS_POLKADOT_PUBLIC_WALLET_READ") != "1")
            Assert.Inconclusive("Explicitly enable the read-only public Polkadot wallet check.");

        const string genesis = "0x68d56f15f85d3136970ec16946040bc1752654e906147f7e43e9d539d7c3de2f";
        using var provider = new PolkadotOASIS("https://polkadot-asset-hub-rpc.polkadot.io", genesis,
            privateKey: "", bridgePoolAddress: null, storage: null, nft: null, tokenEscrow: null);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);
        Assert.AreEqual(genesis, provider.ReportedGenesisHash);

        var request = new GetWeb3WalletBalanceRequest
        {
            // Treasury pallet account, encoded as the native AccountId32.
            WalletAddress = "0x6d6f646c70792f74727372790000000000000000000000000000000000000000"
        };
        var context = new ObservedContext();
        var previous = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            var result = provider.GetBalance(request);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsTrue(result.Result > 0d);
            Assert.AreEqual(0, context.PostCount);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private sealed class ObservedContext : SynchronizationContext
    {
        private int _posts;

        public int PostCount => Volatile.Read(ref _posts);

        public override void Post(SendOrPostCallback callback, object? state)
        {
            Interlocked.Increment(ref _posts);
            base.Post(callback, state);
        }
    }
}
