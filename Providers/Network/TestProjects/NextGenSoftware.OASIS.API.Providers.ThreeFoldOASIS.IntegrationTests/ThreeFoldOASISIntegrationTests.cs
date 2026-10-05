using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS.IntegrationTests
{
    public class ThreeFoldOASISIntegrationTests
    {
        [Fact]
        public async Task FullProviderLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var threeFoldProvider = new ThreeFoldOASIS("https://grid.threefold.io");

            // Act & Assert - Activation
            var activationResult = await threeFoldProvider.ActivateProviderAsync();
            Assert.False(activationResult.IsError);
            Assert.True(activationResult.Result);
            Assert.Contains("ThreeFold provider activated successfully", activationResult.Message);

            // Act & Assert - Deactivation
            var deactivationResult = await threeFoldProvider.DeActivateProviderAsync();
            Assert.False(deactivationResult.IsError);
            Assert.True(deactivationResult.Result);
            Assert.Contains("ThreeFold provider deactivated successfully", deactivationResult.Message);
        }

        [Fact]
        public async Task MultipleActivationDeactivationCycles_ShouldWorkCorrectly()
        {
            // Arrange
            var threeFoldProvider = new ThreeFoldOASIS("https://grid.threefold.io");

            // Act & Assert - Multiple cycles
            for (int i = 0; i < 3; i++)
            {
                var activationResult = await threeFoldProvider.ActivateProviderAsync();
                Assert.False(activationResult.IsError);
                Assert.True(activationResult.Result);

                var deactivationResult = await threeFoldProvider.DeActivateProviderAsync();
                Assert.False(deactivationResult.IsError);
                Assert.True(deactivationResult.Result);
            }
        }

        [Fact]
        public void ProviderWallets_ShouldHandleDifferentIds()
        {
            // Arrange
            var threeFoldProvider = new ThreeFoldOASIS("https://grid.threefold.io");
            var testIds = new[]
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Act & Assert
            foreach (var id in testIds)
            {
                var result = threeFoldProvider.LoadProviderWalletsForAvatarById(id);
                Assert.False(result.IsError);
                Assert.NotNull(result.Result);
                Assert.Contains(ProviderType.ThreeFoldOASIS, result.Result.Keys);
            }
        }

        [Fact]
        public async Task AsyncProviderWallets_ShouldWorkCorrectly()
        {
            // Arrange
            var threeFoldProvider = new ThreeFoldOASIS("https://grid.threefold.io");
            var avatarId = Guid.NewGuid();

            // Act
            var result = await threeFoldProvider.LoadProviderWalletsForAvatarByIdAsync(avatarId);

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
            Assert.Contains(ProviderType.ThreeFoldOASIS, result.Result.Keys);
            Assert.Contains("ThreeFold grid", result.Message);
        }

        [Fact]
        public void ProviderProperties_ShouldBeConsistent()
        {
            // Arrange
            var threeFoldProvider = new ThreeFoldOASIS("https://grid.threefold.io");

            // Act & Assert
            Assert.Equal("ThreeFoldOASIS", threeFoldProvider.ProviderName);
            Assert.Equal("ThreeFold Provider", threeFoldProvider.ProviderDescription);
            Assert.NotNull(threeFoldProvider.ProviderType);
            Assert.NotNull(threeFoldProvider.ProviderCategory);
            Assert.Equal("https://grid.threefold.io", threeFoldProvider.HostUri);
        }
    }
}
