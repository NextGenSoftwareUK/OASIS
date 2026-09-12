using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.CouchDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.CouchDBOASIS.UnitTests
{
    [TestClass]
    public class CouchDBOASISProviderTests
    {
        private CouchDBOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new CouchDBOASIS("http://localhost:5984", "admin", "password");
        }

        [TestMethod]
        public void ProviderType_ShouldBeCouchDBOASIS()
        {
            Assert.AreEqual(ProviderType.CouchDBOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeCouchDBOASIS()
        {
            Assert.AreEqual("CouchDBOASIS", _provider.ProviderName);
        }

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderDescription));
        }

        [TestMethod]
        public void GetProviderVersion_ShouldReturnValidVersion()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.GetProviderVersion()));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
