using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS.IntegrationTests
{
    [TestClass]
    public class CouchbaseOASISIntegrationTests
    {
        private CouchbaseOASIS _provider;
        private static readonly string ConnStr = Environment.GetEnvironmentVariable("COUCHBASE_CONN") ?? "couchbase://localhost";
        private static readonly string Username = Environment.GetEnvironmentVariable("COUCHBASE_USER") ?? "Administrator";
        private static readonly string Password = Environment.GetEnvironmentVariable("COUCHBASE_PASS") ?? "password";

        [TestInitialize]
        public void Setup()
        {
            _provider = new CouchbaseOASIS(ConnStr, Username, Password);
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = "CouchbaseTestUser",
                Email = "couchbasetest@example.com",
                FirstName = "Couchbase",
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
                Name = "CouchbaseTestHolon",
                Description = "Test Holon for CouchbaseOASIS integration"
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
