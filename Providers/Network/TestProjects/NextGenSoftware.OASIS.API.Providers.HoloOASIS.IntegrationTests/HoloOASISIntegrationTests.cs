using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.IntegrationTests
{
    public class HoloOASISIntegrationTests
    {
        [Fact]
        public async Task FullProviderLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            var holoProvider = new HoloOASIS();

            // Act & Assert - Activation
            var activationResult = await holoProvider.ActivateProviderAsync();
            Assert.False(activationResult.IsError);
            Assert.True(activationResult.Result);
            Assert.Contains("Holo provider activated successfully", activationResult.Message);

            // Act & Assert - Deactivation
            var deactivationResult = await holoProvider.DeActivateProviderAsync();
            Assert.False(deactivationResult.IsError);
            Assert.True(deactivationResult.Result);
            Assert.Contains("Holo provider deactivated successfully", deactivationResult.Message);
        }

        [Fact]
        public async Task MultipleActivationDeactivationCycles_ShouldWorkCorrectly()
        {
            // Arrange
            var holoProvider = new HoloOASIS();

            // Act & Assert - Multiple cycles
            for (int i = 0; i < 3; i++)
            {
                var activationResult = await holoProvider.ActivateProviderAsync();
                Assert.False(activationResult.IsError);
                Assert.True(activationResult.Result);

                var deactivationResult = await holoProvider.DeActivateProviderAsync();
                Assert.False(deactivationResult.IsError);
                Assert.True(deactivationResult.Result);
            }
        }

        [Fact]
        public void NFTDataLoading_ShouldHandleDifferentTokenAddresses()
        {
            // Arrange
            var holoProvider = new HoloOASIS();
            var testAddresses = new[]
            {
                "HoloTestTokenAddress123",
                "HoloAnotherTokenAddress456",
                "HoloThirdTokenAddress789"
            };

            // Act & Assert
            foreach (var address in testAddresses)
            {
                var result = holoProvider.LoadOnChainNFTData(address);
                Assert.False(result.IsError);
                Assert.NotNull(result.Result);
                Assert.Equal(address, result.Result.TokenId);
                Assert.Contains("Holochain NFT", result.Result.Name);
            }
        }

        [Fact]
        public async Task AsyncNFTDataLoading_ShouldWorkCorrectly()
        {
            // Arrange
            var holoProvider = new HoloOASIS();
            var tokenAddress = "HoloAsyncTestTokenAddress123";

            // Act
            var result = await holoProvider.LoadOnChainNFTDataAsync(tokenAddress);

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
            Assert.Equal(tokenAddress, result.Result.TokenId);
            Assert.Contains("Holochain NFT", result.Result.Name);
            Assert.Contains("Holochain DHT", result.Result.Description);
        }

        [Fact]
        public void ProviderProperties_ShouldBeConsistent()
        {
            // Arrange
            var holoProvider = new HoloOASIS();

            // Act & Assert
            Assert.Equal("HoloOASIS", holoProvider.ProviderName);
            Assert.Equal("Holo Provider", holoProvider.ProviderDescription);
            Assert.NotNull(holoProvider.ProviderType);
            Assert.NotNull(holoProvider.ProviderCategory);
        }
    }
}
