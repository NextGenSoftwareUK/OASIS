using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS.IntegrationTests;

[TestClass]
public sealed class AlgorandPublicLifecycleTests
{
    [TestMethod]
    public async Task PublicAlgodLifecycleAndAccountRead()
    {
        const string address = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAY5HFKQ";
        var provider = new AlgorandOASIS("https://mainnet-api.algonode.cloud", "https://mainnet-idx.algonode.cloud");
        Assert.IsFalse(provider.IsProviderActivated);

        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        Assert.IsTrue(activated.Result);
        Assert.IsTrue(provider.IsProviderActivated);

        var loaded = await provider.LoadAvatarByProviderKeyAsync(address);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.IsNotNull(loaded.Result);
        Assert.AreEqual(address, loaded.Result.ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);
        var metadata = loaded.Result.ProviderMetaData[ProviderType.AlgorandOASIS];
        Assert.AreEqual(address, metadata["address"]);
        Assert.IsTrue(ulong.TryParse(metadata["balance_microALGO"], out _));

        Assert.IsTrue((await provider.DeActivateProviderAsync()).Result);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsTrue(provider.ActivateProvider().Result);
        Assert.IsTrue(provider.IsProviderActivated);
        Assert.IsTrue(provider.DeActivateProvider().Result);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task PublicIndexerReturnsExactConfirmedTransaction()
    {
        const string transactionId = "7MK6WLKFBPC323ATSEKNEKUTQZ23TCCM75SJNSFAHEM65GYJ5ANQ";
        var provider = new AlgorandOASIS("https://mainnet-api.algonode.cloud", "https://mainnet-idx.algonode.cloud");
        foreach (var synchronous in new[] { false, true })
        {
            var loaded = synchronous
                ? provider.LoadHolon(transactionId, loadChildren: false)
                : await provider.LoadHolonAsync(transactionId, loadChildren: false);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.IsTrue(loaded.IsLoaded);
            Assert.IsNotNull(loaded.Result);
            Assert.AreEqual(transactionId, loaded.Result.ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);
            var metadata = loaded.Result.ProviderMetaData[ProviderType.AlgorandOASIS];
            Assert.AreEqual(transactionId, metadata["tx_id"]);
            Assert.AreEqual("pay", metadata["tx_type"]);
            Assert.AreEqual("62440", metadata["confirmed_round"]);
            Assert.AreEqual("100000", metadata["amount_microALGO"]);
            Assert.AreEqual("I3345FUQQ2GRBHFZQPLYQQX5HJMMRZMABCHRLWV6RCJYC6OO4MOLEUBEGU", metadata["sender"]);
            Assert.AreEqual("ALGORANDAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIN5DNAU", metadata["receiver"]);
        }
    }
}
