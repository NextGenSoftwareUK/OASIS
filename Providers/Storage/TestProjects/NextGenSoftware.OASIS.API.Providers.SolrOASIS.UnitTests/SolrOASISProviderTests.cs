using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.SolrOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SolrOASIS.UnitTests
{
    [TestClass]
    public class SolrOASISProviderTests
    {
        private SolrOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new SolrOASIS("http://localhost:8983");
        }

        [TestMethod]
        public void ProviderType_ShouldBeSolrOASIS()
        {
            Assert.AreEqual(ProviderType.SolrOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeSolrOASIS()
        {
            Assert.AreEqual("SolrOASIS", _provider.ProviderName);
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
