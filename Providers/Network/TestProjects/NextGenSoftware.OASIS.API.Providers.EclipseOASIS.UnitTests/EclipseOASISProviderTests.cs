using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.EclipseOASIS;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.EclipseOASIS.UnitTests
{
    /// <summary>
    /// Construction and delegation contract checks. No RPC call is made: a loopback port is never contacted
    /// because nothing is activated or written.
    /// </summary>
    [TestClass]
    public class EclipseOASISProviderTests
    {
        private const string Loopback = "http://127.0.0.1:1/";

        [TestMethod]
        public void IdentifiesAsEclipseAndStartsDeactivated()
        {
            var provider = new EclipseOASIS();
            Assert.AreEqual(ProviderType.EclipseOASIS, provider.ProviderType.Value);
            Assert.AreEqual("EclipseOASIS", provider.ProviderName);
            Assert.IsFalse(string.IsNullOrWhiteSpace(provider.ProviderDescription));
            Assert.AreEqual(ProviderCategory.StorageAndNetwork, provider.ProviderCategory.Value);
            Assert.AreEqual(EclipseOASIS.DefaultRpcUrl.TrimEnd('/') + "/", provider.RpcUrl);
            Assert.IsFalse(provider.IsProviderActivated);
        }

        [TestMethod]
        public async Task WithoutSignerStorageAndActivationReturnExplicitErrors()
        {
            var provider = new EclipseOASIS(Loopback);
            Assert.IsNull(provider.SvmStorage);

            var activation = await provider.ActivateProviderAsync();
            Assert.IsTrue(activation.IsError);
            StringAssert.Contains(activation.Message, "signing account");
            Assert.IsFalse(provider.IsProviderActivated);

            var saved = provider.SaveHolon(new Holon { Id = Guid.NewGuid(), Name = "unsigned" });
            Assert.IsTrue(saved.IsError);
            Assert.IsNull(saved.Result);
            var loaded = await provider.LoadAvatarAsync(Guid.NewGuid());
            Assert.IsTrue(loaded.IsError);
            Assert.IsNull(loaded.Result);
        }

        [TestMethod]
        public void SignerComposesSvmStorageAgainstTheEclipseEndpoint()
        {
            var account = new Account();
            var provider = new EclipseOASIS(Loopback, account.PrivateKey.Key, account.PublicKey.Key);
            Assert.IsNotNull(provider.SvmStorage);
            Assert.AreEqual(Loopback, provider.RpcUrl);
            Assert.AreEqual(ProviderType.EclipseOASIS, provider.ProviderType.Value);
            Assert.IsFalse(provider.IsProviderActivated);
            Assert.IsFalse(provider.SvmStorage!.IsProviderActivated);
        }

        [TestMethod]
        public void RejectsHalfConfiguredSignerAndRelativeEndpoint()
        {
            var account = new Account();
            Assert.ThrowsException<ArgumentException>(() => new EclipseOASIS(Loopback, account.PrivateKey.Key, null));
            Assert.ThrowsException<ArgumentException>(() => new EclipseOASIS(Loopback, null, account.PublicKey.Key));
            Assert.ThrowsException<UriFormatException>(() => new EclipseOASIS("eclipse-rpc"));
        }

        [TestMethod]
        public async Task DeactivationWithoutActivationSucceeds()
        {
            var provider = new EclipseOASIS(Loopback);
            var result = await provider.DeActivateProviderAsync();
            Assert.IsFalse(result.IsError);
            Assert.IsTrue(result.Result);
            Assert.IsFalse(provider.IsProviderActivated);
        }
    }
}
