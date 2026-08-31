using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS.UnitTests
{
    [TestClass]
    public class MeilisearchOASISProviderTests
    {
        private MeilisearchOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new MeilisearchOASIS("http://localhost:7700");
        }

        [TestMethod]
        public void ProviderType_ShouldBeMeilisearchOASIS()
        {
            Assert.AreEqual(ProviderType.MeilisearchOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeMeilisearchOASIS()
        {
            Assert.AreEqual("MeilisearchOASIS", _provider.ProviderName);
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
