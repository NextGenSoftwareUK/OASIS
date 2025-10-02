using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests
{
    public class TRONOASISTests
    {
        [Fact]
        public void Constructor_ShouldInitializeProviderCorrectly()
        {
            // Arrange & Act
            var tronProvider = new TRONOASIS();

            // Assert
            Assert.Equal("TRONOASIS", tronProvider.ProviderName);
            Assert.Equal("TRON Provider", tronProvider.ProviderDescription);
            Assert.Equal(ProviderType.TRONOASIS, tronProvider.ProviderType.Value);
            Assert.Equal(ProviderCategory.StorageAndNetwork, tronProvider.ProviderCategory.Value);
        }

        [Fact]
        public void ProviderName_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var tronProvider = new TRONOASIS();

            // Assert
            Assert.Equal("TRONOASIS", tronProvider.ProviderName);
        }

        [Fact]
        public void ProviderDescription_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var tronProvider = new TRONOASIS();

            // Assert
            Assert.Equal("TRON Provider", tronProvider.ProviderDescription);
        }

        [Fact]
        public void ProviderType_ShouldBeTRONOASIS()
        {
            // Arrange & Act
            var tronProvider = new TRONOASIS();

            // Assert
            Assert.Equal(ProviderType.TRONOASIS, tronProvider.ProviderType.Value);
        }

        [Fact]
        public void ProviderCategory_ShouldBeStorageAndNetwork()
        {
            // Arrange & Act
            var tronProvider = new TRONOASIS();

            // Assert
            Assert.Equal(ProviderCategory.StorageAndNetwork, tronProvider.ProviderCategory.Value);
        }

        [Fact]
        public async Task ActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var tronProvider = new TRONOASIS();

            // Act
            var result = await tronProvider.ActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("TRON provider activated successfully", result.Message);
        }

        [Fact]
        public async Task DeActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var tronProvider = new TRONOASIS();

            // Act
            var result = await tronProvider.DeActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("TRON provider deactivated successfully", result.Message);
        }

        [Fact]
        public void LoadOnChainNFTData_ShouldReturnTRONNFT()
        {
            // Arrange
            var tronProvider = new TRONOASIS();
            var tokenAddress = "TTestTokenAddress123";

            // Act
            var result = tronProvider.LoadOnChainNFTData(tokenAddress);

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
            Assert.Equal(tokenAddress, result.Result.TokenId);
            Assert.Contains("TRON NFT", result.Result.Name);
            Assert.Contains("TRON blockchain", result.Result.Description);
        }
    }
}
