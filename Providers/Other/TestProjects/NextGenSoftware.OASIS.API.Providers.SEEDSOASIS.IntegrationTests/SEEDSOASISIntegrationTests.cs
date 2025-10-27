using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.IntegrationTests
{
    public class SEEDSOASISIntegrationTests
    {
        [Fact]
        public async Task FullProviderLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var seedsProvider = new SEEDSOASIS();

            // Act & Assert - Activation
            var activationResult = await seedsProvider.ActivateProviderAsync();
            Assert.False(activationResult.IsError);
            Assert.True(activationResult.Result);
            Assert.Contains("SEEDS provider activated successfully", activationResult.Message);

            // Act & Assert - Deactivation
            var deactivationResult = await seedsProvider.DeActivateProviderAsync();
            Assert.False(deactivationResult.IsError);
            Assert.True(deactivationResult.Result);
            Assert.Contains("SEEDS provider deactivated successfully", deactivationResult.Message);
        }

        [Fact]
        public async Task MultipleActivationDeactivationCycles_ShouldWorkCorrectly()
        {
            // Arrange
            var seedsProvider = new SEEDSOASIS();

            // Act & Assert - Multiple cycles
            for (int i = 0; i < 3; i++)
            {
                var activationResult = await seedsProvider.ActivateProviderAsync();
                Assert.False(activationResult.IsError);
                Assert.True(activationResult.Result);

                var deactivationResult = await seedsProvider.DeActivateProviderAsync();
                Assert.False(deactivationResult.IsError);
                Assert.True(deactivationResult.Result);
            }
        }

        [Fact]
        public void SEEDSConstants_ShouldBeValid()
        {
            // Arrange & Act
            var seedsProvider = new SEEDSOASIS();

            // Assert
            Assert.NotNull(SEEDSOASIS.ENDPOINT_TEST);
            Assert.NotNull(SEEDSOASIS.ENDPOINT_LIVE);
            Assert.NotNull(SEEDSOASIS.SEEDS_EOSIO_ACCOUNT_TEST);
            Assert.NotNull(SEEDSOASIS.SEEDS_EOSIO_ACCOUNT_LIVE);
            Assert.NotNull(SEEDSOASIS.CHAINID_TEST);
            Assert.NotNull(SEEDSOASIS.CHAINID_LIVE);
            Assert.NotNull(SEEDSOASIS.PUBLICKEY_TEST);
            Assert.NotNull(SEEDSOASIS.PUBLICKEY_LIVE);
            Assert.NotNull(SEEDSOASIS.APIKEY_TEST);
            Assert.NotNull(SEEDSOASIS.APIKEY_LIVE);
        }

        [Fact]
        public void ProviderProperties_ShouldBeConsistent()
        {
            // Arrange
            var seedsProvider = new SEEDSOASIS();

            // Act & Assert
            Assert.Equal("SEEDSOASIS", seedsProvider.ProviderName);
            Assert.Equal("SEEDS Provider", seedsProvider.ProviderDescription);
            Assert.NotNull(seedsProvider.ProviderType);
            Assert.NotNull(seedsProvider.ProviderCategory);
        }

        [Fact]
        public void TelosOASIS_ShouldBeInitialized()
        {
            // Arrange
            var seedsProvider = new SEEDSOASIS();

            // Act & Assert
            Assert.NotNull(seedsProvider.TelosOASIS);
        }
    }
}
