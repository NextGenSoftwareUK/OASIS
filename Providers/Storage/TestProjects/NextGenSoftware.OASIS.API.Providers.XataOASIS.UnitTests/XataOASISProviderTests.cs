using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.XataOASIS;

namespace NextGenSoftware.OASIS.API.Providers.XataOASIS.UnitTests
{
    [TestClass]
    public class XataOASISProviderTests
    {
        private XataOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new XataOASIS("https://workspace.region.xata.sh/db/oasis:main", "APIKEY");
        }

        [TestMethod]
        public void ProviderType_ShouldBeXataOASIS()
        {
            Assert.AreEqual(ProviderType.XataOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeXataOASIS()
        {
            Assert.AreEqual("XataOASIS", _provider.ProviderName);
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
