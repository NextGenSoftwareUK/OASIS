using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests;

[TestClass]
public sealed class CosmosPublicWalletBalanceTests
{
    [TestMethod]
    public void SynchronousWalletBalanceReadsCosmosHubWithoutPostingToCaller()
    {
        if (Environment.GetEnvironmentVariable("OASIS_COSMOS_PUBLIC_WALLET_READ") != "1")
            Assert.Inconclusive("Explicitly enable the public Cosmos Hub wallet read.");

        var provider = new CosmosBlockChainOASIS("https://cosmos-rpc.publicnode.com");
        var request = new GetWeb3WalletBalanceRequest
        {
            WalletAddress = "cosmos1jv65s3grqf6v6jl3dp4t6c9t9rk99cd88lyufl"
        };
        var context = new ObservedContext();
        var previous = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            var result = provider.GetBalance(request);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsTrue(result.Result > 0d);
            Assert.IsFalse(provider.IsProviderActivated);
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
