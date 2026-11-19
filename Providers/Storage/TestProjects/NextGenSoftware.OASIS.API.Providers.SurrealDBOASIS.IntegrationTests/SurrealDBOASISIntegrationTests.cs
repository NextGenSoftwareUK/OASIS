using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.IntegrationTests
{
    /// <summary>
    /// Exercises the real CRUD surface against a live backend.
    ///
    /// Connection details come from SURREALDBOASIS_&lt;PARAM&gt; environment variables. When the
    /// endpoint is absent, the tests are inconclusive. An explicitly configured
    /// fixture must activate successfully.
    /// </summary>
    [TestClass]
    public class SurrealDBOASISIntegrationTests
    {
        private SurrealDBOASIS _provider = null!;
        private bool _live;

        [TestInitialize]
        public void Setup()
        {
            _provider = SurrealDBOASISTestFactory.Create();
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SURREALDBOASIS_ENDPOINT")))
                return;

            var activation = _provider.ActivateProvider();
            Assert.IsFalse(activation.IsError, activation.Exception?.ToString() ?? activation.Message);
            Assert.IsTrue(_provider.IsProviderActivated, "The configured SurrealDB fixture did not activate.");
            _live = true;
        }

        private void RequireLive()
        {
            if (!_live)
                Assert.Inconclusive(
                    "No live SurrealDBOASIS backend configured - set the SURREALDBOASIS_* environment variables to run this.");
        }

        [TestMethod]
        public async Task AvatarDetail_RoundTripsByGuid()
        {
            RequireLive();

            var detail = new AvatarDetail { Id = Guid.NewGuid(), Karma = 29 };
            var saved = await _provider.SaveAvatarDetailAsync(detail);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loaded = await _provider.LoadAvatarDetailAsync(detail.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.IsNotNull(loaded.Result);
            Assert.AreEqual(detail.Id, loaded.Result.Id);
            Assert.AreEqual(detail.Karma, loaded.Result.Karma);
        }

        [TestMethod]
        public async Task HardDeletedAvatar_IsAbsentByGuidAndUsername()
        {
            RequireLive();

            var id = Guid.NewGuid();
            var avatar = new Avatar { Id = id, Username = $"oasis-it-{id:N}", Email = $"{id:N}@test.local" };
            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);
            var before = await _provider.LoadAvatarAsync(id);
            Assert.IsFalse(before.IsError, before.Message);
            Assert.AreEqual(id, before.Result?.Id);

            var deleted = await _provider.DeleteAvatarAsync(id, softDelete: false);
            Assert.IsFalse(deleted.IsError, deleted.Message);
            Assert.IsTrue(deleted.Result);

            var byId = await _provider.LoadAvatarAsync(id);
            Assert.IsNull(byId.Exception);
            Assert.IsNull(byId.Result);
            var byUsername = await _provider.LoadAvatarByUsernameAsync(avatar.Username);
            Assert.IsNull(byUsername.Exception);
            Assert.IsNull(byUsername.Result);
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
