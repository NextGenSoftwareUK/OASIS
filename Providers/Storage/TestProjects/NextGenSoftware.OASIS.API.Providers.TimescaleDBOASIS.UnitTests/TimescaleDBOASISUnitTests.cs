using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.TimescaleDBOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.TimescaleDBOASIS.UnitTests
{
    [TestClass]
    public class TimescaleDBOASISProviderTests
    {
        private TimescaleDBOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new TimescaleDBOASIS("Host=localhost;Port=5432;Database=oasis;Username=postgres;Password=test");
        }

        [TestMethod]
        public void ProviderType_ShouldBeTimescaleDBOASIS()
        {
            Assert.AreEqual(ProviderType.TimescaleDBOASIS, _provider.ProviderType);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeTimescaleDBOASIS()
        {
            Assert.AreEqual("TimescaleDBOASIS", _provider.ProviderName);
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
