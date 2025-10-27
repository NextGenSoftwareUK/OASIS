using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS.UnitTests
{
    public class ChainLinkOASISTests
    {
        [Fact]
        public void Constructor_ShouldInitializeProviderCorrectly()
        {
            // Arrange & Act
            var chainLinkProvider = new ChainLinkOASIS();

            // Assert
            Assert.Equal("ChainLinkOASIS", chainLinkProvider.ProviderName);
            Assert.Equal("ChainLink Provider", chainLinkProvider.ProviderDescription);
            Assert.Equal(ProviderType.ChainLinkOASIS, chainLinkProvider.ProviderType.Value);
            Assert.Equal(ProviderCategory.StorageAndNetwork, chainLinkProvider.ProviderCategory.Value);
        }

        [Fact]
        public void ProviderName_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var chainLinkProvider = new ChainLinkOASIS();

            // Assert
            Assert.Equal("ChainLinkOASIS", chainLinkProvider.ProviderName);
        }

        [Fact]
        public void ProviderDescription_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var chainLinkProvider = new ChainLinkOASIS();

            // Assert
            Assert.Equal("ChainLink Provider", chainLinkProvider.ProviderDescription);
        }

        [Fact]
        public void ProviderType_ShouldBeChainLinkOASIS()
        {
            // Arrange & Act
            var chainLinkProvider = new ChainLinkOASIS();

            // Assert
            Assert.Equal(ProviderType.ChainLinkOASIS, chainLinkProvider.ProviderType.Value);
        }

        [Fact]
        public void ProviderCategory_ShouldBeStorageAndNetwork()
        {
            // Arrange & Act
            var chainLinkProvider = new ChainLinkOASIS();

            // Assert
            Assert.Equal(ProviderCategory.StorageAndNetwork, chainLinkProvider.ProviderCategory.Value);
        }

        [Fact]
        public async Task ActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var chainLinkProvider = new ChainLinkOASIS();

            // Act
            var result = await chainLinkProvider.ActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("ChainLink provider activated successfully", result.Message);
        }

        [Fact]
        public async Task DeActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var chainLinkProvider = new ChainLinkOASIS();

            // Act
            var result = await chainLinkProvider.DeActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("ChainLink provider deactivated successfully", result.Message);
        }

        [Fact]
        public void LoadOnChainNFTData_ShouldReturnChainLinkNFT()
        {
            // Arrange
            var chainLinkProvider = new ChainLinkOASIS();
            var tokenAddress = "ChainLinkTestTokenAddress123";

            // Act
            var result = chainLinkProvider.LoadOnChainNFTData(tokenAddress);

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
            Assert.Equal(tokenAddress, result.Result.TokenId);
            Assert.Contains("ChainLink NFT", result.Result.Name);
            Assert.Contains("ChainLink oracle", result.Result.Description);
        }
    }
}
