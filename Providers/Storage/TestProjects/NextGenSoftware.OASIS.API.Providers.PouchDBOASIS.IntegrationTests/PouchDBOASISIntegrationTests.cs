using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.PouchDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.IntegrationTests
{
    [TestClass]
    public class PouchDBOASISIntegrationTests
    {
        private PouchDBOASIS _provider = null!;
        private static readonly string ServerUrl = Environment.GetEnvironmentVariable("COUCHDB_URL") ?? "http://localhost:5984";
        private static readonly string? Username = Environment.GetEnvironmentVariable("COUCHDB_USER");
        private static readonly string? Password = Environment.GetEnvironmentVariable("COUCHDB_PASS");

        [TestInitialize]
        public void Setup()
        {
            _provider = new PouchDBOASIS(ServerUrl, Username, Password);
            _provider.ActivateProvider();
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnSuccessResult()
        {
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "pouchdb-test-user", Email = "pouchdb@test.com" };
            var result = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
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
            var holon = new Holon { Id = Guid.NewGuid(), Name = "PouchDB Test Holon" };
            var result = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsNotNull(result.Result);
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
