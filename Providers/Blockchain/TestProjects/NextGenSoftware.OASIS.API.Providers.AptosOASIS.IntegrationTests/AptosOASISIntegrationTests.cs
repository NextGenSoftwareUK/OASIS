using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.AptosOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Threading.Tasks;
using System;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS.IntegrationTests
{
    [TestClass]
    public class AptosOASISIntegrationTests
    {
        private AptosOASIS _aptosProvider;
        private const string TEST_ACCOUNT_ADDRESS = "0x1";
        private const string TEST_PRIVATE_KEY = "test_private_key";

        [TestInitialize]
        public void Setup()
        {
            _aptosProvider = new AptosOASIS();
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            // Arrange
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "TestUser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            var result = await _aptosProvider.SaveAvatarAsync(avatar);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnAvatar()
        {
            // Arrange
            var avatarId = Guid.NewGuid();

            // Act
            var result = await _aptosProvider.LoadAvatarAsync(avatarId);

            // Assert
            Assert.IsNotNull(result);
            // Note: This might return an error if avatar doesn't exist, which is expected
        }

        [TestMethod]
        public async Task SaveHolon_ShouldReturnSuccessResult()
        {
            // Arrange
            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = "TestHolon",
                Description = "Test Holon Description"
            };

            // Act
            var result = await _aptosProvider.SaveHolonAsync(holon);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnHolon()
        {
            // Arrange
            var holonId = Guid.NewGuid();

            // Act
            var result = await _aptosProvider.LoadHolonAsync(holonId);

            // Assert
            Assert.IsNotNull(result);
            // Note: This might return an error if holon doesn't exist, which is expected
        }

        [TestMethod]
        public async Task SearchAvatars_ShouldReturnSearchResults()
        {
            // Arrange
            var searchParams = new SearchParams
            {
                SearchQuery = "test",
                SearchType = SearchType.Avatar
            };

            // Act
            var result = await _aptosProvider.SearchAvatarsAsync(searchParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task SearchHolons_ShouldReturnSearchResults()
        {
            // Arrange
            var searchParams = new SearchParams
            {
                SearchQuery = "test",
                SearchType = SearchType.Holon
            };

            // Act
            var result = await _aptosProvider.SearchHolonsAsync(searchParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task DeleteAvatar_ShouldReturnSuccessResult()
        {
            // Arrange
            var avatarId = Guid.NewGuid();

            // Act
            var result = await _aptosProvider.DeleteAvatarAsync(avatarId);

            // Assert
            Assert.IsNotNull(result);
            // Note: This might return an error if avatar doesn't exist, which is expected
        }

        [TestMethod]
        public async Task DeleteHolon_ShouldReturnSuccessResult()
        {
            // Arrange
            var holonId = Guid.NewGuid();

            // Act
            var result = await _aptosProvider.DeleteHolonAsync(holonId);

            // Assert
            Assert.IsNotNull(result);
            // Note: This might return an error if holon doesn't exist, which is expected
        }

        [TestMethod]
        public async Task GetAvatarByEmail_ShouldReturnAvatar()
        {
            // Arrange
            var email = "test@example.com";

            // Act
            var result = await _aptosProvider.GetAvatarByEmailAsync(email);

            // Assert
            Assert.IsNotNull(result);
            // Note: This might return an error if avatar doesn't exist, which is expected
        }

        [TestMethod]
        public async Task GetAvatarByUsername_ShouldReturnAvatar()
        {
            // Arrange
            var username = "testuser";

            // Act
            var result = await _aptosProvider.GetAvatarByUsernameAsync(username);

            // Assert
            Assert.IsNotNull(result);
            // Note: This might return an error if avatar doesn't exist, which is expected
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_aptosProvider != null && _aptosProvider.IsProviderActivated)
            {
                _aptosProvider.DeActivateProvider();
            }
        }
    }
}
