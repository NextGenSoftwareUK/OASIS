using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.PLANOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PLANOASIS.IntegrationTests
{
    public class PLANOASISIntegrationTests
    {
        [Fact]
        public async Task FullProviderLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var planProvider = new PLANOASIS();

            // Act & Assert - Activation
            var activationResult = await planProvider.ActivateProviderAsync();
            Assert.False(activationResult.IsError);
            Assert.True(activationResult.Result);
            Assert.Contains("PLAN provider activated successfully", activationResult.Message);

            // Act & Assert - Deactivation
            var deactivationResult = await planProvider.DeActivateProviderAsync();
            Assert.False(deactivationResult.IsError);
            Assert.True(deactivationResult.Result);
            Assert.Contains("PLAN provider deactivated successfully", deactivationResult.Message);
        }

        [Fact]
        public async Task MultipleActivationDeactivationCycles_ShouldWorkCorrectly()
        {
            // Arrange
            var planProvider = new PLANOASIS();

            // Act & Assert - Multiple cycles
            for (int i = 0; i < 3; i++)
            {
                var activationResult = await planProvider.ActivateProviderAsync();
                Assert.False(activationResult.IsError);
                Assert.True(activationResult.Result);

                var deactivationResult = await planProvider.DeActivateProviderAsync();
                Assert.False(deactivationResult.IsError);
                Assert.True(deactivationResult.Result);
            }
        }

        [Fact]
        public async Task AvatarLoading_ShouldHandleDifferentIds()
        {
            // Arrange
            var planProvider = new PLANOASIS();
            var testIds = new[]
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            // Act & Assert
            foreach (var id in testIds)
            {
                var result = await planProvider.LoadAvatarAsync(id);
                Assert.False(result.IsError);
                Assert.NotNull(result.Result);
                Assert.Equal(id, result.Result.Id);
                Assert.Contains("plan_user", result.Result.Username);
            }
        }

        [Fact]
        public void ProviderProperties_ShouldBeConsistent()
        {
            // Arrange
            var planProvider = new PLANOASIS();

            // Act & Assert
            Assert.Equal("PLANOASIS", planProvider.ProviderName);
            Assert.Equal("PLAN Provider", planProvider.ProviderDescription);
            Assert.NotNull(planProvider.ProviderType);
            Assert.NotNull(planProvider.ProviderCategory);
        }
    }
}
