using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.FilecoinOASIS;

namespace NextGenSoftware.OASIS.API.Providers.FilecoinOASIS.IntegrationTests
{
    [TestClass]
    public class FilecoinOASISIntegrationTests
    {
        private FilecoinOASIS _provider = null!;

        [TestInitialize]
        public async Task Setup()
        {
            if (Environment.GetEnvironmentVariable("OASIS_FILECOIN_TEST_LOTUS_WRITES") != "1")
                Assert.Inconclusive("Enable writes only on a dedicated disposable Lotus fixture.");

            var endpoint = Environment.GetEnvironmentVariable("OASIS_FILECOIN_TEST_LOTUS_RPC");
            Assert.IsTrue(Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) &&
                uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp && string.IsNullOrEmpty(uri.UserInfo));
            _provider = new FilecoinOASIS(endpoint, Environment.GetEnvironmentVariable("OASIS_FILECOIN_TEST_LOTUS_TOKEN") ?? string.Empty);
            var activation = await _provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Exception?.GetType().FullName);
            Assert.IsTrue(activation.Result && _provider.IsProviderActivated);
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
        public async Task SaveAndLoadHolon_RoundTrips()
        {
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

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }

    [TestClass]
    public class FilecoinLotusActivationTests
    {
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task ActivatesAgainstDedicatedLotusNode(bool synchronous)
        {
            var endpoint = Environment.GetEnvironmentVariable("OASIS_FILECOIN_TEST_LOTUS_RPC");
            if (string.IsNullOrWhiteSpace(endpoint))
                Assert.Inconclusive("Configure the dedicated read-only Lotus fixture endpoint.");

            Assert.IsTrue(Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) &&
                uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp && string.IsNullOrEmpty(uri.UserInfo));
            var provider = new FilecoinOASIS(endpoint, Environment.GetEnvironmentVariable("OASIS_FILECOIN_TEST_LOTUS_TOKEN") ?? string.Empty);
            try
            {
                var activation = synchronous ? provider.ActivateProvider() : await provider.ActivateProviderAsync();
                Assert.IsFalse(activation.IsError, activation.Exception?.GetType().FullName);
                Assert.IsTrue(activation.Result);
                Assert.IsTrue(provider.IsProviderActivated);
            }
            finally
            {
                var deactivation = synchronous ? provider.DeActivateProvider() : await provider.DeActivateProviderAsync();
                Assert.IsTrue(deactivation.Result);
                Assert.IsFalse(provider.IsProviderActivated);
            }
        }
    }
}
