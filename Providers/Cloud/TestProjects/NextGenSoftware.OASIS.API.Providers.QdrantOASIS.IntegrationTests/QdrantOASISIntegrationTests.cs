using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.QdrantOASIS;
using NextGenSoftware.OASIS.API.Core.Holons;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.QdrantOASIS.IntegrationTests
{
    [TestClass]
    public class QdrantOASISIntegrationTests
    {
        private QdrantOASIS _provider;


        [TestInitialize]
        public async Task Setup()
        {
            if (Environment.GetEnvironmentVariable("OASIS_QDRANT_STORAGE_TEST") != "1")
                Assert.Inconclusive("Enable writes only for a disposable Qdrant instance.");
            var host = Environment.GetEnvironmentVariable("QDRANT_HOST");
            Assert.IsTrue(Uri.TryCreate(host, UriKind.Absolute, out var endpoint) &&
                endpoint.IsLoopback && endpoint.Scheme == Uri.UriSchemeHttp &&
                endpoint.AbsolutePath == "/" && endpoint.UserInfo.Length == 0 &&
                endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0);
            _provider = new QdrantOASIS(host!);
            var activated = await _provider.ActivateProviderAsync();
            Assert.IsFalse(activated.IsError, activated.Message);
        }

        [TestMethod]
        public async Task HolonRoundTripsAndSoftDeleteHidesIt()
        {
            var holon = new Holon { Id = Guid.NewGuid(), Name = "Qdrant native lifecycle" };
            var saved = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(saved.IsError, saved.Message);
            try
            {
                var loaded = await _provider.LoadHolonAsync(holon.Id);
                Assert.IsFalse(loaded.IsError, loaded.Message);
                Assert.IsTrue(loaded.IsLoaded);
                Assert.AreEqual(1, loaded.LoadedCount);
                Assert.IsNotNull(loaded.Result);
                Assert.AreEqual(holon.Id, loaded.Result.Id);
                Assert.AreEqual(holon.Name, loaded.Result.Name);
            }
            finally
            {
                var deleted = await _provider.DeleteHolonAsync(holon.Id);
                Assert.IsFalse(deleted.IsError, deleted.Message);
                Assert.IsTrue(deleted.IsDeleted);
                Assert.AreEqual(1, deleted.DeletedCount);
                Assert.IsTrue(deleted.Result.IsDeleted);
            }

            var missing = await _provider.LoadHolonAsync(holon.Id);
            Assert.IsTrue(missing.IsError);
            Assert.IsNull(missing.Result);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
