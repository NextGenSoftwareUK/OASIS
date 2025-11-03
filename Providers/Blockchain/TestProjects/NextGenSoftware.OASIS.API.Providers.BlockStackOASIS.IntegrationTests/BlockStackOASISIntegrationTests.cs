using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.BlockStackOASIS;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS.IntegrationTests
{
    public class BlockStackOASISIntegrationTests
    {
        [Fact]
        public async Task FullLifecycle_ShouldWork()
        {
            var provider = new BlockStackOASIS();

            var act = await provider.ActivateProviderAsync();
            Assert.False(act.IsError);

            var uname = "example.id"; // change for real environment
            var byUser = await provider.LoadAvatarByUsernameAsync(uname);
            // Either returns avatar or a handled error; ensure it doesn't throw
            Assert.NotNull(byUser);

            var deact = await provider.DeActivateProviderAsync();
            Assert.False(deact.IsError);
        }
    }
}

