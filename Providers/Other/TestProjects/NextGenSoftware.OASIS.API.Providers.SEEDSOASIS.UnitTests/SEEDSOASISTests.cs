using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.UnitTests
{
    public class SEEDSOASISTests
    {
        // SEEDS runs on Telos. Point Telos at a closed local port so no test
        // touches a real chain.
        private static SEEDSOASIS NewProvider() =>
            new SEEDSOASIS(new TelosOASIS.TelosOASIS("http://127.0.0.1:1", "testaccount", "testchain", "testkey"));

        [Fact]
        public void Constructor_ShouldInitializeProviderCorrectly()
        {
            // Arrange & Act
            var seedsProvider = NewProvider();

            // Assert
            Assert.Equal("SEEDSOASIS", seedsProvider.ProviderName);
            Assert.Equal("SEEDS Provider", seedsProvider.ProviderDescription);
            Assert.Equal(ProviderType.SEEDSOASIS, seedsProvider.ProviderType.Value);
            Assert.Equal(ProviderCategory.Application, seedsProvider.ProviderCategory.Value);
        }

        [Fact]
        public void ProviderName_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var seedsProvider = NewProvider();

            // Assert
            Assert.Equal("SEEDSOASIS", seedsProvider.ProviderName);
        }

        [Fact]
        public void ProviderDescription_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var seedsProvider = NewProvider();

            // Assert
            Assert.Equal("SEEDS Provider", seedsProvider.ProviderDescription);
        }

        [Fact]
        public void ProviderType_ShouldBeSEEDSOASIS()
        {
            // Arrange & Act
            var seedsProvider = NewProvider();

            // Assert
            Assert.Equal(ProviderType.SEEDSOASIS, seedsProvider.ProviderType.Value);
        }

        [Fact]
        public void ProviderCategory_ShouldBeStorageAndNetwork()
        {
            // Arrange & Act
            var seedsProvider = NewProvider();

            // Assert
            Assert.Equal(ProviderCategory.Application, seedsProvider.ProviderCategory.Value);
        }

        [Fact]
        public async Task ActivateProviderAsync_WhenTelosUnreachable_FailsClearly()
        {
            // Arrange
            var seedsProvider = NewProvider();

            // Act
            var result = await seedsProvider.ActivateProviderAsync();

            // Assert: SEEDS must not report itself active without its Telos transport.
            Assert.True(result.IsError);
            Assert.False(result.Result);
            Assert.False(seedsProvider.IsProviderActivated);
            Assert.Contains("could not activate its Telos storage transport", result.Message);
        }

        [Fact]
        public async Task DeActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var seedsProvider = NewProvider();

            // Act
            var result = await seedsProvider.DeActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.False(seedsProvider.IsProviderActivated);
        }

        [Fact]
        public void SEEDSConstants_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var seedsProvider = NewProvider();

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
