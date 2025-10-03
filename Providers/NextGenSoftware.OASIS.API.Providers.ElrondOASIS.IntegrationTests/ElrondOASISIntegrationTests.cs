using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.ElrondOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS.IntegrationTests
{
    public class ElrondOASISIntegrationTests
    {
        [Fact]
        public async Task FullProviderLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var elrondProvider = new ElrondOASIS();

            // Act & Assert - Activation
            var activationResult = await elrondProvider.ActivateProviderAsync();
            Assert.False(activationResult.IsError);
            Assert.True(activationResult.Result);
            Assert.Contains("Elrond provider activated successfully", activationResult.Message);

            // Act & Assert - Deactivation
            var deactivationResult = await elrondProvider.DeActivateProviderAsync();
            Assert.False(deactivationResult.IsError);
            Assert.True(deactivationResult.Result);
            Assert.Contains("Elrond provider deactivated successfully", deactivationResult.Message);
        }

        [Fact]
        public async Task MultipleActivationDeactivationCycles_ShouldWorkCorrectly()
        {
            // Arrange
            var elrondProvider = new ElrondOASIS();

            // Act & Assert - Multiple cycles
            for (int i = 0; i < 3; i++)
            {
                var activationResult = await elrondProvider.ActivateProviderAsync();
                Assert.False(activationResult.IsError);
                Assert.True(activationResult.Result);

                var deactivationResult = await elrondProvider.DeActivateProviderAsync();
                Assert.False(deactivationResult.IsError);
                Assert.True(deactivationResult.Result);
            }
        }

        [Fact]
        public async Task AvatarLoading_ShouldHandleDifferentIds()
        {
            // Arrange
            var elrondProvider = new ElrondOASIS();
            var testIds = new[]
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Act & Assert
            foreach (var id in testIds)
            {
                var result = await elrondProvider.LoadAvatarAsync(id);
                Assert.False(result.IsError);
                Assert.NotNull(result.Result);
                Assert.Equal(id, result.Result.Id);
                Assert.Contains("elrond_user", result.Result.Username);
            }
        }

        [Fact]
        public void ProviderProperties_ShouldBeConsistent()
        {
            // Arrange
            var elrondProvider = new ElrondOASIS();

            // Act & Assert
            Assert.Equal("ElrondOASIS", elrondProvider.ProviderName);
            Assert.Equal("Elrond Provider", elrondProvider.ProviderDescription);
            Assert.NotNull(elrondProvider.ProviderType);
            Assert.NotNull(elrondProvider.ProviderCategory);
        }
    }
}
