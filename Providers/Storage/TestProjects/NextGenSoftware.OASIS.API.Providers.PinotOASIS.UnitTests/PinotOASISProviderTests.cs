using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.PinotOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PinotOASIS.UnitTests
{
    [TestClass]
    public class PinotOASISProviderTests
    {
        private PinotOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new PinotOASIS("http://localhost:8000", "http://localhost:9000");

        [TestMethod]
        public void ProviderType_ShouldBePinotOASIS()
            => Assert.AreEqual(ProviderType.PinotOASIS, _provider.ProviderType.Value);

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
            => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderName_ShouldBePinotOASIS()
            => Assert.AreEqual("PinotOASIS", _provider.ProviderName);

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
