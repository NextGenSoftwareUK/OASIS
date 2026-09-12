using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.WeaviateOASIS;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.WeaviateOASIS.IntegrationTests
{
    [TestClass]
    public class WeaviateOASISIntegrationTests
    {
        private WeaviateOASIS _provider;
        private static readonly string Host = Environment.GetEnvironmentVariable("WEAVIATE_HOST") ?? "http://localhost:8080";

        [TestInitialize]
        public void Setup()
        {
            _provider = new WeaviateOASIS(Host);
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "WeaviateTestUser",
                Email = "weaviatetest@example.com",
                FirstName = "Weaviate",
                LastName = "Tester"
            };
            var result = await _provider.SaveAvatarAsync(avatar);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnAvatar()
        {
            var result = await _provider.LoadAvatarAsync(Guid.NewGuid());
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task SaveHolon_ShouldReturnSuccessResult()
        {
            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = "WeaviateTestHolon",
                Description = "Test Holon for WeaviateOASIS integration"
            };
            var result = await _provider.SaveHolonAsync(holon);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnHolon()
        {
            var result = await _provider.LoadHolonAsync(Guid.NewGuid());
            Assert.IsNotNull(result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
