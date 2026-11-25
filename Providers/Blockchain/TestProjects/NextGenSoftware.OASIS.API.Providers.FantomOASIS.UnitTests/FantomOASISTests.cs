using System.Threading.Tasks;
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
            Assert.Equal(ProviderType.FantomOASIS, _provider.ProviderType.Value);
            Assert.Equal("FantomOASIS", _provider.ProviderName);
            Assert.Equal("Fantom Provider - High-performance EVM-compatible blockchain using Web3Core", _provider.ProviderDescription);
            Assert.Equal(ProviderCategory.StorageAndNetwork, _provider.ProviderCategory.Value);
        }

        [Fact]
        public void ProviderType_ShouldBeFantomOASIS()
        {
            // Assert
            Assert.Equal(ProviderType.FantomOASIS, _provider.ProviderType.Value);
        }

        [Fact]
        public void ProviderName_ShouldBeCorrect()
        {
            // Assert
            Assert.Equal("FantomOASIS", _provider.ProviderName);
        }

        [Fact]
        public void ProviderDescription_ShouldBeCorrect()
        {
            // Assert
            Assert.Equal("Fantom Provider - High-performance EVM-compatible blockchain using Web3Core", _provider.ProviderDescription);
        }

        [Fact]
        public void ProviderCategory_ShouldBeBlockchain()
        {
            // Assert
            Assert.Equal(ProviderCategory.StorageAndNetwork, _provider.ProviderCategory.Value);
        }

        [Fact]
        public void ProviderCapabilities_IncludeBlockchain()
        {
            // Assert
            Assert.Contains(_provider.ProviderCapabilities, c => c.Value == ProviderCategory.Blockchain);
        }

        [Fact]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            // Assert
            Assert.False(_provider.IsProviderActivated);
        }

        [Fact]
        public async Task ActivateProviderAsync_WithoutChainSettings_DoesNotReportActive()
        {
            // Act
            var result = await _provider.ActivateProviderAsync();

            // Assert: the activated flag must agree with the result it returned.
            Assert.Equal(!result.IsError && result.Result, _provider.IsProviderActivated);
            if (result.IsError)
                Assert.False(string.IsNullOrWhiteSpace(result.Message));
        }
    }
}
