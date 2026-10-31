using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests
{
    /// <summary>
    /// Exercises the real CRUD surface against a live backend.
    ///
    /// Missing ORACLEDBOASIS_CONNECTIONSTRING makes the run inconclusive.
    /// Configured activation failures fail the run. Activation currently creates
    /// schema, so configure only an explicitly authorized test database.
    /// </summary>
    [TestClass]
    public class OracleDBOASISIntegrationTests
    {
        private OracleDBOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")))
                Assert.Inconclusive("Set ORACLEDBOASIS_CONNECTIONSTRING only for an authorized test database; activation currently creates schema.");
            _provider = OracleDBOASISTestFactory.Create();
            var activation = _provider.ActivateProvider();
            Assert.IsFalse(activation.IsError, "Configured Oracle activation failed; inspect its OASISResult exception.");
            Assert.IsTrue(activation.Result);
            Assert.IsTrue(_provider.IsProviderActivated);
        }

        private void RequireLive()
        {
            Assert.IsTrue(_provider.IsProviderActivated);
        }

        [TestMethod]
        public async Task SaveAndLoadAvatar_RoundTrips()
        {
            RequireLive();

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"oasis-it-{Guid.NewGuid():N}",
                Email = $"oasis-it-{Guid.NewGuid():N}@test.local"
            };

            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loaded = await _provider.LoadAvatarAsync(avatar.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.IsNotNull(loaded.Result);
            Assert.AreEqual(avatar.Username, loaded.Result!.Username);

            await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
        }

        [TestMethod]
        public async Task LoadAvatarByUsername_FindsTheSavedAvatar()
        {
            RequireLive();

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"oasis-it-{Guid.NewGuid():N}",
                Email = $"oasis-it-{Guid.NewGuid():N}@test.local"
            };

            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);

            var found = await _provider.LoadAvatarByUsernameAsync(avatar.Username);
            Assert.IsFalse(found.IsError, found.Message);
            Assert.AreEqual(avatar.Id, found.Result!.Id);

            await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
        }

        [TestMethod]
        public async Task SaveAndLoadHolon_RoundTrips()
        {
            RequireLive();

            var holon = new Holon { Id = Guid.NewGuid(), Name = $"OASIS IT Holon {Guid.NewGuid():N}" };

            var saved = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loaded = await _provider.LoadHolonAsync(holon.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(holon.Name, loaded.Result!.Name);

            await _provider.DeleteHolonAsync(holon.Id);
        }

        [TestMethod]
        public async Task DeletedAvatar_IsNotReturnedAsLive()
        {
            RequireLive();

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"oasis-it-{Guid.NewGuid():N}",
                Email = $"oasis-it-{Guid.NewGuid():N}@test.local"
            };

            await _provider.SaveAvatarAsync(avatar);
            var deleted = await _provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
            Assert.IsFalse(deleted.IsError, deleted.Message);

            var reloaded = await _provider.LoadAvatarAsync(avatar.Id);
            Assert.IsTrue(reloaded.IsError || reloaded.Result == null || reloaded.Result.IsDeleted,
                "A soft-deleted avatar should not come back as a live record.");
        }

        [TestMethod]
        public async Task LoadAllAvatars_ReturnsAResult()
        {
            RequireLive();
            var all = await _provider.LoadAllAvatarsAsync();
            Assert.IsNotNull(all);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
