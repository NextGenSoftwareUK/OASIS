using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.TypesenseOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.IntegrationTests
{
    [TestClass]
    public class TypesenseOASISIntegrationTests
    {
        private TypesenseOASIS _provider = null!;
        [TestInitialize]
        public async Task Setup()
        {
            var host = Environment.GetEnvironmentVariable("TYPESENSE_HOST");
            var key = Environment.GetEnvironmentVariable("TYPESENSE_API_KEY");
            if (Environment.GetEnvironmentVariable("OASIS_TYPESENSE_STORAGE_TEST") != "1")
                Assert.Inconclusive("Enable writes only for a disposable Typesense instance.");

            Assert.IsTrue(Uri.TryCreate(host, UriKind.Absolute, out var endpoint) &&
                endpoint.IsLoopback && endpoint.Scheme == Uri.UriSchemeHttp &&
                endpoint.AbsolutePath == "/" && endpoint.UserInfo.Length == 0 &&
                endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0);
            Assert.IsFalse(string.IsNullOrWhiteSpace(key));

            _provider = new TypesenseOASIS(host!, key!);
            var active = await _provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message);
            Assert.IsTrue(active.Result);
        }

        [TestMethod]
        public async Task AvatarSaveAndHardDeleteRoundTrip()
        {
            var id = Guid.NewGuid();
            var avatar = new Avatar { Id = id, Username = $"typesense-{id:N}", Email = $"{id:N}@example.test" };
            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);
            Assert.IsTrue(saved.IsSaved);
            try
            {
                var loaded = await _provider.LoadAvatarAsync(id);
                Assert.IsFalse(loaded.IsError, loaded.Message);
                Assert.AreEqual(avatar.Username, loaded.Result.Username);
            }
            finally
            {
                var deleted = await _provider.DeleteAvatarAsync(id, softDelete: false);
                Assert.IsFalse(deleted.IsError, deleted.Message);
                Assert.IsTrue(deleted.IsDeleted);
                Assert.AreEqual(1, deleted.DeletedCount);
            }
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnAvatar()
        {
            var result = await _provider.LoadAvatarAsync(Guid.NewGuid());
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task HolonPointReadAndSoftDeleteRoundTrip()
        {
            var holon = new Holon { Id = Guid.NewGuid(), Name = "typesense-native-" + Guid.NewGuid().ToString("N") };
            var saved = await _provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsTrue(saved.IsSaved, saved.Message);
            try
            {
                var loaded = await _provider.LoadHolonAsync(holon.Id, loadChildren: false);
                Assert.IsTrue(loaded.IsLoaded, loaded.Message);
                Assert.AreEqual(1, loaded.LoadedCount);
                Assert.AreEqual(holon.Name, loaded.Result.Name);

                var deleted = await _provider.DeleteHolonSoftAsync(holon.Id);
                Assert.IsTrue(deleted.IsDeleted, deleted.Message);
                Assert.AreEqual(1, deleted.DeletedCount);
                var hidden = await _provider.LoadHolonAsync(holon.Id, loadChildren: false);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, hidden.ErrorCode);
            }
            finally
            {
                var removed = await _provider.DeleteHolonSoftAsync(holon.Id, softDelete: false);
                Assert.IsTrue(removed.IsDeleted, removed.Message);
                var repeated = await _provider.DeleteHolonSoftAsync(holon.Id, softDelete: false);
                Assert.IsTrue(repeated.IsError, "A second native hard delete must reject the absent fixture.");
                Assert.IsFalse(repeated.IsDeleted);
                Assert.AreEqual(0, repeated.DeletedCount);
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
