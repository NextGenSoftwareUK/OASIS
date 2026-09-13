using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.ArangoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ArangoDBOASIS.UnitTests
{
    [TestClass]
    public class ArangoDBOASISProviderTests
    {
        private ArangoDBOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new ArangoDBOASIS("http://localhost:8529");
        }

        [TestMethod]
        public void ProviderType_ShouldBeArangoDBOASIS()
        {
            Assert.AreEqual(ProviderType.ArangoDBOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeArangoDBOASIS()
        {
            Assert.AreEqual("ArangoDBOASIS", _provider.ProviderName);
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
