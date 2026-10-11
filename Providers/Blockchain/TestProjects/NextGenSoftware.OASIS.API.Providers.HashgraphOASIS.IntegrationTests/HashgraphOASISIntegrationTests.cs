using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests
{
    [TestClass]
    public class HashgraphUnconfiguredStorageTests
    {
        private HashgraphOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new HashgraphOASIS();
        }

        [TestMethod]
        public async Task UnconfiguredAvatarSaveReportsMissingStorageConfiguration()
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
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "explicit contract");
        }

        [TestMethod]
        public async Task UnconfiguredAvatarLoadReportsMissingStorageConfiguration()
        {
            // Arrange
            var avatarId = Guid.NewGuid();

            // Act
            var result = await _provider.LoadAvatarAsync(avatarId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "explicit contract");
        }

        [TestMethod]
        public async Task UnconfiguredHolonSaveReportsMissingStorageConfiguration()
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
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "explicit contract");
        }

        [TestMethod]
        public async Task UnconfiguredHolonLoadReportsMissingStorageConfiguration()
        {
            // Arrange
            var holonId = Guid.NewGuid();

            // Act
            var result = await _provider.LoadHolonAsync(holonId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "explicit contract");
        }

        [TestMethod]
        public async Task UnconfiguredAvatarSearchReportsMissingStorageConfiguration()
        {
            // Arrange
            var searchParams = new SearchParams
            {
                SearchOnlyForCurrentAvatar = false,
                FilterByMetaData = new Dictionary<string, string> { ["Username"] = "TestUser" },
                SearchGroups = new List<ISearchGroupBase> { new SearchTextGroup { SearchQuery = "TestUser" } }
            };

            // Act
            var result = await _provider.SearchAsync(searchParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "explicit contract");
        }

        [TestMethod]
        public async Task UnconfiguredHolonSearchReportsMissingStorageConfiguration()
        {
            // Arrange
            var searchParams = new SearchParams
            {
                SearchOnlyForCurrentAvatar = false,
                FilterByMetaData = new Dictionary<string, string> { ["Name"] = "TestHolon" },
                SearchGroups = new List<ISearchGroupBase> { new SearchTextGroup { SearchQuery = "TestHolon" } }
            };

            // Act
            var result = await _provider.SearchAsync(searchParams);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "explicit contract");
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
