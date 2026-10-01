using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.MarqoOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MarqoOASIS.UnitTests
{
    [TestClass]
    public class MarqoOASISProviderTests
    {
        private MarqoOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new MarqoOASIS("http://localhost:8882");
        }

        [TestMethod]
        public void ProviderType_ShouldBeMarqoOASIS()
        {
            Assert.AreEqual(ProviderType.MarqoOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeMarqoOASIS()
        {
            Assert.AreEqual("MarqoOASIS", _provider.ProviderName);
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
