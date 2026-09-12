using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS.UnitTests
{
    [TestClass]
    public class ClickHouseOASISProviderTests
    {
        private ClickHouseOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new ClickHouseOASIS("Host=localhost;Port=8123;Database=oasis;Username=default;Password=");
        }

        [TestMethod]
        public void ProviderType_ShouldBeClickHouseOASIS()
        {
            Assert.AreEqual(ProviderType.ClickHouseOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeClickHouseOASIS()
        {
            Assert.AreEqual("ClickHouseOASIS", _provider.ProviderName);
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
