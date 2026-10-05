using Xunit;
using NextGenSoftware.OASIS.API.Providers.FantomOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.FantomOASIS.UnitTests
{
    /// <summary>
    /// Unit tests for Fantom OASIS Provider
    /// </summary>
    public class FantomOASISTests
    {
        private readonly FantomOASIS _provider;

        public FantomOASISTests()
        {
            _provider = new FantomOASIS();
        }

        [Fact]
        public void Constructor_ShouldInitializeProvider()
        {
            // Assert
            Assert.NotNull(_provider);
            Assert.Equal(ProviderType.FantomOASIS, _provider.ProviderType);
            Assert.Equal("Fantom OASIS Provider", _provider.ProviderName);
            Assert.Equal("Fantom (FTM) OASIS Provider", _provider.ProviderDescription);
            Assert.Equal(ProviderCategory.Blockchain, _provider.ProviderCategory);
        }

        [Fact]
        public void ProviderType_ShouldBeFantomOASIS()
        {
            // Assert
            Assert.Equal(ProviderType.FantomOASIS, _provider.ProviderType);
        }

        [Fact]
        public void ProviderName_ShouldBeCorrect()
        {
            // Assert
            Assert.Equal("Fantom OASIS Provider", _provider.ProviderName);
        }

        [Fact]
        public void ProviderDescription_ShouldBeCorrect()
        {
            // Assert
            Assert.Equal("Fantom (FTM) OASIS Provider", _provider.ProviderDescription);
        }

        [Fact]
        public void ProviderCategory_ShouldBeBlockchain()
        {
            // Assert
            Assert.Equal(ProviderCategory.Blockchain, _provider.ProviderCategory);
        }

        [Fact]
        public void ProviderVersion_ShouldNotBeEmpty()
        {
            // Assert
            Assert.NotNull(_provider.ProviderVersion);
            Assert.NotEmpty(_provider.ProviderVersion);
        }

        [Fact]
        public void IsActivated_ShouldBeFalseInitially()
        {
            // Assert
            Assert.False(_provider.IsActivated);
        }

        [Fact]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            // Assert
            Assert.False(_provider.IsProviderActivated);
        }

        [Fact]
        public void IsProviderConnected_ShouldBeFalseInitially()
        {
            // Assert
            Assert.False(_provider.IsProviderConnected);
        }
    }
}
