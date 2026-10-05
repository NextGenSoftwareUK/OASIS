using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS.UnitTests
{
    public class ThreeFoldOASISTests
    {
        [Fact]
        public void Constructor_ShouldInitializeProviderCorrectly()
        {
            // Arrange
            var hostUri = "https://grid.threefold.io";

            // Act
            var threeFoldProvider = new ThreeFoldOASIS(hostUri);

            // Assert
            Assert.Equal("ThreeFoldOASIS", threeFoldProvider.ProviderName);
            Assert.Equal("ThreeFold Provider", threeFoldProvider.ProviderDescription);
            Assert.Equal(ProviderType.ThreeFoldOASIS, threeFoldProvider.ProviderType.Value);
            Assert.Equal(ProviderCategory.StorageAndNetwork, threeFoldProvider.ProviderCategory.Value);
            Assert.Equal(hostUri, threeFoldProvider.HostUri);
        }

        [Fact]
        public void ProviderName_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var threeFoldProvider = new ThreeFoldOASIS("https://grid.threefold.io");

            // Assert
            Assert.Equal("ThreeFoldOASIS", threeFoldProvider.ProviderName);
        }

        [Fact]
        public void ProviderDescription_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var threeFoldProvider = new ThreeFoldOASIS("https://grid.threefold.io");

            // Assert
            Assert.Equal("ThreeFold Provider", threeFoldProvider.ProviderDescription);
        }

        [Fact]
        public void ProviderType_ShouldBeThreeFoldOASIS()
        {
            // Arrange & Act
            var threeFoldProvider = new ThreeFoldOASIS("https://grid.threefold.io");

            // Assert
            Assert.Equal(ProviderType.ThreeFoldOASIS, threeFoldProvider.ProviderType.Value);
        }

        [Fact]
        public void ProviderCategory_ShouldBeStorageAndNetwork()
        {
            // Arrange & Act
            var threeFoldProvider = new ThreeFoldOASIS("https://grid.threefold.io");

            // Assert
            Assert.Equal(ProviderCategory.StorageAndNetwork, threeFoldProvider.ProviderCategory.Value);
        }

        [Fact]
        public void HostUri_ShouldBeSetCorrectly()
        {
            // Arrange
            var hostUri = "https://grid.threefold.io";

            // Act
            var threeFoldProvider = new ThreeFoldOASIS(hostUri);

            // Assert
            Assert.Equal(hostUri, threeFoldProvider.HostUri);
        }

        [Fact]
        public async Task ActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var threeFoldProvider = new ThreeFoldOASIS("https://grid.threefold.io");

            // Act
            var result = await threeFoldProvider.ActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("ThreeFold provider activated successfully", result.Message);
        }

        [Fact]
        public async Task DeActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var threeFoldProvider = new ThreeFoldOASIS("https://grid.threefold.io");

            // Act
            var result = await threeFoldProvider.DeActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("ThreeFold provider deactivated successfully", result.Message);
        }
    }
}
