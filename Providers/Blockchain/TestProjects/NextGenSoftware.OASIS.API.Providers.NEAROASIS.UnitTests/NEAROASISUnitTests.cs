using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.NEAROASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests
{
    [TestClass]
    public class NEAROASISProviderTests
    {
        private NEAROASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new NEAROASIS();
        }

        [TestMethod]
        public void ProviderType_ShouldBeNEAROASIS()
        {
            // Arrange & Act
            var providerType = _provider.ProviderType;

            // Assert
            Assert.AreEqual(ProviderType.NEAROASIS, providerType.Value);
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
        public void ProviderName_ShouldBeNEAROASIS()
        {
            // Arrange & Act
            var providerName = _provider.ProviderName;

            // Assert
            Assert.AreEqual("NEAROASIS", providerName);
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
        public void ActivateProvider_ShouldSetIsProviderActivatedToTrue()
        {
            // Arrange
            Assert.IsFalse(_provider.IsProviderActivated);

            // Act
            var result = _provider.ActivateProvider();

            // Assert
            Assert.IsTrue(result.IsError == false);
            Assert.IsTrue(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void DeActivateProvider_ShouldSetIsProviderActivatedToFalse()
        {
            // Arrange
            _provider.ActivateProvider();
            Assert.IsTrue(_provider.IsProviderActivated);

            // Act
            var result = _provider.DeActivateProvider();

            // Assert
            Assert.IsTrue(result.IsError == false);
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderVersion_ShouldBeSetViaProviderName()
        {
            // Arrange & Act
            var name = _provider.ProviderName;

            // Assert
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
