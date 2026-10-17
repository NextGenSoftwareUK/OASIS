using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.MemcachedOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MemcachedOASIS.UnitTests
{
    [TestClass]
    public class MemcachedOASISProviderTests
    {
        private MemcachedOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new MemcachedOASIS("localhost", 11211);
        }

        [TestMethod]
        public void ProviderType_ShouldBeMemcachedOASIS()
        {
            Assert.AreEqual(ProviderType.MemcachedOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeMemcachedOASIS()
        {
            Assert.AreEqual("MemcachedOASIS", _provider.ProviderName);
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
