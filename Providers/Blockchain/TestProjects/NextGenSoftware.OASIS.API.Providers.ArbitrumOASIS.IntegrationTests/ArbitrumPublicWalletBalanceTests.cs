using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.IntegrationTests;

[TestClass]
public sealed class ArbitrumPublicWalletBalanceTests
{
    [TestMethod]
    public async Task PublicWalletBalanceChecksChainWithoutActivatingStorage()
    {
        if (Environment.GetEnvironmentVariable("OASIS_ARBITRUM_PUBLIC_WALLET_READ") != "1")
            Assert.Inconclusive("Explicitly enable the public Arbitrum One balance check.");

        var request = new GetWeb3WalletBalanceRequest { WalletAddress = "0x82aF49447D8a07e3bd95BD0d56f35241523fBab1" };
        var provider = new ArbitrumOASIS_Web3Core();
        var context = new ObservedContext();
        var previous = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            var result = provider.GetBalance(request);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsTrue(result.IsLoaded && result.Result > 0d);
            Assert.IsFalse(provider.IsProviderActivated);
            Assert.AreEqual(0, context.PostCount);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        var wrongChain = new ArbitrumOASIS_Web3Core(expectedChainId: 137);
        var rejected = await wrongChain.GetBalanceAsync(request);
        Assert.IsTrue(rejected.IsError);
        StringAssert.Contains(rejected.Message, "chain ID does not match");
        Assert.IsFalse(wrongChain.IsProviderActivated);
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
