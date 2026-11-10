using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.WeaviateOASIS;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.WeaviateOASIS.IntegrationTests
{
    [TestClass]
    public class WeaviateOASISIntegrationTests
    {
        private WeaviateOASIS _provider;

        [TestInitialize]
        public async Task Setup()
        {
            if (Environment.GetEnvironmentVariable("OASIS_WEAVIATE_STORAGE_TEST") != "1")
                Assert.Inconclusive("Enable writes only for a disposable Weaviate instance.");
            var host = Environment.GetEnvironmentVariable("WEAVIATE_HOST");
            Assert.IsTrue(Uri.TryCreate(host, UriKind.Absolute, out var endpoint) &&
                endpoint.IsLoopback && endpoint.Scheme == Uri.UriSchemeHttp &&
                endpoint.AbsolutePath == "/" && endpoint.UserInfo.Length == 0 &&
                endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0);
            _provider = new WeaviateOASIS(host!);
            var activated = await _provider.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Message);
        }

        [TestMethod]
        public async Task AvatarHardDeleteAcknowledgesOnlyAnExistingRecord()
        {
            var id = Guid.NewGuid();
            var avatar = new Avatar { Id = id, Username = $"weaviate-{id:N}", Email = $"{id:N}@example.test" };
            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);
            try
            {
                var loaded = await _provider.LoadAvatarAsync(id);
                Assert.IsFalse(loaded.IsError, loaded.Message);
                Assert.AreEqual(id, loaded.Result.Id);
            }
            finally
            {
                var deleted = await _provider.DeleteAvatarAsync(id, softDelete: false);
                Assert.IsFalse(deleted.IsError, deleted.Message);
                Assert.IsTrue(deleted.IsDeleted);
                Assert.AreEqual(1, deleted.DeletedCount);
            }

            var repeated = await _provider.DeleteAvatarAsync(id, softDelete: false);
            Assert.IsTrue(repeated.IsError);
            Assert.IsFalse(repeated.IsDeleted);
            Assert.AreEqual(0, repeated.DeletedCount);
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnAvatar()
        {
            var result = await _provider.LoadAvatarAsync(Guid.NewGuid());
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task LoadAllHolonsReturnsRecordsAcrossCursorPages()
        {
            var created = new List<Guid>();
            var cleanupFailures = new List<string>();
            try
            {
                for (var i = 0; i < 101; i++)
                {
                    var id = Guid.NewGuid();
                    var result = await _provider.SaveHolonAsync(new Holon
                    {
                        Id = id,
                        Name = $"weaviate-pagination-{id:N}"
                    });
                    Assert.IsFalse(result.IsError, result.Message);
                    created.Add(id);
                }

                var loaded = await _provider.LoadAllHolonsAsync(loadChildren: false);
                Assert.IsFalse(loaded.IsError, loaded.Message);
                Assert.IsTrue(loaded.IsLoaded);
                var ids = loaded.Result.Select(holon => holon.Id).ToList();
                Assert.AreEqual(ids.Count, loaded.LoadedCount);
                CollectionAssert.AreEquivalent(created, ids.Where(created.Contains).ToList());
            }
            finally
            {
                foreach (var id in created)
                {
                    var deleted = await _provider.DeleteHolonAsync(id);
                    if (deleted.IsError || !deleted.IsDeleted)
                        cleanupFailures.Add($"{id}: {deleted.Message}");
                }
            }

            Assert.AreEqual(0, cleanupFailures.Count, string.Join(Environment.NewLine, cleanupFailures));
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
