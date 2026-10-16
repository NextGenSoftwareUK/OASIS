using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests
{
    [TestClass]
    public class HashgraphOASISProviderTests
    {
        private HashgraphOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new HashgraphOASIS();
        }

        [TestMethod]
        public void ProviderType_ShouldBeHashgraphOASIS()
        {
            // Arrange & Act
            var providerType = _provider.ProviderType;

            // Assert
            Assert.AreEqual(ProviderType.HashgraphOASIS, providerType.Value);
        }

        [DataTestMethod]
        [DataRow(false)] [DataRow(true)]
        public async Task CredentialValidationUsesSharedInterfaceBeforeAnyActivation(bool synchronous)
        {
            var storage=(IOASISStorageProvider)_provider;
            foreach(var input in new[] {(" ",0),("owned-synthetic",-1)})
            foreach(var kind in new[] {0,1,2,3,4})
            {
                var result=kind switch {
                    0=>synchronous ? storage.LoadAvatarByVerificationToken(input.Item1,input.Item2):await storage.LoadAvatarByVerificationTokenAsync(input.Item1,input.Item2),
                    1=>synchronous ? storage.LoadAvatarByResetToken(input.Item1,input.Item2):await storage.LoadAvatarByResetTokenAsync(input.Item1,input.Item2),
                    2=>synchronous ? storage.LoadAvatarByRefreshToken(input.Item1,input.Item2):await storage.LoadAvatarByRefreshTokenAsync(input.Item1,input.Item2),
                    3=>synchronous ? storage.LoadAvatarByPublicKey(input.Item1,input.Item2):await storage.LoadAvatarByPublicKeyAsync(input.Item1,input.Item2),
                    _=>synchronous ? storage.LoadAvatarByPrivateKey(input.Item1,input.Item2):await storage.LoadAvatarByPrivateKeyAsync(input.Item1,input.Item2)};
                Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);Assert.IsTrue(result.IsError);
                Assert.IsInstanceOfType(result.Exception,typeof(System.ArgumentException));Assert.IsNull(result.Result);
                Assert.IsFalse(_provider.IsProviderActivated);
            }
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
        public void ProviderName_ShouldBeHashgraphOASIS()
        {
            // Arrange & Act
            var providerName = _provider.ProviderName;

            // Assert
            Assert.AreEqual("HashgraphOASIS", providerName);
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
        public void UnconfiguredProviderCannotClaimRealActivation()
        {
            // Arrange
            Assert.IsFalse(_provider.IsProviderActivated);

            // Act
            var result = _provider.ActivateProvider();

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void DeActivateUnconfiguredProviderRemainsInactive()
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
        public void ProviderCapabilities_ShouldDeclareBlockchain()
        {
            // Arrange & Act
            Assert.IsTrue(_provider.ProviderCapabilities.Any(capability => capability.Value == ProviderCategory.Blockchain));
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
