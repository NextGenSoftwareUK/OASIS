using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS.UnitTests
{
    public class ScuttlebuttOASISTests
    {
        [Fact]
        public void Constructor_ShouldInitializeProviderCorrectly()
        {
            // Arrange & Act
            var scuttlebuttProvider = new ScuttlebuttOASIS();

            // Assert
            Assert.Equal("ScuttlebuttOASIS", scuttlebuttProvider.ProviderName);
            Assert.Equal("Scuttlebutt Provider", scuttlebuttProvider.ProviderDescription);
            Assert.Equal(ProviderType.ScuttlebuttOASIS, scuttlebuttProvider.ProviderType.Value);
            Assert.Equal(ProviderCategory.StorageAndNetwork, scuttlebuttProvider.ProviderCategory.Value);
        }

        [Fact]
        public void ProviderName_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var scuttlebuttProvider = new ScuttlebuttOASIS();

            // Assert
            Assert.Equal("ScuttlebuttOASIS", scuttlebuttProvider.ProviderName);
        }

        [Fact]
        public void ProviderDescription_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var scuttlebuttProvider = new ScuttlebuttOASIS();

            // Assert
            Assert.Equal("Scuttlebutt Provider", scuttlebuttProvider.ProviderDescription);
        }

        [Fact]
        public void ProviderType_ShouldBeScuttlebuttOASIS()
        {
            // Arrange & Act
            var scuttlebuttProvider = new ScuttlebuttOASIS();

            // Assert
            Assert.Equal(ProviderType.ScuttlebuttOASIS, scuttlebuttProvider.ProviderType.Value);
        }

        [Fact]
        public void ProviderCategory_ShouldBeStorageAndNetwork()
        {
            // Arrange & Act
            var scuttlebuttProvider = new ScuttlebuttOASIS();

            // Assert
            Assert.Equal(ProviderCategory.StorageAndNetwork, scuttlebuttProvider.ProviderCategory.Value);
        }

        [Fact]
        public async Task ActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var scuttlebuttProvider = new ScuttlebuttOASIS();

            // Act
            var result = await scuttlebuttProvider.ActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("Scuttlebutt provider activated successfully", result.Message);
        }

        [Fact]
        public async Task DeActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var scuttlebuttProvider = new ScuttlebuttOASIS();

            // Act
            var result = await scuttlebuttProvider.DeActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("Scuttlebutt provider deactivated successfully", result.Message);
        }

        [Fact]
        public async Task LoadAvatarAsync_ShouldReturnScuttlebuttAvatar()
        {
            // Arrange
            var scuttlebuttProvider = new ScuttlebuttOASIS();
            var avatarId = Guid.NewGuid();

            // Act
            var result = await scuttlebuttProvider.LoadAvatarAsync(avatarId);

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
            Assert.Equal(avatarId, result.Result.Id);
            Assert.Contains("scuttlebutt_user", result.Result.Username);
            Assert.Contains("scuttlebutt.example", result.Result.Email);
        }
    }
}
