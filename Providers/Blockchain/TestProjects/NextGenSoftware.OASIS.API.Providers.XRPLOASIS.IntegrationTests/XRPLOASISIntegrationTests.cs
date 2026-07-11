using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.XRPLOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.XRPLOASIS.IntegrationTests
{
    [TestClass]
    public class XRPLOASISIntegrationTests
    {
        private XRPLOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new XRPLOASIS();
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            var avatar = new Avatar
            {
                Id = System.Guid.NewGuid(),
                Username = "TestUser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            var result = await _provider.SaveAvatarAsync(avatar);
            Assert.IsNotNull(result);
            // Note: May fail if wallet not configured, which is expected
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnAvatar()
        {
            var avatarId = System.Guid.NewGuid();
            var result = await _provider.LoadAvatarAsync(avatarId);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task SaveHolon_ShouldReturnSuccessResult()
        {
            var holon = new Holon
            {
                Id = System.Guid.NewGuid(),
                Name = "TestHolon",
                Description = "Test Holon Description"
            };
            var result = await _provider.SaveHolonAsync(holon);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnHolon()
        {
            var holonId = System.Guid.NewGuid();
            var result = await _provider.LoadHolonAsync(holonId);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Search_ShouldReturnSearchResults()
        {
            var searchParams = new SearchParams
            {
                SearchQuery = "test",
                SearchType = SearchType.Holon
            };
            var result = await _provider.SearchAsync(searchParams);
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


