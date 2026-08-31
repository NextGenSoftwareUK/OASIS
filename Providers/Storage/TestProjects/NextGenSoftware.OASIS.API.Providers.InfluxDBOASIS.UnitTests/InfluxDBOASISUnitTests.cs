using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.UnitTests
{
    [TestClass]
    public class InfluxDBOASISProviderTests
    {
        private InfluxDBOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new InfluxDBOASIS("http://localhost:8086", "test-token", "oasis-org", "oasis-bucket");
        }

        [TestMethod]
        public void ProviderType_ShouldBeInfluxDBOASIS()
        {
            Assert.AreEqual(ProviderType.InfluxDBOASIS, _provider.ProviderType);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeInfluxDBOASIS()
        {
            Assert.AreEqual("InfluxDBOASIS", _provider.ProviderName);
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
