using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.AptosOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS.UnitTests
{
    [TestClass]
    public class AptosOASISProviderTests
    {
        private AptosOASIS _aptosProvider;

        [TestInitialize]
        public void Setup()
        {
            _aptosProvider = new AptosOASIS();
        }

        [TestMethod]
        public void ProviderType_ShouldBeAptosOASIS()
        {
            // Arrange & Act
            var providerType = _aptosProvider.ProviderType;

            // Assert
            Assert.AreEqual(ProviderType.AptosOASIS, providerType);
        }

        [TestMethod]
        public void ProviderCategory_ShouldBeBlockchain()
        {
            // Arrange & Act
            var category = _aptosProvider.ProviderCategory;

            // Assert
            Assert.AreEqual(ProviderCategory.Blockchain, category);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            // Arrange & Act
            var isActivated = _aptosProvider.IsProviderActivated;

            // Assert
            Assert.IsFalse(isActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeAptosOASIS()
        {
            // Arrange & Act
            var providerName = _aptosProvider.ProviderName;

            // Assert
            Assert.AreEqual("AptosOASIS", providerName);
        }

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
        {
            // Arrange & Act
            var description = _aptosProvider.ProviderDescription;

            // Assert
            Assert.IsNotNull(description);
            Assert.IsFalse(string.IsNullOrEmpty(description));
        }

        [TestMethod]
        public void ActivateProvider_ShouldSetIsProviderActivatedToTrue()
        {
            // Arrange
            Assert.IsFalse(_aptosProvider.IsProviderActivated);

            // Act
            var result = _aptosProvider.ActivateProvider();

            // Assert
            Assert.IsTrue(result.IsError == false);
            Assert.IsTrue(_aptosProvider.IsProviderActivated);
        }

        [TestMethod]
        public void DeActivateProvider_ShouldSetIsProviderActivatedToFalse()
        {
            // Arrange
            _aptosProvider.ActivateProvider();
            Assert.IsTrue(_aptosProvider.IsProviderActivated);

            // Act
            var result = _aptosProvider.DeActivateProvider();

            // Assert
            Assert.IsTrue(result.IsError == false);
            Assert.IsFalse(_aptosProvider.IsProviderActivated);
        }

        [TestMethod]
        public void GetProviderVersion_ShouldReturnValidVersion()
        {
            // Arrange & Act
            var version = _aptosProvider.GetProviderVersion();

            // Assert
            Assert.IsNotNull(version);
            Assert.IsFalse(string.IsNullOrEmpty(version));
        }

        [TestMethod]
        public void GetProviderType_ShouldReturnAptosOASIS()
        {
            // Arrange & Act
            var providerType = _aptosProvider.GetProviderType();

            // Assert
            Assert.AreEqual(ProviderType.AptosOASIS, providerType);
        }

        [TestMethod]
        public void GetProviderCategory_ShouldReturnBlockchain()
        {
            // Arrange & Act
            var category = _aptosProvider.GetProviderCategory();

            // Assert
            Assert.AreEqual(ProviderCategory.Blockchain, category);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_aptosProvider != null && _aptosProvider.IsProviderActivated)
            {
                _aptosProvider.DeActivateProvider();
            }
        }
    }
}
