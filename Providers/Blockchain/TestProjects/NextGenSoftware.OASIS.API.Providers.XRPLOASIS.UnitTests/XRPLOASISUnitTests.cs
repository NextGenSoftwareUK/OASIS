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
            Assert.AreEqual(ProviderType.XRPLOASIS, _provider.ProviderType);
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
        public void ActivateProvider_ShouldSetIsProviderActivatedToTrue()
        {
            Assert.IsFalse(_provider.IsProviderActivated);
            var result = _provider.ActivateProvider();
            Assert.IsTrue(result.IsError == false);
            Assert.IsTrue(_provider.IsProviderActivated);
        }

        [TestMethod]
        public void DeActivateProvider_ShouldSetIsProviderActivatedToFalse()
        {
            _provider.ActivateProvider();
            Assert.IsTrue(_provider.IsProviderActivated);
            var result = _provider.DeActivateProvider();
            Assert.IsTrue(result.IsError == false);
            Assert.IsFalse(_provider.IsProviderActivated);
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
            {
                _provider.DeActivateProvider();
            }
        }
    }
}


