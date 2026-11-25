using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.MatrixOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MatrixOASIS.UnitTests
{
    /// <summary>
    /// Provider metadata checks. These run with no backend - they assert the provider
    /// identifies itself correctly and starts deactivated.
    /// </summary>
    [TestClass]
    public class MatrixOASISProviderTests
    {
        private MatrixOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new MatrixOASIS("http://127.0.0.1:8008", "token", "!room:oasis.test", new byte[32]);

        [TestMethod]
        public void ProviderType_ShouldBeSet()
            => Assert.AreNotEqual(ProviderType.None, _provider.ProviderType.Value);

        [TestMethod]
        public void ProviderName_ShouldNotBeEmpty()
            => Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderName));

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
            => Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.ProviderDescription));

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
            => Assert.IsFalse(_provider.IsProviderActivated);

        [TestMethod]
        public void ProviderCategory_ShouldBeSet()
            => Assert.IsNotNull(_provider.ProviderCategory);

        [TestMethod]
        public void Constructor_RequiresA32ByteEncryptionKey()
            => Assert.ThrowsException<ArgumentException>(() => new MatrixOASIS("http://127.0.0.1:8008", "token", "!room:oasis.test", new byte[16]));

        [TestMethod]
        public void Constructor_RequiresARoomId()
            => Assert.ThrowsException<ArgumentException>(() => new MatrixOASIS("http://127.0.0.1:8008", "token", "#alias:oasis.test", new byte[32]));

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
