using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.UnitTests
{
    public class HoloOASISTests
    {
        [Fact]
        public void Constructor_ShouldInitializeProviderCorrectly()
        {
            // Arrange & Act
            var holoProvider = new HoloOASIS();

            // Assert
            Assert.Equal("HoloOASIS", holoProvider.ProviderName);
            Assert.Equal("Holo Provider", holoProvider.ProviderDescription);
            Assert.Equal(ProviderType.HoloOASIS, holoProvider.ProviderType.Value);
            Assert.Equal(ProviderCategory.StorageAndNetwork, holoProvider.ProviderCategory.Value);
        }

        [Fact]
        public void ProviderName_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var holoProvider = new HoloOASIS();

            // Assert
            Assert.Equal("HoloOASIS", holoProvider.ProviderName);
        }

        [Fact]
        public void ProviderDescription_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var holoProvider = new HoloOASIS();

            // Assert
            Assert.Equal("Holo Provider", holoProvider.ProviderDescription);
        }

        [Fact]
        public void ProviderType_ShouldBeHoloOASIS()
        {
            // Arrange & Act
            var holoProvider = new HoloOASIS();

            // Assert
            Assert.Equal(ProviderType.HoloOASIS, holoProvider.ProviderType.Value);
        }

        [Fact]
        public void ProviderCategory_ShouldBeStorageAndNetwork()
        {
            // Arrange & Act
            var holoProvider = new HoloOASIS();

            // Assert
            Assert.Equal(ProviderCategory.StorageAndNetwork, holoProvider.ProviderCategory.Value);
        }

        [Fact]
        public async Task ActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var holoProvider = new HoloOASIS();

            // Act
            var result = await holoProvider.ActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("Holo provider activated successfully", result.Message);
        }

        [Fact]
        public async Task DeActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var holoProvider = new HoloOASIS();

            // Act
            var result = await holoProvider.DeActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("Holo provider deactivated successfully", result.Message);
        }

        [Fact]
        public void LoadOnChainNFTData_ShouldReturnHolochainNFT()
        {
            // Arrange
            var holoProvider = new HoloOASIS();
            var tokenAddress = "HoloTestTokenAddress123";

            // Act
            var result = holoProvider.LoadOnChainNFTData(tokenAddress);

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
            Assert.Equal(tokenAddress, result.Result.TokenId);
            Assert.Contains("Holochain NFT", result.Result.Name);
            Assert.Contains("Holochain DHT", result.Result.Description);
        }
    }
}
