using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Infrastructure.EOSClient;
using NextGenSoftware.OASIS.API.Providers.TelosOASIS;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS.UnitTests
{
    /// <summary>
    /// Records mapped through TelosOASIS belong to Telos: the composed EOSIO implementation must stamp
    /// <see cref="ProviderType.TelosOASIS"/> in ProviderUniqueStorageKey, never EOSIOOASIS. The EOSIO client
    /// boundary is replaced so no node is contacted.
    /// </summary>
    [TestClass]
    public class TelosOASISStorageKeyTests
    {
        private const string HolonRow = "{\"id\":\"6f1d2a8e-6f55-4a37-9c4b-0a1c2b3d4e5f\",\"name\":\"telos-key\",\"eosioId\":\"telos-row-7\"}";

        private static TelosOASIS Compose()
        {
            var telos = new TelosOASIS("http://127.0.0.1:1/", "oasisaccount", "chain", "5KQwrPbwdL6PhXujxW37FSSQZ1JiwsST4cqQzDeyXtP79zkvFD3");
            var inner = telos.EOSIOOASIS;
            var client = DispatchProxy.Create<IEosClient, RowClient>();
            typeof(EOSIOOASIS.EOSIOOASIS).GetField("_eosClient", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inner, client);
            typeof(EOSIOOASIS.EOSIOOASIS).GetProperty("IsProviderActivated")!.SetValue(inner, true);
            return telos;
        }

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
        public async Task HolonLoadedByProviderKeyThroughTelosCarriesTheTelosKey(bool synchronous)
        {
            var telos = Compose();
            var loaded = synchronous ? telos.LoadHolon("telos-row-7") : await telos.LoadHolonAsync("telos-row-7");

            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual("telos-row-7", loaded.Result.ProviderUniqueStorageKey[ProviderType.TelosOASIS]);
            Assert.IsFalse(loaded.Result.ProviderUniqueStorageKey.ContainsKey(ProviderType.EOSIOOASIS));
        }

        public class RowClient : DispatchProxy
        {
            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
                => targetMethod?.Name == nameof(IEosClient.GetHolonByProviderKeyAsync)
                    ? Task.FromResult<object>(HolonRow)
                    : throw new InvalidOperationException("Unexpected EOSIO call: " + targetMethod?.Name);
        }
    }
}
