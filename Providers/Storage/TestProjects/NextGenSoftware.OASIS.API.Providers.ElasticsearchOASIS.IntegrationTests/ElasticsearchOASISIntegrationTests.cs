using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.IntegrationTests
{
    [TestClass]
    public class ElasticsearchOASISIntegrationTests
    {
        private ElasticsearchOASIS _provider;
        private static readonly string Host = Environment.GetEnvironmentVariable("ELASTICSEARCH_HOST") ?? "http://localhost:9200";

        [TestInitialize]
        public void Setup()
        {
            _provider = new ElasticsearchOASIS(Host);
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "ElasticsearchTestUser",
                Email = "elasticsearchtest@example.com",
                FirstName = "Elasticsearch",
                LastName = "Tester"
            };
            var result = await _provider.SaveAvatarAsync(avatar);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnAvatar()
        {
            var avatarId = Guid.NewGuid();
            var result = await _provider.LoadAvatarAsync(avatarId);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task SaveHolon_ShouldReturnSuccessResult()
        {
            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = "ElasticsearchTestHolon",
                Description = "Test Holon for ElasticsearchOASIS integration"
            };
            var result = await _provider.SaveHolonAsync(holon);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsError);
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnHolon()
        {
            var holonId = Guid.NewGuid();
            var result = await _provider.LoadHolonAsync(holonId);
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
