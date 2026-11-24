using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.XRPLOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.XRPLOASIS.UnitTests
{
    [TestClass]
    public class XRPLOASISProviderTests
    {
        private XRPLOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new XRPLOASIS();
        }

        [TestMethod]
        public void ProviderType_ShouldBeXRPLOASIS()
        {
            Assert.AreEqual(ProviderType.XRPLOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeXRPLOASIS()
        {
            Assert.AreEqual("XRPLOASIS", _provider.ProviderName);
        }

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
        {
            Assert.IsNotNull(_provider.ProviderDescription);
            Assert.IsFalse(string.IsNullOrEmpty(_provider.ProviderDescription));
        }

        [TestMethod]
        public void UnconfiguredProvider_RefusesActivationWithAnExplicitConfigurationError()
        {
            // Storage needs an RPC endpoint, Ed25519 key, Kubo endpoint and encryption key; nothing is contacted without them.
            Assert.IsFalse(_provider.IsProviderActivated);
            var result = _provider.ActivateProvider();
            Assert.IsTrue(result.IsError);
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void DeActivateProvider_LeavesTheProviderInactive()
        {
            var result = _provider.DeActivateProvider();
            Assert.IsFalse(result.IsError);
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void ProviderCategory_ShouldBeStorageAndNetwork()
        {
            Assert.AreEqual(ProviderCategory.StorageAndNetwork, _provider.ProviderCategory.Value);
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


