using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.UnitTests
{
    /// <summary>Storage configuration contract of RadixOASIS over the OasisKeyValueStore component (no network).</summary>
    [TestClass]
    public class RadixKeyValueStorageConfigurationTests
    {
        private const string Key = "0101010101010101010101010101010101010101010101010101010101010101";

        private static RadixOASISConfig Complete() => new()
        {
            NetworkId = 240,
            NetworkName = "localnet",
            CoreApiUri = "http://127.0.0.1:1/core/",
            EngineStateApiUri = "http://127.0.0.1:1/engine-state/",
            PrivateKey = Key,
            KeyValueStoreComponentAddress = "component_loc1cp5tnvnhyaz0qgmj2vpe92ttvxyd8zsycxtnuhgh03r9wd32yj5ssl",
            StorageEncryptionKey = Key
        };

        [TestMethod]
        public async Task BridgeOnlyInstance_FailsStorageExplicitly()
        {
            using var provider = new RadixOASIS("https://stokenet.radixdlt.com", 2, "", "");
            var activated = await provider.ActivateProviderAsync();
            Assert.IsTrue(activated.IsError);
            StringAssert.Contains(activated.Message, "KeyValueStoreComponentAddress");
            Assert.IsFalse(provider.IsProviderActivated);
            Assert.IsNotNull(provider.RadixBridgeService);
            Assert.IsTrue((await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = "x" })).IsError);
        }

        [TestMethod]
        public void PartialStorageConfig_IsRejected()
        {
            var config = Complete();
            config.StorageEncryptionKey = null;
            Assert.ThrowsException<ArgumentException>(() => new RadixOASIS(config));
        }

        [TestMethod]
        public void ShortEncryptionKey_IsRejected()
        {
            var config = Complete();
            config.StorageEncryptionKey = "0102";
            Assert.ThrowsException<ArgumentException>(() => new RadixOASIS(config));
        }

        [TestMethod]
        public void CompleteConfig_DerivesTheSigningAccount()
        {
            using var provider = new RadixOASIS(Complete());
            StringAssert.StartsWith(provider.StorageAccountAddress, "account_loc1");
        }

        [TestMethod]
        public async Task UnreachableNode_FailsActivation()
        {
            using var provider = new RadixOASIS(Complete());
            var activated = await provider.ActivateProviderAsync();
            Assert.IsTrue(activated.IsError);
            Assert.IsFalse(provider.IsProviderActivated);
        }

        [TestMethod]
        public void ManifestStrings_AreEscaped()
        {
            Assert.AreEqual("\"a\\\"b\\\\c\\n\"", RadixLedgerClient.ManifestString("a\"b\\c\n"));
        }

        [TestMethod]
        public void Backend_RequiresAComponentAddress()
        {
            using var ledger = new RadixLedgerClient(new Uri("http://127.0.0.1:1/core/"), new Uri("http://127.0.0.1:1/engine-state/"), 240, "localnet", Key);
            Assert.ThrowsException<ArgumentException>(() => new RadixKeyValueBackend(ledger, "account_loc1xyz"));
            StringAssert.StartsWith(ledger.SignatureBadge, "resource_loc1");
        }
    }
}
