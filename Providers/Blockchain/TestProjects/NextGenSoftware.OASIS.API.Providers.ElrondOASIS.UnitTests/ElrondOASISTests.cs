using System;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.ElrondOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS.UnitTests
{
    public class ElrondOASISTests
    {
        [Fact]
        public void Constructor_ShouldInitializeProviderCorrectly()
        {
            // Arrange & Act
            var elrondProvider = new ElrondOASIS();

            // Assert
            Assert.Equal("ElrondOASIS", elrondProvider.ProviderName);
            Assert.Equal("Elrond Provider", elrondProvider.ProviderDescription);
            Assert.Equal(ProviderType.ElrondOASIS, elrondProvider.ProviderType.Value);
            Assert.Equal(ProviderCategory.StorageAndNetwork, elrondProvider.ProviderCategory.Value);
        }

        [Fact]
        public void ProviderName_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var elrondProvider = new ElrondOASIS();

            // Assert
            Assert.Equal("ElrondOASIS", elrondProvider.ProviderName);
        }

        [Fact]
        public void ProviderDescription_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var elrondProvider = new ElrondOASIS();

            // Assert
            Assert.Equal("Elrond Provider", elrondProvider.ProviderDescription);
        }

        [Fact]
        public void ProviderType_ShouldBeElrondOASIS()
        {
            // Arrange & Act
            var elrondProvider = new ElrondOASIS();

            // Assert
            Assert.Equal(ProviderType.ElrondOASIS, elrondProvider.ProviderType.Value);
        }

        [Fact]
        public void ProviderCategory_ShouldBeStorageAndNetwork()
        {
            // Arrange & Act
            var elrondProvider = new ElrondOASIS();

            // Assert
            Assert.Equal(ProviderCategory.StorageAndNetwork, elrondProvider.ProviderCategory.Value);
        }

        [Fact]
        public async Task ActivateProviderAsync_WithoutStorageConfiguration_FailsClearly()
        {
            // Arrange
            var elrondProvider = new ElrondOASIS();

            // Act
            var result = await elrondProvider.ActivateProviderAsync();

            // Assert: an unconfigured provider must refuse to activate rather than
            // pretend it has a MultiversX store behind it.
            Assert.True(result.IsError);
            Assert.False(result.Result);
            Assert.Contains("requires an explicit proxy", result.Message);
        }

        [Fact]
        public async Task DeActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var elrondProvider = new ElrondOASIS();

            // Act
            var result = await elrondProvider.DeActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("ElrondOASIS deactivated", result.Message);
        }

        [Fact]
        public async Task LoadAvatarAsync_NeverInventsAnAvatarThatDoesNotExist()
        {
            // An unknown id must not come back as a fabricated avatar (the old test expected made-up
            // "elrond_user"/"elrond.example" data); either an error or no result is the honest outcome.
            var result = await new ElrondOASIS().LoadAvatarAsync(Guid.NewGuid());
            Assert.True(result.IsError || result.Result == null);
        }
    }
}
