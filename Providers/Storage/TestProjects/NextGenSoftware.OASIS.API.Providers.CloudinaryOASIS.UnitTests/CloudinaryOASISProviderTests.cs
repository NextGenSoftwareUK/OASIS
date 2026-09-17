using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.CloudinaryOASIS;

namespace NextGenSoftware.OASIS.API.Providers.CloudinaryOASIS.UnitTests
{
    [TestClass]
    public class CloudinaryOASISProviderTests
    {
        private CloudinaryOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new CloudinaryOASIS("test-cloud", "test-api-key", "test-api-secret");
        }

        [TestMethod]
        public void ProviderType_ShouldBeCloudinaryOASIS()
        {
            Assert.AreEqual(ProviderType.CloudinaryOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeCloudinaryOASIS()
        {
            Assert.AreEqual("CloudinaryOASIS", _provider.ProviderName);
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
