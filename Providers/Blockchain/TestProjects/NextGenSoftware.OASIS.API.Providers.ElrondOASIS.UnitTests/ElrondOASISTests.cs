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
        public async Task ActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var elrondProvider = new ElrondOASIS();

            // Act
            var result = await elrondProvider.ActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("Elrond provider activated successfully", result.Message);
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
            Assert.Contains("Elrond provider deactivated successfully", result.Message);
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
