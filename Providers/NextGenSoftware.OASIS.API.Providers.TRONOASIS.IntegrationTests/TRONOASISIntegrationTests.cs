using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.IntegrationTests
{
    public class TRONOASISIntegrationTests
    {
        [Fact]
        public async Task FullProviderLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var tronProvider = new TRONOASIS();

            // Act & Assert - Activation
            var activationResult = await tronProvider.ActivateProviderAsync();
            Assert.False(activationResult.IsError);
            Assert.True(activationResult.Result);
            Assert.Contains("TRON provider activated successfully", activationResult.Message);

            // Act & Assert - Deactivation
            var deactivationResult = await tronProvider.DeActivateProviderAsync();
            Assert.False(deactivationResult.IsError);
            Assert.True(deactivationResult.Result);
            Assert.Contains("TRON provider deactivated successfully", deactivationResult.Message);
        }

        [Fact]
        public async Task MultipleActivationDeactivationCycles_ShouldWorkCorrectly()
        {
            // Arrange
            var tronProvider = new TRONOASIS();

            // Act & Assert - Multiple cycles
            for (int i = 0; i < 3; i++)
            {
                var activationResult = await tronProvider.ActivateProviderAsync();
                Assert.False(activationResult.IsError);
                Assert.True(activationResult.Result);

                var deactivationResult = await tronProvider.DeActivateProviderAsync();
                Assert.False(deactivationResult.IsError);
                Assert.True(deactivationResult.Result);
            }
        }

        [Fact]
        public void NFTDataLoading_ShouldHandleDifferentTokenAddresses()
        {
            // Arrange
            var tronProvider = new TRONOASIS();
            var testAddresses = new[]
            {
                "TTestTokenAddress123",
                "TAnotherTokenAddress456",
                "TThirdTokenAddress789"
            };

            // Act & Assert
            foreach (var address in testAddresses)
            {
                var result = tronProvider.LoadOnChainNFTData(address);
                Assert.False(result.IsError);
                Assert.NotNull(result.Result);
                Assert.Equal(address, result.Result.TokenId);
                Assert.Contains("TRON NFT", result.Result.Name);
            }
        }

        [Fact]
        public async Task AsyncNFTDataLoading_ShouldWorkCorrectly()
        {
            // Arrange
            var tronProvider = new TRONOASIS();
            var tokenAddress = "TAsyncTestTokenAddress123";

            // Act
            var result = await tronProvider.LoadOnChainNFTDataAsync(tokenAddress);

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
            Assert.Equal(tokenAddress, result.Result.TokenId);
            Assert.Contains("TRON NFT", result.Result.Name);
            Assert.Contains("TRON blockchain", result.Result.Description);
        }

        [Fact]
        public void ProviderProperties_ShouldBeConsistent()
        {
            // Arrange
            var tronProvider = new TRONOASIS();

            // Act & Assert
            Assert.Equal("TRONOASIS", tronProvider.ProviderName);
            Assert.Equal("TRON Provider", tronProvider.ProviderDescription);
            Assert.NotNull(tronProvider.ProviderType);
            Assert.NotNull(tronProvider.ProviderCategory);
        }
    }
}
