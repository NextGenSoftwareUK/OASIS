using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests
{
    [TestClass]
    public class DynamoDBOASISProviderTests
    {
        private DynamoDBOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new DynamoDBOASIS("test-key", "test-secret", "us-east-1");
        }

        [TestMethod]
        public void ProviderType_ShouldBeDynamoDBOASIS()
        {
            Assert.AreEqual(ProviderType.DynamoDBOASIS, _provider.ProviderType);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeDynamoDBOASIS()
        {
            Assert.AreEqual("DynamoDBOASIS", _provider.ProviderName);
        }

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
        {
            Assert.IsNotNull(_provider.ProviderDescription);
            Assert.IsFalse(string.IsNullOrEmpty(_provider.ProviderDescription));
        }

        [TestMethod]
        public void GetProviderVersion_ShouldReturnValidVersion()
        {
            var version = _provider.GetProviderVersion();
            Assert.IsNotNull(version);
            Assert.IsFalse(string.IsNullOrEmpty(version));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
