using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.UnitTests
{
    public class SEEDSOASISTests
    {
        [Fact]
        public void Constructor_ShouldInitializeProviderCorrectly()
        {
            // Arrange & Act
            var seedsProvider = new SEEDSOASIS();

            // Assert
            Assert.Equal("SEEDSOASIS", seedsProvider.ProviderName);
            Assert.Equal("SEEDS Provider", seedsProvider.ProviderDescription);
            Assert.Equal(ProviderType.SEEDSOASIS, seedsProvider.ProviderType.Value);
            Assert.Equal(ProviderCategory.StorageAndNetwork, seedsProvider.ProviderCategory.Value);
        }

        [Fact]
        public void ProviderName_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var seedsProvider = new SEEDSOASIS();

            // Assert
            Assert.Equal("SEEDSOASIS", seedsProvider.ProviderName);
        }

        [Fact]
        public void ProviderDescription_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var seedsProvider = new SEEDSOASIS();

            // Assert
            Assert.Equal("SEEDS Provider", seedsProvider.ProviderDescription);
        }

        [Fact]
        public void ProviderType_ShouldBeSEEDSOASIS()
        {
            // Arrange & Act
            var seedsProvider = new SEEDSOASIS();

            // Assert
            Assert.Equal(ProviderType.SEEDSOASIS, seedsProvider.ProviderType.Value);
        }

        [Fact]
        public void ProviderCategory_ShouldBeStorageAndNetwork()
        {
            // Arrange & Act
            var seedsProvider = new SEEDSOASIS();

            // Assert
            Assert.Equal(ProviderCategory.StorageAndNetwork, seedsProvider.ProviderCategory.Value);
        }

        [Fact]
        public async Task ActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var seedsProvider = new SEEDSOASIS();

            // Act
            var result = await seedsProvider.ActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("SEEDS provider activated successfully", result.Message);
        }

        [Fact]
        public async Task DeActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var seedsProvider = new SEEDSOASIS();

            // Act
            var result = await seedsProvider.DeActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("SEEDS provider deactivated successfully", result.Message);
        }

        [Fact]
        public void SEEDSConstants_ShouldBeSetCorrectly()
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
        }
    }
}
