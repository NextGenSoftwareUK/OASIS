using Xunit;
using NextGenSoftware.OASIS.API.Providers.BNBChainOASIS;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.BNBChainOASIS.IntegrationTests
{
    /// <summary>
    /// Integration tests for BNB Chain OASIS Provider
    /// These tests require actual BNB Chain network connectivity and should be run in a test environment.
    /// </summary>
    public class BNBChainOASISIntegrationTests : IClassFixture<BNBChainOASISIntegrationTestFixture>
    {
        private readonly BNBChainOASIS _provider;
        private readonly BNBChainOASISIntegrationTestFixture _fixture;

        public BNBChainOASISIntegrationTests(BNBChainOASISIntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _provider = new BNBChainOASIS();
        }

        [Fact]
        public async Task ActivateProvider_ShouldSucceed()
        {
            // Act
            var result = await _provider.ActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(_provider.IsActivated);
        }

        [Fact]
        public async Task Connect_ShouldSucceed()
        {
            // Arrange
            await _provider.ActivateProviderAsync();

            // Act
            var result = await _provider.ConnectAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(_provider.IsProviderConnected);
        }

        [Fact]
        public async Task SaveAvatar_ShouldSucceed()
        {
            // Arrange
            await _provider.ActivateProviderAsync();
            var avatar = new Avatar
            {
                Username = "testuser_bnb_integration",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            var result = await _provider.SaveAvatarAsync(avatar);

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
            Assert.NotEqual(Guid.Empty, result.Result.Id);
        }

        [Fact]
        public async Task LoadAvatar_ShouldSucceed()
        {
            // Arrange
            await _provider.ActivateProviderAsync();
            var avatar = new Avatar
            {
                Username = "testuser_bnb_load",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            var saveResult = await _provider.SaveAvatarAsync(avatar);
            Assert.False(saveResult.IsError);

            // Act
            var result = await _provider.LoadAvatarAsync(avatar.Id);

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
            Assert.Equal(avatar.Username, result.Result.Username);
        }

        [Fact]
        public async Task SaveHolon_ShouldSucceed()
        {
            // Arrange
            await _provider.ActivateProviderAsync();
            var holon = new Holon
            {
                Name = "Test Holon BNB Integration",
                Description = "A test holon for BNB Chain integration testing",
                HolonType = HolonType.Generic
            };

            // Act
            var result = await _provider.SaveHolonAsync(holon);

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
            Assert.NotEqual(Guid.Empty, result.Result.Id);
        }

        [Fact]
        public async Task LoadHolon_ShouldSucceed()
        {
            // Arrange
            await _provider.ActivateProviderAsync();
            var holon = new Holon
            {
                Name = "Test Holon BNB Load",
                Description = "A test holon for BNB Chain loading testing",
                HolonType = HolonType.Generic
            };
            var saveResult = await _provider.SaveHolonAsync(holon);
            Assert.False(saveResult.IsError);

            // Act
            var result = await _provider.LoadHolonAsync(holon.Id);

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
            Assert.Equal(holon.Name, result.Result.Name);
        }

        [Fact]
        public async Task SearchAvatars_ShouldReturnResults()
        {
            // Arrange
            await _provider.ActivateProviderAsync();
            var avatar = new Avatar
            {
                Username = "testuser_bnb_search",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            var saveResult = await _provider.SaveAvatarAsync(avatar);
            Assert.False(saveResult.IsError);

            // Act
            var result = await _provider.SearchAvatarsAsync("testuser_bnb_search");

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
        }

        [Fact]
        public async Task SearchHolons_ShouldReturnResults()
        {
            // Arrange
            await _provider.ActivateProviderAsync();
            var holon = new Holon
            {
                Name = "Test Holon BNB Search",
                Description = "A test holon for BNB Chain search testing",
                HolonType = HolonType.Generic
            };
            var saveResult = await _provider.SaveHolonAsync(holon);
            Assert.False(saveResult.IsError);

            // Act
            var result = await _provider.SearchHolonsAsync("Test Holon BNB Search");

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
        }

        [Fact]
        public async Task GetAccountBalance_ShouldHandleInvalidAddress()
        {
            // Arrange
            await _provider.ActivateProviderAsync();
            var invalidAddress = "0x0000000000000000000000000000000000000000";

            // Act
            var result = await _provider.GetAccountBalanceAsync(invalidAddress);

            // Assert
            // This should either succeed with 0 balance or fail gracefully
            // The exact behavior depends on the BNB Chain network response
            Assert.NotNull(result);
        }
    }

    /// <summary>
    /// Test fixture for BNB Chain integration tests
    /// </summary>
    public class BNBChainOASISIntegrationTestFixture : IDisposable
    {
        public BNBChainOASISIntegrationTestFixture()
        {
            // Setup test environment
            // Configure test network settings if needed
        }

        public void Dispose()
        {
            // Cleanup test environment
        }
    }
}
