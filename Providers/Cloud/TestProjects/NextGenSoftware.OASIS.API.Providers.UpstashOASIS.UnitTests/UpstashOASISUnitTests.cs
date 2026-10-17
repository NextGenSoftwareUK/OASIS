using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.UpstashOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.UpstashOASIS.UnitTests
{
    [TestClass]
    public class UpstashOASISProviderTests
    {
        private UpstashOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new UpstashOASIS("https://test.upstash.io", "test-token");
        }

        [TestMethod]
        public void ProviderType_ShouldBeUpstashOASIS()
        {
            Assert.AreEqual(ProviderType.UpstashOASIS, _provider.ProviderType);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeUpstashOASIS()
        {
            Assert.AreEqual("UpstashOASIS", _provider.ProviderName);
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
