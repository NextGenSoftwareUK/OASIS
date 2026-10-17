using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.UnitTests
{
    [TestClass]
    public class ElasticsearchOASISProviderTests
    {
        private ElasticsearchOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new ElasticsearchOASIS("http://localhost:9200");
        }

        [TestMethod]
        public void ProviderType_ShouldBeElasticsearchOASIS()
        {
            Assert.AreEqual(ProviderType.ElasticsearchOASIS, _provider.ProviderType);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeElasticsearchOASIS()
        {
            Assert.AreEqual("ElasticsearchOASIS", _provider.ProviderName);
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
