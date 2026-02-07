using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.PolygonOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests
{
    [TestClass]
    public class Web3CoreOASISProviderTests
    {
        private PolygonOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new PolygonOASIS("http://localhost:8545", "", "0x0000000000000000000000000000000000000000");
        }

        [TestMethod]
        public void ProviderType_ShouldBePolygonOASIS()
        {
            var providerType = _provider.ProviderType;
            Assert.IsNotNull(providerType);
            Assert.AreEqual(ProviderType.PolygonOASIS, providerType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            var isActivated = _provider.IsProviderActivated;
            Assert.IsFalse(isActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBePolygonOASIS()
        {
            var providerName = _provider.ProviderName;
            Assert.AreEqual("PolygonOASIS", providerName);
        }

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
        {
            var description = _provider.ProviderDescription;
            Assert.IsNotNull(description);
            Assert.IsFalse(string.IsNullOrEmpty(description));
        }

        [TestMethod]
        public void ActivateProvider_ShouldSetIsProviderActivatedToTrue()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
            var result = _provider.ActivateProvider();
            Assert.IsFalse(result.IsError);
            Assert.IsTrue(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void DeActivateProvider_ShouldSetIsProviderActivatedToFalse()
        {
            _provider.ActivateProvider();
            Assert.IsTrue(_provider.IsProviderActivated);
            var result = _provider.DeActivateProvider();
            Assert.IsFalse(result.IsError);
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
            {
                _provider.DeActivateProvider();
            }
        }
    }
}
