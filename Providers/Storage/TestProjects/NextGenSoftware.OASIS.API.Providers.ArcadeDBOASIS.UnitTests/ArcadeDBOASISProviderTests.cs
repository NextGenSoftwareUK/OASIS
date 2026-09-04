using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.ArcadeDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ArcadeDBOASIS.UnitTests
{
    [TestClass]
    public class ArcadeDBOASISProviderTests
    {
        private ArcadeDBOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new ArcadeDBOASIS("http://localhost:2480", "root", "playwithdata");
        }

        [TestMethod]
        public void ProviderType_ShouldBeArcadeDBOASIS()
        {
            Assert.AreEqual(ProviderType.ArcadeDBOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeArcadeDBOASIS()
        {
            Assert.AreEqual("ArcadeDBOASIS", _provider.ProviderName);
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
