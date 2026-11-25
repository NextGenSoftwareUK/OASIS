using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS.IntegrationTests
{
    public class ChainLinkOASISIntegrationTests
    {
        [Fact]
        public async Task FullProviderLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var chainLinkProvider = new ChainLinkOASIS();

            // Act & Assert - Activation
            var activationResult = await chainLinkProvider.ActivateProviderAsync();
            Assert.False(activationResult.IsError);
            Assert.True(activationResult.Result);
            Assert.Contains("ChainLink provider activated successfully", activationResult.Message);

            // Act & Assert - Deactivation
            var deactivationResult = await chainLinkProvider.DeActivateProviderAsync();
            Assert.False(deactivationResult.IsError);
            Assert.True(deactivationResult.Result);
            Assert.Contains("ChainLink provider deactivated successfully", deactivationResult.Message);
        }

        [Fact]
        public async Task MultipleActivationDeactivationCycles_ShouldWorkCorrectly()
        {
            // Arrange
            var chainLinkProvider = new ChainLinkOASIS();

            // Act & Assert - Multiple cycles
            for (int i = 0; i < 3; i++)
            {
                var activationResult = await chainLinkProvider.ActivateProviderAsync();
                Assert.False(activationResult.IsError);
                Assert.True(activationResult.Result);

                var deactivationResult = await chainLinkProvider.DeActivateProviderAsync();
                Assert.False(deactivationResult.IsError);
                Assert.True(deactivationResult.Result);
            }
        }

        [Fact]
        public async Task NFTDataLoading_IsAnExplicitUnsupportedError()
        {
            // Chainlink exposes no NFT metadata lookup; the provider must say so rather than invent data.
            var chainLinkProvider = new ChainLinkOASIS();
            var sync = chainLinkProvider.LoadOnChainNFTData("0x0000000000000000000000000000000000000001");
            var async_ = await chainLinkProvider.LoadOnChainNFTDataAsync("0x0000000000000000000000000000000000000001");
            Assert.True(sync.IsError);
            Assert.Null(sync.Result);
            Assert.True(async_.IsError);
            Assert.Null(async_.Result);
        }

        [Fact]
        public async Task RecordStorage_FailsExplicitlyBecauseChainlinkHasNoRecordStore()
        {
            var chainLinkProvider = new ChainLinkOASIS();
            var avatar = await chainLinkProvider.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = Guid.NewGuid(), Username = "u" });
            var holon = await chainLinkProvider.SaveHolonAsync(new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.NewGuid() });
            var load = await chainLinkProvider.LoadAvatarAsync(Guid.NewGuid());
            var delete = chainLinkProvider.DeleteAvatar(Guid.NewGuid());
            foreach (var (isError, saved, message) in new[] { (avatar.IsError, avatar.IsSaved, avatar.Message), (holon.IsError, holon.IsSaved, holon.Message),
                         (load.IsError, false, load.Message), (delete.IsError, false, delete.Message) })
            {
                Assert.True(isError);
                Assert.False(saved);
                Assert.Contains("no OASIS record store", message);
            }
        }

        [Fact]
        public void ProviderProperties_ShouldBeConsistent()
        {
            // Arrange
            var chainLinkProvider = new ChainLinkOASIS();

            // Act & Assert
            Assert.Equal("ChainLinkOASIS", chainLinkProvider.ProviderName);
            Assert.Equal("ChainLink Provider", chainLinkProvider.ProviderDescription);
            Assert.NotNull(chainLinkProvider.ProviderType);
            Assert.NotNull(chainLinkProvider.ProviderCategory);
        }
    }
}
