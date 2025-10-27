using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS.IntegrationTests
{
    public class ChainLinkOASISIntegrationTests
    {
        [Fact]
        public async Task FullProviderLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var chainLinkProvider = new ChainLinkOASIS();

            // Act & Assert - Activation
            var activationResult = await chainLinkProvider.ActivateProviderAsync();
            Assert.False(activationResult.IsError);
            Assert.True(activationResult.Result);
            Assert.Contains("ChainLink provider activated successfully", activationResult.Message);

            // Act & Assert - Deactivation
            var deactivationResult = await chainLinkProvider.DeActivateProviderAsync();
            Assert.False(deactivationResult.IsError);
            Assert.True(deactivationResult.Result);
            Assert.Contains("ChainLink provider deactivated successfully", deactivationResult.Message);
        }

        [Fact]
        public async Task MultipleActivationDeactivationCycles_ShouldWorkCorrectly()
        {
            // Arrange
            var chainLinkProvider = new ChainLinkOASIS();

            // Act & Assert - Multiple cycles
            for (int i = 0; i < 3; i++)
            {
                var activationResult = await chainLinkProvider.ActivateProviderAsync();
                Assert.False(activationResult.IsError);
                Assert.True(activationResult.Result);

                var deactivationResult = await chainLinkProvider.DeActivateProviderAsync();
                Assert.False(deactivationResult.IsError);
                Assert.True(deactivationResult.Result);
            }
        }

        [Fact]
        public void NFTDataLoading_ShouldHandleDifferentTokenAddresses()
        {
            // Arrange
            var chainLinkProvider = new ChainLinkOASIS();
            var testAddresses = new[]
            {
                "ChainLinkTestTokenAddress123",
                "ChainLinkAnotherTokenAddress456",
                "ChainLinkThirdTokenAddress789"
            };

            // Act & Assert
            foreach (var address in testAddresses)
            {
                var result = chainLinkProvider.LoadOnChainNFTData(address);
                Assert.False(result.IsError);
                Assert.NotNull(result.Result);
                Assert.Equal(address, result.Result.TokenId);
                Assert.Contains("ChainLink NFT", result.Result.Name);
            }
        }

        [Fact]
        public async Task AsyncNFTDataLoading_ShouldWorkCorrectly()
        {
            // Arrange
            var chainLinkProvider = new ChainLinkOASIS();
            var tokenAddress = "ChainLinkAsyncTestTokenAddress123";

            // Act
            var result = await chainLinkProvider.LoadOnChainNFTDataAsync(tokenAddress);

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
            Assert.Equal(tokenAddress, result.Result.TokenId);
            Assert.Contains("ChainLink NFT", result.Result.Name);
            Assert.Contains("ChainLink oracle", result.Result.Description);
        }

        [Fact]
        public void ProviderProperties_ShouldBeConsistent()
        {
            // Arrange
            var chainLinkProvider = new ChainLinkOASIS();

            // Act & Assert
            Assert.Equal("ChainLinkOASIS", chainLinkProvider.ProviderName);
            Assert.Equal("ChainLink Provider", chainLinkProvider.ProviderDescription);
            Assert.NotNull(chainLinkProvider.ProviderType);
            Assert.NotNull(chainLinkProvider.ProviderCategory);
        }
    }
}
