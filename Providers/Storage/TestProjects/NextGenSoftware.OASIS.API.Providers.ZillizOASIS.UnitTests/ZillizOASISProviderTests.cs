using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.ZillizOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ZillizOASIS.UnitTests
{
    [TestClass]
    public class ZillizOASISProviderTests
    {
        private ZillizOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new ZillizOASIS("https://in01-abc.aws-us-west-2.vectordb.zillizcloud.com", "APIKEY");
        }

        [TestMethod]
        public void ProviderType_ShouldBeZillizOASIS()
        {
            Assert.AreEqual(ProviderType.ZillizOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeZillizOASIS()
        {
            Assert.AreEqual("ZillizOASIS", _provider.ProviderName);
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
