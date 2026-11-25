using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.ONION_Protocol;

namespace NextGenSoftware.OASIS.API.Providers.ONION_Protocol.UnitTests
{
    /// <summary>Construction and metadata checks; these need no Tor client or backend.</summary>
    [TestClass]
    public class OnionOASISProviderTests
    {
        private const string Onion = "http://oasisoasisoasisoasisoasisoasisoasisoasisoasisoasisoasi.onion";

        private static OnionOASIS Create(string url = Onion, string token = "token", int keyLength = 32, int socksPort = 9050)
            => new OnionOASIS(url, token, new byte[keyLength], "127.0.0.1", socksPort);

        [TestMethod]
        public void ProviderType_IsOnionOASIS() => Assert.AreEqual(ProviderType.OnionOASIS, Create().ProviderType.Value);

        [TestMethod]
        public void ProviderName_IsOnionOASIS() => Assert.AreEqual("OnionOASIS", Create().ProviderName);

        [TestMethod]
        public void ProviderDescription_IsSet() => Assert.IsFalse(string.IsNullOrWhiteSpace(Create().ProviderDescription));

        [TestMethod]
        public void IsProviderActivated_IsFalseInitially() => Assert.IsFalse(Create().IsProviderActivated);

        [TestMethod]
        public void Constructor_RejectsAClearnetUrl()
            => Assert.ThrowsException<ArgumentException>(() => Create(url: "http://127.0.0.1:18900"));

        [TestMethod]
        public void Constructor_RejectsANonHttpOnionUrl()
            => Assert.ThrowsException<ArgumentException>(() => Create(url: "ftp://oasis.onion"));

        [TestMethod]
        public void Constructor_RequiresA32ByteEncryptionKey()
            => Assert.ThrowsException<ArgumentException>(() => Create(keyLength: 16));

        [TestMethod]
        public void Constructor_RequiresTheServiceToken()
            => Assert.ThrowsException<ArgumentException>(() => Create(token: ""));

        [TestMethod]
        public void Constructor_RejectsAnInvalidSocksPort()
            => Assert.ThrowsException<ArgumentOutOfRangeException>(() => Create(socksPort: 0));

        [TestMethod]
        public void Activation_FailsWithAnErrorWhenTorIsUnreachable()
        {
            // Port 1 on loopback has no SOCKS5 listener, so activation must report the failure, not succeed.
            var provider = Create(socksPort: 1);
            var result = provider.ActivateProvider();
            Assert.IsTrue(result.IsError);
            Assert.IsFalse(provider.IsProviderActivated);
        }
    }
}
