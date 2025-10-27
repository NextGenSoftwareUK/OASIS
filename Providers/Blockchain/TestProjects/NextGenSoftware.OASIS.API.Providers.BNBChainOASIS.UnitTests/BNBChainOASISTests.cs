using Xunit;
using NextGenSoftware.OASIS.API.Providers.BNBChainOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.BNBChainOASIS.UnitTests
{
    /// <summary>
    /// Unit tests for BNB Chain OASIS Provider
    /// </summary>
    public class BNBChainOASISTests
    {
        private readonly BNBChainOASIS _provider;

        public BNBChainOASISTests()
        {
            _provider = new BNBChainOASIS();
        }

        [Fact]
        public void Constructor_ShouldInitializeProvider()
        {
            // Assert
            Assert.NotNull(_provider);
            Assert.Equal(ProviderType.BNBChainOASIS, _provider.ProviderType);
            Assert.Equal("BNB Chain OASIS Provider", _provider.ProviderName);
            Assert.Equal("BNB Chain (Binance Smart Chain) OASIS Provider", _provider.ProviderDescription);
            Assert.Equal(ProviderCategory.Blockchain, _provider.ProviderCategory);
        }

        [Fact]
        public void ProviderType_ShouldBeBNBChainOASIS()
        {
            // Assert
            Assert.Equal(ProviderType.BNBChainOASIS, _provider.ProviderType);
        }

        [Fact]
        public void ProviderName_ShouldBeCorrect()
        {
            // Assert
            Assert.Equal("BNB Chain OASIS Provider", _provider.ProviderName);
        }

        [Fact]
        public void ProviderDescription_ShouldBeCorrect()
        {
            // Assert
            Assert.Equal("BNB Chain (Binance Smart Chain) OASIS Provider", _provider.ProviderDescription);
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
