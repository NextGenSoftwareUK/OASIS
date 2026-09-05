using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.UpstashOASIS;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.UpstashOASIS.IntegrationTests
{
    [TestClass]
    public class UpstashOASISIntegrationTests
    {
        private UpstashOASIS _provider;
        private static readonly string RestUrl = Environment.GetEnvironmentVariable("UPSTASH_REST_URL") ?? "https://test.upstash.io";
        private static readonly string Token = Environment.GetEnvironmentVariable("UPSTASH_TOKEN") ?? "test-token";

        [TestInitialize]
        public void Setup()
        {
            _provider = new UpstashOASIS(RestUrl, Token);
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "UpstashTestUser",
                Email = "upstashtest@example.com",
                FirstName = "Upstash",
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
                Name = "UpstashTestHolon",
                Description = "Test Holon for UpstashOASIS integration"
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
