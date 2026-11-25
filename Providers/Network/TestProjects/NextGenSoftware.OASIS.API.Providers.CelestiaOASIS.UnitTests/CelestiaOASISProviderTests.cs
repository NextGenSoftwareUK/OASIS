using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.CelestiaOASIS.UnitTests
{
    /// <summary>Provider identity and configuration validation. Construction never contacts the node.</summary>
    [TestClass]
    public class CelestiaOASISProviderTests
    {
        private const string Namespace = "6f617369732d756e6974"; // "oasis-unit"

        private static CelestiaOASIS Create() => new("http://127.0.0.1:1", "token", Namespace, 1, RandomNumberGenerator.GetBytes(32));

        [TestMethod]
        public void ProviderIdentifiesAsCelestiaAndStartsDeactivated()
        {
            using var provider = Create();
            Assert.AreEqual(ProviderType.CelestiaOASIS, provider.ProviderType.Value);
            Assert.AreEqual("CelestiaOASIS", provider.ProviderName);
            Assert.IsFalse(string.IsNullOrWhiteSpace(provider.ProviderDescription));
            Assert.AreEqual(ProviderCategory.StorageAndNetwork, provider.ProviderCategory.Value);
            Assert.IsFalse(provider.IsProviderActivated);
        }

        [TestMethod]
        public void StorageConfigurationIsRequired()
        {
            var key = RandomNumberGenerator.GetBytes(32);
            Assert.ThrowsException<ArgumentException>(() => new CelestiaOASIS("", "token", Namespace, 1, key));
            Assert.ThrowsException<ArgumentException>(() => new CelestiaOASIS("http://127.0.0.1:1", "", Namespace, 1, key));
            Assert.ThrowsException<ArgumentException>(() => new CelestiaOASIS("http://127.0.0.1:1", "token", "00", 1, key));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new CelestiaOASIS("http://127.0.0.1:1", "token", Namespace, 0, key));
            Assert.ThrowsException<ArgumentException>(() => new CelestiaOASIS("http://127.0.0.1:1", "token", Namespace, 1, new byte[16]));
        }

        [TestMethod]
        public async Task UnreachableNodeFailsActivation()
        {
            using var provider = Create();
            var result = await provider.ActivateProviderAsync();
            Assert.IsTrue(result.IsError);
            Assert.IsFalse(provider.IsProviderActivated);
        }

        [TestMethod]
        public void NamespaceIsVersionZeroWithTenByteId()
        {
            var ns = CelestiaBlobKeyValueBackend.NamespaceBytes(Namespace);
            Assert.AreEqual(29, ns.Length);
            Assert.IsTrue(ns.Take(19).All(b => b == 0));
            CollectionAssert.AreEqual(Convert.FromHexString(Namespace), ns[19..]);
        }
    }
}
