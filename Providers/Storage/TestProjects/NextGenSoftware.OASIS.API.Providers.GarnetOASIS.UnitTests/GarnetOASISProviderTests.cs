using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.GarnetOASIS;

namespace NextGenSoftware.OASIS.API.Providers.GarnetOASIS.UnitTests
{
    [TestClass]
    public class GarnetOASISProviderTests
    {
        private GarnetOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new GarnetOASIS("localhost:3278");

        [TestMethod]
        public void ProviderType_ShouldBeGarnetOASIS()
            => Assert.AreEqual(ProviderType.GarnetOASIS, _provider.ProviderType.Value);

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
            => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderName_ShouldBeGarnetOASIS()
            => Assert.AreEqual("GarnetOASIS", _provider.ProviderName);

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
