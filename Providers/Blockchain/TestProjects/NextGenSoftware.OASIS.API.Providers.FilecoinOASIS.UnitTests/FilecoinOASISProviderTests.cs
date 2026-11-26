using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.FilecoinOASIS;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.FilecoinOASIS.UnitTests
{
    /// <summary>
    /// Backend-free checks of the FEVM provider contract: identity, Web3Core storage binding,
    /// chain identity validation and activation preconditions (no RPC call is made).
    /// </summary>
    [TestClass]
    public class FilecoinOASISProviderTests
    {
        [TestMethod]
        public void DefaultProvider_IdentifiesAsFilecoinFevmStorage()
        {
            var provider = new FilecoinOASIS();
            Assert.AreEqual(ProviderType.FilecoinOASIS, provider.ProviderType.Value);
            Assert.AreEqual("FilecoinOASIS", provider.ProviderName);
            Assert.AreEqual(ProviderCategory.StorageAndNetwork, provider.ProviderCategory.Value);
            Assert.IsInstanceOfType(provider, typeof(Web3CoreOASISBaseProvider));
            Assert.IsInstanceOfType(provider, typeof(IOASISStorageProvider));
            Assert.IsInstanceOfType(provider, typeof(IOASISNETProvider));
            Assert.IsFalse(provider.IsProviderActivated);
        }

        [TestMethod]
        public void MainnetDefaults_AreTheFilecoinFevmChain()
        {
            Assert.AreEqual(new BigInteger(314), FilecoinOASIS.MainnetChainId);
            StringAssert.EndsWith(FilecoinOASIS.MainnetRpc, "/rpc/v1");
        }

        [TestMethod]
        public void NonPositiveChainIdentity_IsRejected()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new FilecoinOASIS("http://127.0.0.1:1234/rpc/v1", "", "", BigInteger.Zero));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new FilecoinOASIS("http://127.0.0.1:1234/rpc/v1", "", "", new BigInteger(-314)));
        }

        [TestMethod]
        public async Task ActivationWithoutKeyOrContract_FailsWithAnExplicitError()
        {
            var provider = new FilecoinOASIS("http://127.0.0.1:1/rpc/v1", "", "", new BigInteger(31415926));
            var result = await provider.ActivateProviderAsync();
            Assert.IsTrue(result.IsError);
            Assert.IsFalse(result.Result);
            Assert.IsFalse(provider.IsProviderActivated);
            StringAssert.Contains(result.Message, "private key and deployed contract address");
        }
    }
}
