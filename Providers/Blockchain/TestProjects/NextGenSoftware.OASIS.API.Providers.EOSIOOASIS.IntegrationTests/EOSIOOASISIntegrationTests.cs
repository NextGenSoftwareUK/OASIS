using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.IntegrationTests
{
    [TestClass]
    public class EOSIOOASISIntegrationTests
    {
        private EOSIOOASIS _provider = null!;

        [TestInitialize]
        public async Task Setup()
        {
            string endpoint = Environment.GetEnvironmentVariable("OASIS_ANTELOPE_ENDPOINT") ?? "http://127.0.0.1:8888/";
            string account = Environment.GetEnvironmentVariable("OASIS_ANTELOPE_ACCOUNT") ?? "eosio";
            string chainId = Environment.GetEnvironmentVariable("OASIS_ANTELOPE_CHAIN_ID")
                ?? throw new AssertInconclusiveException("OASIS_ANTELOPE_CHAIN_ID must identify a running Antelope chain.");
            string privateKey = Environment.GetEnvironmentVariable("OASIS_ANTELOPE_PRIVATE_KEY")
                ?? throw new AssertInconclusiveException("OASIS_ANTELOPE_PRIVATE_KEY must authorize the configured contract account.");

            _provider = new EOSIOOASIS(endpoint, account, chainId, privateKey);
            var activated = await _provider.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Message);
            Assert.IsTrue(activated.Result, activated.Message);
        }

        [TestMethod]
        public async Task AvatarCrud_PersistsOnOfficialAntelopeNode()
        {
            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"antelope-{Guid.NewGuid():N}",
                Email = $"antelope-{Guid.NewGuid():N}@example.test",
                FirstName = "Antelope",
                LastName = "Runtime"
            };

            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loaded = await _provider.LoadAvatarAsync(avatar.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(avatar.Username, loaded.Result.Username);

            avatar.FirstName = "Updated";
            var updated = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(updated.IsError, updated.Message);
            loaded = await _provider.LoadAvatarAsync(avatar.Id);
            Assert.AreEqual("Updated", loaded.Result.FirstName);

            var deleted = await _provider.DeleteAvatarAsync(avatar.Id, false);
            Assert.IsFalse(deleted.IsError, deleted.Message);
        }

        [TestMethod]
        public async Task HolonCrud_PersistsOnOfficialAntelopeNode()
        {
            var holon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = $"antelope-holon-{Guid.NewGuid():N}",
                Description = "Official Antelope node verification"
            };

            var saved = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loaded = await _provider.LoadHolonAsync(holon.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(holon.Name, loaded.Result.Name);

            holon.Description = "Updated on chain";
            var updated = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(updated.IsError, updated.Message);
            loaded = await _provider.LoadHolonAsync(holon.Id);
            Assert.AreEqual("Updated on chain", loaded.Result.Description);

            var deleted = await _provider.DeleteHolonAsync(holon.Id);
            Assert.IsFalse(deleted.IsError, deleted.Message);
        }

        [TestCleanup]
        public void Cleanup() => _provider?.DeActivateProvider();
    }
}
