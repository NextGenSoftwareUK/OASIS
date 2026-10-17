using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS;

namespace NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS.UnitTests
{
    [TestClass]
    public class OpenSearchOASISProviderTests
    {
        private OpenSearchOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new OpenSearchOASIS("http://localhost:9200");
        }

        [TestMethod]
        public void ProviderType_ShouldBeOpenSearchOASIS()
        {
            Assert.AreEqual(ProviderType.OpenSearchOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeOpenSearchOASIS()
        {
            Assert.AreEqual("OpenSearchOASIS", _provider.ProviderName);
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
