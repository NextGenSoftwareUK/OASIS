using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.LitestreamOASIS;

namespace NextGenSoftware.OASIS.API.Providers.LitestreamOASIS.UnitTests
{
    [TestClass]
    public class LitestreamOASISProviderTests
    {
        private LitestreamOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new LitestreamOASIS(Path.Combine(Path.GetTempPath(), "oasis-test.db"));
        }

        [TestMethod]
        public void ProviderType_ShouldBeLitestreamOASIS()
        {
            Assert.AreEqual(ProviderType.LitestreamOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeLitestreamOASIS()
        {
            Assert.AreEqual("LitestreamOASIS", _provider.ProviderName);
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
