using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS.IntegrationTests
{
    public class ScuttlebuttOASISIntegrationTests
    {
        [Fact]
        public async Task FullProviderLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var scuttlebuttProvider = new ScuttlebuttOASIS();

            // Act & Assert - Activation
            var activationResult = await scuttlebuttProvider.ActivateProviderAsync();
            Assert.False(activationResult.IsError);
            Assert.True(activationResult.Result);
            Assert.Contains("Scuttlebutt provider activated successfully", activationResult.Message);

            // Act & Assert - Deactivation
            var deactivationResult = await scuttlebuttProvider.DeActivateProviderAsync();
            Assert.False(deactivationResult.IsError);
            Assert.True(deactivationResult.Result);
            Assert.Contains("Scuttlebutt provider deactivated successfully", deactivationResult.Message);
        }

        [Fact]
        public async Task MultipleActivationDeactivationCycles_ShouldWorkCorrectly()
        {
            // Arrange
            var scuttlebuttProvider = new ScuttlebuttOASIS();

            // Act & Assert - Multiple cycles
            for (int i = 0; i < 3; i++)
            {
                var activationResult = await scuttlebuttProvider.ActivateProviderAsync();
                Assert.False(activationResult.IsError);
                Assert.True(activationResult.Result);

                var deactivationResult = await scuttlebuttProvider.DeActivateProviderAsync();
                Assert.False(deactivationResult.IsError);
                Assert.True(deactivationResult.Result);
            }
        }

        [Fact]
        public async Task AvatarLoading_ShouldHandleDifferentIds()
        {
            // Arrange
            var scuttlebuttProvider = new ScuttlebuttOASIS();
            var testIds = new[]
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Act & Assert
            foreach (var id in testIds)
            {
                var result = await scuttlebuttProvider.LoadAvatarAsync(id);
                Assert.False(result.IsError);
                Assert.NotNull(result.Result);
                Assert.Equal(id, result.Result.Id);
                Assert.Contains("scuttlebutt_user", result.Result.Username);
            }
        }

        [Fact]
        public void ProviderProperties_ShouldBeConsistent()
        {
            // Arrange
            var scuttlebuttProvider = new ScuttlebuttOASIS();

            // Act & Assert
            Assert.Equal("ScuttlebuttOASIS", scuttlebuttProvider.ProviderName);
            Assert.Equal("Scuttlebutt Provider", scuttlebuttProvider.ProviderDescription);
            Assert.NotNull(scuttlebuttProvider.ProviderType);
            Assert.NotNull(scuttlebuttProvider.ProviderCategory);
        }
    }
}
