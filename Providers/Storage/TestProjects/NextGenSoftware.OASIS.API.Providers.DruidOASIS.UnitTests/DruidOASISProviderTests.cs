using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.DruidOASIS;

namespace NextGenSoftware.OASIS.API.Providers.DruidOASIS.UnitTests
{
    [TestClass]
    public class DruidOASISProviderTests
    {
        private DruidOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new DruidOASIS("http://localhost:8888");

        [TestMethod]
        public void ProviderType_ShouldBeDruidOASIS()
            => Assert.AreEqual(ProviderType.DruidOASIS, _provider.ProviderType.Value);

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
            => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderName_ShouldBeDruidOASIS()
            => Assert.AreEqual("DruidOASIS", _provider.ProviderName);

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
            => Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderDescription));

        [TestMethod]
        public void GetProviderVersion_ShouldReturnValidVersion()
            => Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.GetProviderVersion()));

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
