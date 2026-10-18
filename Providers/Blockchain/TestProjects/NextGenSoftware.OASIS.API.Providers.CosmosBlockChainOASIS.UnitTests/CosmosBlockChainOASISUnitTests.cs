using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests
{
    [TestClass]
    public class CosmosBlockChainOASISProviderTests
    {
        private CosmosBlockChainOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new CosmosBlockChainOASIS();
        }

        [TestMethod]
        public void ProviderType_ShouldBeCosmosBlockChainOASIS()
        {
            // Arrange & Act
            var providerType = _provider.ProviderType;

            // Assert
            Assert.AreEqual(ProviderType.CosmosBlockChainOASIS, providerType.Value);
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
        public void ProviderName_ShouldBeCosmosBlockChainOASIS()
        {
            // Arrange & Act
            var providerName = _provider.ProviderName;

            // Assert
            Assert.AreEqual("CosmosBlockChainOASIS", providerName);
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
        public void UnconfiguredOwnerActivationMustFailWithoutClaimingWritableStorage()
        {
            using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1",storageEncryptionKeyResolver:()=>new byte[32]);
            // Arrange
            Assert.IsFalse(_provider.IsProviderActivated);

            // Act
            var result = provider.ActivateProvider();

            // Assert
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message,"owner signing key");
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void FailedActivationCanBeDeactivatedWithoutClaimingItWasActive()
        {
            // Arrange
            var activation = _provider.ActivateProvider();
            Assert.IsTrue(activation.IsError);
            Assert.IsFalse(_provider.IsProviderActivated);

            // Act
            var result = _provider.DeActivateProvider();

            // Assert
            Assert.IsTrue(result.IsError == false);
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeNonEmpty()
        {
            var name = _provider.ProviderName;
            Assert.IsNotNull(name);
            Assert.IsFalse(string.IsNullOrEmpty(name));
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
