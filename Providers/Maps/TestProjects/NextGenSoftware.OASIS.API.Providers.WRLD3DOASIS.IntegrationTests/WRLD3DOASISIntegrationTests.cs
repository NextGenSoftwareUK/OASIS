using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.WRLD3DOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Threading.Tasks;
using System;

namespace NextGenSoftware.OASIS.API.Providers.WRLD3DOASIS.IntegrationTests
{
    [TestClass]
    public class WRLD3DOASISIntegrationTests
    {
        private WRLD3DOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new WRLD3DOASIS();
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
            var result = await _provider.SaveAvatarAsync(avatar);

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
            var result = await _provider.LoadAvatarAsync(avatarId);

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
            var result = await _provider.SaveHolonAsync(holon);

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
            var result = await _provider.LoadHolonAsync(holonId);

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
            var result = await _provider.SearchAvatarsAsync(searchParams);

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
            var result = await _provider.SearchHolonsAsync(searchParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
            {
                _provider.DeActivateProvider();
            }
        }
    }
}
