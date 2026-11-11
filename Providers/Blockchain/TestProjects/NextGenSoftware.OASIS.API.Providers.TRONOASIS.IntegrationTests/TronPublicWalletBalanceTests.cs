using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.IntegrationTests;

public sealed class TronPublicWalletBalanceTests
{
    [PublicTronRead]
    public async Task SynchronousWalletBalanceReadsSolidifiedTrxWithoutCallerPosts()
    {
        var provider = new TRONOASIS(
            fullNodeGrpcEndpoint: "https://tron-grpc.publicnode.com:443",
            solidityNodeGrpcEndpoint: "https://tron-solidity-grpc.publicnode.com:443",
            genesisBlockId: TronPublicHeaderMetadataSource.PublicGenesis);
        var request = new GetWeb3WalletBalanceRequest { WalletAddress = "T9yD14Nj9j7xAB4dbGeiX9h8unkKHxuWwb" };
        var context = new ObservedContext();
        var previous = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(context);
            var result = provider.GetBalance(request);
            Assert.False(result.IsError, result.Message);
            Assert.True(result.Result > 0d);
            Assert.False(provider.IsProviderActivated);
            Assert.Equal(0, context.PostCount);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
            var deactivation = await provider.DeActivateProviderAsync();
            Assert.False(deactivation.IsError, deactivation.Message);
        }
    }

    private sealed class PublicTronReadAttribute : FactAttribute
    {
        public PublicTronReadAttribute()
        {
            if (Environment.GetEnvironmentVariable("OASIS_TRON_PUBLIC_WALLET_READ") != "1")
                Skip = "Explicitly enable the read-only public TRON wallet check.";
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
