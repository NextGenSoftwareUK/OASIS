using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.AlgoliaOASIS;

namespace NextGenSoftware.OASIS.API.Providers.AlgoliaOASIS.UnitTests
{
    [TestClass]
    public class AlgoliaOASISProviderTests
    {
        private AlgoliaOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new AlgoliaOASIS("APPID", "APIKEY");
        }

        [TestMethod]
        public void ProviderType_ShouldBeAlgoliaOASIS()
        {
            Assert.AreEqual(ProviderType.AlgoliaOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeAlgoliaOASIS()
        {
            Assert.AreEqual("AlgoliaOASIS", _provider.ProviderName);
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
