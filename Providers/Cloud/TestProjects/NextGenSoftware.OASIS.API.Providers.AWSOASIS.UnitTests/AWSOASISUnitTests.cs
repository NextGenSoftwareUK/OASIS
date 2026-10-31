using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.AWSOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests
{
    [TestClass]
    public class AWSOASISProviderTests
    {
        private AWSOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new AWSOASIS("us-east-1","owned-local","owned-local","http://127.0.0.1:1");
        }

        [TestMethod]
        public void ProviderType_ShouldBeAWSOASIS()
        {
            // Arrange & Act
            var providerType = _provider.ProviderType;

            // Assert
            Assert.AreEqual(ProviderType.AWSOASIS, providerType.Value);
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
        public void ProviderName_ShouldBeAWSOASIS()
        {
            // Arrange & Act
            var providerName = _provider.ProviderName;

            // Assert
            Assert.AreEqual("AWSOASIS", providerName);
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

        [TestMethod]
        public void ProviderImplementsActualStorageAndNearbyContracts()
        {
            // Arrange
            Assert.IsFalse(_provider.IsProviderActivated);

            Assert.IsInstanceOfType(_provider,typeof(IOASISStorageProvider));
            Assert.IsInstanceOfType(_provider,typeof(IOASISNETProvider));
            // Successful activation is verified by the owned real DynamoDB suite, not an offline stub.
        }

        [TestMethod]
        public void DeActivateProvider_ShouldSetIsProviderActivatedToFalse()
        {
            // Arrange
            Assert.IsFalse(_provider.IsProviderActivated);

            // Act
            var result = _provider.DeActivateProvider();

            // Assert
            Assert.IsTrue(result.IsError == false);
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void DeactivationIsIdempotentWithoutNetworkAccess()
        {
            Assert.IsFalse(_provider.DeActivateProvider().IsError);
            Assert.IsFalse(_provider.DeActivateProvider().IsError);
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null)
            {
                _provider.DeActivateProvider();
            }
        }
    }
}
