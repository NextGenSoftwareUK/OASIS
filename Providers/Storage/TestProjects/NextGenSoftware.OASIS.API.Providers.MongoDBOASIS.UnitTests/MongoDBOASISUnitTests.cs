using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests
{
    [TestClass]
    public class MongoDBOASISProviderTests
    {
        private MongoProvider _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            // Construction is deliberately backend-free. Activation belongs to the
            // disposable integration profile because it opens a real MongoDB connection.
            _provider = new MongoProvider("mongodb://127.0.0.1:27017", "oasis-unit-tests");
        }

        [TestMethod]
        public void ProviderType_ShouldBeMongoDBOASIS()
        {
            // Arrange & Act
            var providerType = _provider.ProviderType;

            // Assert
            Assert.AreEqual(ProviderType.MongoDBOASIS, providerType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            // Arrange & Act
            var isActivated = _provider.IsProviderActivated;

            // Assert
            Assert.IsFalse(isActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeMongoDBOASIS()
        {
            // Arrange & Act
            var providerName = _provider.ProviderName;

            // Assert
            Assert.AreEqual("MongoDBOASIS", providerName);
        }

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
        {
            // Arrange & Act
            var description = _provider.ProviderDescription;

            // Assert
            Assert.IsNotNull(description);
            Assert.IsFalse(string.IsNullOrEmpty(description));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
            {
                _provider.DeActivateProvider();
            }
        }
    }
}
