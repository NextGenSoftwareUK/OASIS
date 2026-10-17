using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.LanceDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.LanceDBOASIS.UnitTests
{
    [TestClass]
    public class LanceDBOASISProviderTests
    {
        private LanceDBOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new LanceDBOASIS("https://db.lancedb.com", "APIKEY", "oasis");
        }

        [TestMethod]
        public void ProviderType_ShouldBeLanceDBOASIS()
        {
            Assert.AreEqual(ProviderType.LanceDBOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeLanceDBOASIS()
        {
            Assert.AreEqual("LanceDBOASIS", _provider.ProviderName);
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
