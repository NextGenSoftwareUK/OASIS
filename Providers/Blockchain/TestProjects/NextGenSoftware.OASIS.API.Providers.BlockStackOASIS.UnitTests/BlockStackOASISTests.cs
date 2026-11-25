using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.BlockStackOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS.UnitTests
{
    public class BlockStackOASISTests
    {
        [Fact]
        public void Constructor_ShouldInitializeProviderCorrectly()
        {
            var provider = new BlockStackOASIS();
            Assert.Equal("BlockStackOASIS", provider.ProviderName);
            Assert.Equal("BlockStack Provider", provider.ProviderDescription);
            Assert.Equal(ProviderType.BlockStackOASIS, provider.ProviderType.Value);
            Assert.Equal(ProviderCategory.StorageAndNetwork, provider.ProviderCategory.Value);
        }

        [Fact]
        public async Task Activate_Then_Deactivate_ShouldSucceed()
        {
            var provider = new BlockStackOASIS();
            var act = await provider.ActivateProviderAsync();
            Assert.False(act.IsError);
            var deact = await provider.DeActivateProviderAsync();
            Assert.False(deact.IsError);
        }

        [Fact]
        public async Task LoadAvatarByUsername_ShouldGracefullyHandleUnknown()
        {
            var provider = new BlockStackOASIS();
            var res = await provider.LoadAvatarByUsernameAsync("not-a-real-name.id");
            Assert.True(res.IsError); // Expect error since name likely cannot be resolved
        }

        [Fact]
        public async Task LoadAllAvatars_IsRefusedBecauseGaiaHasNoListing()
        {
            var provider = new BlockStackOASIS();
            var res = await provider.LoadAllAvatarsAsync();
            // Gaia hubs cannot enumerate users, so listing all avatars must fail
            // with guidance rather than return a misleading empty set.
            Assert.True(res.IsError);
            Assert.Contains("no user listing", res.Message);
        }
    }
}

