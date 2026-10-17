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
            Assert.Equal(ProviderType.BNBChainOASIS, _provider.ProviderType.Value);
            Assert.Equal("BNBChainOASIS", _provider.ProviderName);
            Assert.NotNull(_provider.ProviderDescription);
            Assert.NotEmpty(_provider.ProviderDescription);
            Assert.NotNull(_provider.ProviderCategory.Value);
        }

        [Fact]
        public void ProviderType_ShouldBeBNBChainOASIS()
        {
            // Assert
            Assert.Equal(ProviderType.BNBChainOASIS, _provider.ProviderType.Value);
        }

        [Fact]
        public void ProviderName_ShouldBeCorrect()
        {
            // Assert
            Assert.Equal("BNBChainOASIS", _provider.ProviderName);
        }

        [Fact]
        public void ProviderDescription_ShouldBeCorrect()
        {
            // Assert
            Assert.NotNull(_provider.ProviderDescription);
            Assert.NotEmpty(_provider.ProviderDescription);
        }

        [Fact]
        public void ProviderCategory_ShouldBeSet()
        {
            // Assert
            Assert.NotNull(_provider.ProviderCategory.Value);
        }

        [Fact]
        public void ProviderName_ShouldNotBeEmpty()
        {
            // Assert
            Assert.NotNull(_provider.ProviderName);
            Assert.NotEmpty(_provider.ProviderName);
        }

        [Fact]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            // Assert
            Assert.False(_provider.IsProviderActivated);
        }
    }
}
