using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests
{
    [TestClass]
    public class AzureCosmosDBOASISProviderTests
    {
        private AzureCosmosDBOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new AzureCosmosDBOASIS(new Uri("https://localhost:8081"), "testKey", "testDb", new List<string> { "testCollection" });
        }

        [TestMethod]
        public void ProviderType_ShouldBeAzureCosmosDBOASIS()
        {
            // Arrange & Act
            var providerType = _provider.ProviderType.Value;

            // Assert
            Assert.AreEqual(ProviderType.AzureCosmosDBOASIS, providerType);
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
        public void ProviderName_ShouldBeAzureCosmosDBOASIS()
        {
            // Arrange & Act
            var providerName = _provider.ProviderName;

            // Assert
            Assert.AreEqual("AzureCosmosDBOASIS", providerName);
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
        public void ProviderName_ShouldNotBeEmpty()
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
