using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.GUNOASIS;

namespace NextGenSoftware.OASIS.API.Providers.GUNOASIS.UnitTests
{
    /// <summary>
    /// Provider metadata checks. These run with no backend - they assert the provider
    /// identifies itself correctly and starts deactivated.
    /// </summary>
    [TestClass]
    public class GUNOASISProviderTests
    {
        private GUNOASIS _provider = null!;

        [TestInitialize]
        public void Setup() => _provider = new GUNOASIS("http://127.0.0.1:8765", new byte[32]);

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
        {
            Assert.ThrowsException<ArgumentException>(() => new GUNOASIS("http://127.0.0.1:8765", null!));
            Assert.ThrowsException<ArgumentException>(() => new GUNOASIS("http://127.0.0.1:8765", new byte[16]));
        }

        [TestMethod]
        public void Constructor_RejectsNonWebRelayUrls()
            => Assert.ThrowsException<ArgumentException>(() => new GUNOASIS("ftp://127.0.0.1:8765", new byte[32]));

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
                _provider.DeActivateProvider();
        }
    }
}
