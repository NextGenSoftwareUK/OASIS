using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.PLANOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.PLANOASIS.UnitTests
{
    public class PLANOASISTests
    {
        [Fact]
        public void Constructor_ShouldInitializeProviderCorrectly()
        {
            // Arrange & Act
            var planProvider = new PLANOASIS();

            // Assert
            Assert.Equal("PLANOASIS", planProvider.ProviderName);
            Assert.Equal("PLAN Provider", planProvider.ProviderDescription);
            Assert.Equal(ProviderType.PLANOASIS, planProvider.ProviderType.Value);
            Assert.Equal(ProviderCategory.StorageAndNetwork, planProvider.ProviderCategory.Value);
        }

        [Fact]
        public void ProviderName_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var planProvider = new PLANOASIS();

            // Assert
            Assert.Equal("PLANOASIS", planProvider.ProviderName);
        }

        [Fact]
        public void ProviderDescription_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var planProvider = new PLANOASIS();

            // Assert
            Assert.Equal("PLAN Provider", planProvider.ProviderDescription);
        }

        [Fact]
        public void ProviderType_ShouldBePLANOASIS()
        {
            // Arrange & Act
            var planProvider = new PLANOASIS();

            // Assert
            Assert.Equal(ProviderType.PLANOASIS, planProvider.ProviderType.Value);
        }

        [Fact]
        public void ProviderCategory_ShouldBeStorageAndNetwork()
        {
            // Arrange & Act
            var planProvider = new PLANOASIS();

            // Assert
            Assert.Equal(ProviderCategory.StorageAndNetwork, planProvider.ProviderCategory.Value);
        }

        [Fact]
        public async Task ActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var planProvider = new PLANOASIS();

            // Act
            var result = await planProvider.ActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("PLAN provider activated successfully", result.Message);
        }

        [Fact]
        public async Task DeActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var planProvider = new PLANOASIS();

            // Act
            var result = await planProvider.DeActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("PLAN provider deactivated successfully", result.Message);
        }

        [Fact]
        public async Task LoadAvatarAsync_WhenApiUnreachable_ReturnsErrorInsteadOfThrowing()
        {
            // Arrange: nothing listens on port 1, so the call fails fast and
            // offline, without depending on the public PLAN API.
            var planProvider = new PLANOASIS("http://127.0.0.1:1/v1");

            // Act
            var result = await planProvider.LoadAvatarAsync(Guid.NewGuid());

            // Assert
            Assert.True(result.IsError);
            Assert.Null(result.Result);
            Assert.False(string.IsNullOrWhiteSpace(result.Message));
        }
    }
}
