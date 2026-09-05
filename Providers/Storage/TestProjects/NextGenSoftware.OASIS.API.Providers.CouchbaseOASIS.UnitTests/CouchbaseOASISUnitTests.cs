using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS.UnitTests
{
    [TestClass]
    public class CouchbaseOASISProviderTests
    {
        private CouchbaseOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new CouchbaseOASIS("couchbase://localhost", "Administrator", "password");
        }

        [TestMethod]
        public void ProviderType_ShouldBeCouchbaseOASIS()
        {
            Assert.AreEqual(ProviderType.CouchbaseOASIS, _provider.ProviderType);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeCouchbaseOASIS()
        {
            Assert.AreEqual("CouchbaseOASIS", _provider.ProviderName);
        }

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
        {
            Assert.IsNotNull(_provider.ProviderDescription);
            Assert.IsFalse(string.IsNullOrEmpty(_provider.ProviderDescription));
        }

        [TestMethod]
        public void GetProviderVersion_ShouldReturnValidVersion()
        {
            var version = _provider.GetProviderVersion();
            Assert.IsNotNull(version);
            Assert.IsFalse(string.IsNullOrEmpty(version));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
