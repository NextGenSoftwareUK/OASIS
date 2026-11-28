using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.TelosOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS.UnitTests
{
    /// <summary>
    /// Records mapped through TelosOASIS belong to Telos: the composed EOSIO implementation must stamp
    /// <see cref="ProviderType.TelosOASIS"/> in ProviderUniqueStorageKey, never EOSIOOASIS. Invalid keys must fail before transport activation;
    /// successful key round trips are exercised against the real node in native integration CI.
    /// </summary>
    [TestClass]
    public class TelosOASISStorageKeyTests
    {
        [TestMethod]
        public void ComposedEosioStorageIsOwnedByTelos()
        {
            var telos = new TelosOASIS("http://127.0.0.1:1/", "oasisaccount", "chain", "5KQwrPbwdL6PhXujxW37FSSQZ1JiwsST4cqQzDeyXtP79zkvFD3");
            var storageType = typeof(EOSIOOASIS.EOSIOOASIS).GetProperty("StorageProviderType", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Assert.AreEqual(ProviderType.TelosOASIS, storageType.GetValue(telos.EOSIOOASIS));
            Assert.AreEqual(ProviderType.EOSIOOASIS, telos.EOSIOOASIS.ProviderType.Value);
            var standalone = new EOSIOOASIS.EOSIOOASIS("http://127.0.0.1:1/", "oasisaccount", "chain", "key");
            Assert.AreEqual(ProviderType.EOSIOOASIS, storageType.GetValue(standalone));
        }

        [DataTestMethod]
        [DataRow(false)][DataRow(true)]
        public async Task NonRecordKeysFailBeforeActivatingTheTransport(bool synchronous)
        {
            var telos = new TelosOASIS("http://127.0.0.1:1/", "oasisaccount", "chain", "key");
            var holon = synchronous ? telos.LoadHolon("telos-row-7") : await telos.LoadHolonAsync("telos-row-7");
            var avatar = synchronous ? telos.LoadAvatarByProviderKey("accountname") : await telos.LoadAvatarByProviderKeyAsync("accountname");
            Assert.IsTrue(holon.IsError);
            Assert.IsTrue(avatar.IsError);
            Assert.IsFalse(telos.EOSIOOASIS.IsProviderActivated);
        }
    }
}
