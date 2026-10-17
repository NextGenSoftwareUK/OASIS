using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.PineconeOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PineconeOASIS.UnitTests
{
    [TestClass]
    public class PineconeOASISProviderTests
    {
        private PineconeOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new PineconeOASIS("https://oasis-storage-abc123.svc.us-east-1-aws.pinecone.io", "APIKEY");
        }

        [TestMethod]
        public void ProviderType_ShouldBePineconeOASIS()
        {
            Assert.AreEqual(ProviderType.PineconeOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBePineconeOASIS()
        {
            Assert.AreEqual("PineconeOASIS", _provider.ProviderName);
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
