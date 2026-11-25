using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.CeramicOASIS.UnitTests
{
    /// <summary>Provider identity and constructor validation; no storage service or Ceramic node is needed.</summary>
    [TestClass]
    public class CeramicOASISProviderTests
    {
        private static CeramicOASIS Create() => new("http://127.0.0.1:18931", "token", new byte[32]);

        [TestMethod]
        public void Constructor_ShouldInitializeProviderIdentity()
        {
            var provider = Create();
            Assert.AreEqual("CeramicOASIS", provider.ProviderName);
            Assert.IsFalse(string.IsNullOrWhiteSpace(provider.ProviderDescription));
            Assert.AreEqual(ProviderType.CeramicOASIS, provider.ProviderType.Value);
            Assert.AreEqual(ProviderCategory.StorageAndNetwork, provider.ProviderCategory.Value);
            Assert.IsFalse(provider.IsProviderActivated);
        }

        [TestMethod]
        public void Constructor_RequiresA32ByteEncryptionKey()
            => Assert.ThrowsException<ArgumentException>(() => new CeramicOASIS("http://127.0.0.1:18931", "token", new byte[16]));

        [TestMethod]
        public void Constructor_RequiresTheServiceToken()
            => Assert.ThrowsException<ArgumentException>(() => new CeramicOASIS("http://127.0.0.1:18931", "", new byte[32]));

        [TestMethod]
        public void Constructor_RequiresAnHttpServiceUrl()
            => Assert.ThrowsException<ArgumentException>(() => new CeramicOASIS("ftp://127.0.0.1:18931", "token", new byte[32]));

        [TestMethod]
        public async Task Activation_FailsExplicitlyWhenTheServiceIsUnreachable()
        {
            var result = await new CeramicOASIS("http://127.0.0.1:1", "token", new byte[32]).ActivateProviderAsync();
            Assert.IsTrue(result.IsError);
            Assert.IsFalse(result.Result);
        }
    }
}
