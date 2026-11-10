using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS.IntegrationTests
{
    [TestClass]
    public class PocketBaseOASISIntegrationTests
    {
        private PocketBaseOASIS _provider = null!;

        [TestInitialize]
        public async Task Setup()
        {
            if (Environment.GetEnvironmentVariable("OASIS_POCKETBASE_STORAGE_TEST") != "1")
                Assert.Inconclusive("Enable only for a disposable PocketBase instance.");

            var url = Environment.GetEnvironmentVariable("POCKETBASEOASIS_BASEURL");
            var email = Environment.GetEnvironmentVariable("POCKETBASEOASIS_ADMINEMAIL");
            var password = Environment.GetEnvironmentVariable("POCKETBASEOASIS_ADMINPASSWORD");
            Assert.IsTrue(Uri.TryCreate(url, UriKind.Absolute, out var endpoint) &&
                endpoint.IsLoopback && endpoint.Scheme == Uri.UriSchemeHttp &&
                endpoint.AbsolutePath == "/" && endpoint.UserInfo.Length == 0 &&
                endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0);
            Assert.IsFalse(string.IsNullOrWhiteSpace(email));
            Assert.IsFalse(string.IsNullOrWhiteSpace(password));

            _provider = new PocketBaseOASIS(url!, email!, password!);
            var activated = await _provider.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Exception?.GetType().Name);
            Assert.IsTrue(_provider.IsProviderActivated);
        }

        [TestMethod]
        public async Task SaveAndLoadAvatar_RoundTrips()
        {
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
        public async Task HolonRoundTripsAndSoftDeleteHidesIt()
        {
            var holon = new Holon { Id = Guid.NewGuid(), Name = $"OASIS IT Holon {Guid.NewGuid():N}" };

            var saved = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(saved.IsError, saved.Message);

            var loaded = await _provider.LoadHolonAsync(holon.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(holon.Name, loaded.Result!.Name);

            var deleted = await _provider.DeleteHolonAsync(holon.Id);
            Assert.IsFalse(deleted.IsError, deleted.Message);
            Assert.IsTrue(deleted.IsDeleted);
            Assert.AreEqual(1, deleted.DeletedCount);

            var missing = await _provider.LoadHolonAsync(holon.Id);
            Assert.IsTrue(missing.IsError);
            Assert.IsNull(missing.Result);

            var repeated = await _provider.DeleteHolonAsync(holon.Id);
            Assert.IsTrue(repeated.IsError);
            Assert.IsFalse(repeated.IsDeleted);
            Assert.AreEqual(0, repeated.DeletedCount);
        }

        [TestMethod]
        public async Task DeletedAvatar_IsNotReturnedAsLive()
        {
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
            var all = await _provider.LoadAllAvatarsAsync();
            Assert.IsNotNull(all);
        }

        [TestMethod]
        public async Task AvatarDetailRoundTripsWithGeneratedIdentity()
        {
            var marker = $"oasis-detail-{Guid.NewGuid():N}";
            var detail = new AvatarDetail { Id = Guid.Empty, Username = marker, Email = $"{marker}@example.test" };

            var saved = await _provider.SaveAvatarDetailAsync(detail);
            Assert.IsFalse(saved.IsError, saved.Message);
            Assert.IsTrue(saved.IsSaved);
            Assert.AreEqual(1, saved.SavedCount);
            Assert.AreNotEqual(Guid.Empty, detail.Id);

            var loaded = await _provider.LoadAvatarDetailAsync(detail.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.IsTrue(loaded.IsLoaded);
            Assert.AreEqual(1, loaded.LoadedCount);
            Assert.AreEqual(detail.Id, loaded.Result.Id);
            Assert.AreEqual(detail.Username, loaded.Result.Username);
        }

        [TestMethod]
        public async Task RetainedAvatarDetailCanBeRead()
        {
            var configuredId = Environment.GetEnvironmentVariable("OASIS_POCKETBASE_RETAINED_DETAIL_ID");
            if (string.IsNullOrWhiteSpace(configuredId))
                Assert.Inconclusive("Supply the retained PocketBase fixture avatar-detail ID.");

            Assert.IsTrue(Guid.TryParse(configuredId, out var id));
            var loaded = await _provider.LoadAvatarDetailAsync(id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.IsTrue(loaded.IsLoaded);
            Assert.AreEqual(1, loaded.LoadedCount);
            Assert.AreEqual(id, loaded.Result.Id);
            Assert.IsNotNull(loaded.Result.Chakras);
            Assert.IsNotNull(loaded.Result.Aura);
            Assert.IsNotNull(loaded.Result.Stats);
            Assert.IsNotNull(loaded.Result.Skills);
            Assert.IsNotNull(loaded.Result.Attributes);
            Assert.IsNotNull(loaded.Result.SuperPowers);
        }

        [TestMethod]
        public async Task HardDeleteAcknowledgesAndRemovesAvatar()
        {
            var marker = $"oasis-hard-delete-{Guid.NewGuid():N}";
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = marker, Email = $"{marker}@example.test" };

            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);
            Assert.IsTrue(saved.IsSaved);

            var deleted = await _provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
            Assert.IsFalse(deleted.IsError, deleted.Message);
            Assert.IsTrue(deleted.IsDeleted);
            Assert.AreEqual(1, deleted.DeletedCount);

            var repeated = await _provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
            Assert.IsTrue(repeated.IsError);
            Assert.IsFalse(repeated.IsDeleted);
            Assert.AreEqual(0, repeated.DeletedCount);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
