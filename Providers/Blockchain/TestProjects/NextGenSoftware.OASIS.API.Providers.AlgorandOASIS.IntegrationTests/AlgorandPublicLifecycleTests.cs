using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS.IntegrationTests;

[TestClass]
public sealed class AlgorandPublicLifecycleTests
{
    [TestMethod]
    public async Task PublicIndexerMatchesAssetIdAndExactNoteMetadata()
    {
        const string transferId = "7VHTBCU3DDI64UOA7QOEUHVZ4JM6RGSE2H64RB5RHOQJFDEIMXTA";
        const string paymentId = "7MK6WLKFBPC323ATSEKNEKUTQZ23TCCM75SJNSFAHEM65GYJ5ANQ";
        var provider = new AlgorandOASIS("https://mainnet-api.algonode.cloud", "https://mainnet-idx.algonode.cloud");

        var asset = new Dictionary<string, string> { ["tx_id"] = transferId, ["asa_id"] = "31566704" };
        var transfer = await provider.LoadHolonsByMetaDataAsync(asset, MetaKeyValuePairMatchMode.All, loadChildren: false);
        Assert.IsFalse(transfer.IsError, transfer.Message);
        Assert.AreEqual(transferId, transfer.Result.Single().ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);

        var payment = await provider.LoadHolonAsync(paymentId, loadChildren: false);
        Assert.IsFalse(payment.IsError, payment.Message);
        string note = payment.Result.ProviderMetaData[ProviderType.AlgorandOASIS]["note"];
        Assert.IsTrue(note.Length > 24);
        var filters = new Dictionary<string, string> { ["tx_id"] = paymentId, ["note"] = note };
        var exact = await provider.LoadHolonsByMetaDataAsync(filters, MetaKeyValuePairMatchMode.All, loadChildren: false);
        Assert.IsFalse(exact.IsError, exact.Message);
        Assert.AreEqual(paymentId, exact.Result.Single().ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);

        filters["note"] = note[..24];
        var prefixOnly = await provider.LoadHolonsByMetaDataAsync(filters, MetaKeyValuePairMatchMode.All, loadChildren: false);
        Assert.IsFalse(prefixOnly.IsError, prefixOnly.Message);
        Assert.AreEqual(0, prefixOnly.LoadedCount);
    }

    [TestMethod]
    public async Task PublicIndexerMatchesExactTransactionParticipants()
    {
        const string id = "7MK6WLKFBPC323ATSEKNEKUTQZ23TCCM75SJNSFAHEM65GYJ5ANQ";
        const string sender = "I3345FUQQ2GRBHFZQPLYQQX5HJMMRZMABCHRLWV6RCJYC6OO4MOLEUBEGU";
        const string receiver = "ALGORANDAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIN5DNAU";
        var provider = new AlgorandOASIS("https://mainnet-api.algonode.cloud", "https://mainnet-idx.algonode.cloud");
        var filters = new Dictionary<string, string> { ["tx_id"] = id, ["sender"] = sender, ["receiver"] = receiver };
        var matched = await provider.LoadHolonsByMetaDataAsync(filters, MetaKeyValuePairMatchMode.All, loadChildren: false);
        Assert.IsFalse(matched.IsError, matched.Message);
        Assert.AreEqual(id, matched.Result.Single().ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);

        filters["receiver"] = sender;
        var rejected = await provider.LoadHolonsByMetaDataAsync(filters, MetaKeyValuePairMatchMode.All, loadChildren: false);
        Assert.IsFalse(rejected.IsError, rejected.Message);
        Assert.AreEqual(0, rejected.LoadedCount);

        filters.Remove("sender");
        filters["receiver"] = receiver;
        var nativeReceiver = provider.LoadHolonsByMetaData(filters, MetaKeyValuePairMatchMode.All, loadChildren: false);
        Assert.IsFalse(nativeReceiver.IsError, nativeReceiver.Message);
        Assert.AreEqual(id, nativeReceiver.Result.Single().ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);
    }

    [TestMethod]
    public async Task PublicIndexerPreservesNativeTransactionTimestamp()
    {
        const string id = "7MK6WLKFBPC323ATSEKNEKUTQZ23TCCM75SJNSFAHEM65GYJ5ANQ";
        var provider = new AlgorandOASIS("https://mainnet-api.algonode.cloud", "https://mainnet-idx.algonode.cloud");
        var result = await provider.LoadHolonAsync(id, loadChildren: false);
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual(id, result.Result.ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);
        Assert.AreEqual("62440", result.Result.ProviderMetaData[ProviderType.AlgorandOASIS]["confirmed_round"]);
        var expected = System.DateTimeOffset.FromUnixTimeSeconds(1560614017).UtcDateTime;
        Assert.AreEqual(expected, result.Result.CreatedDate);
        Assert.AreEqual(expected, result.Result.ModifiedDate);
    }

    [TestMethod]
    public async Task PublicIndexerQueriesTransactionMetadataUsingNativeFilters()
    {
        const string id = "7MK6WLKFBPC323ATSEKNEKUTQZ23TCCM75SJNSFAHEM65GYJ5ANQ";
        var provider = new AlgorandOASIS("https://mainnet-api.algonode.cloud", "https://mainnet-idx.algonode.cloud");
        var exact = provider.LoadHolonsByMetaData("tx_id", id, loadChildren: false);
        Assert.IsFalse(exact.IsError, exact.Message);
        Assert.AreEqual(id, exact.Result.Single().ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);

        var filters = new Dictionary<string, string> { ["tx_id"] = id, ["tx_type"] = "pay", ["confirmed_round"] = "62440" };
        var intersection = await provider.LoadHolonsByMetaDataAsync(filters, MetaKeyValuePairMatchMode.All, loadChildren: false);
        Assert.IsFalse(intersection.IsError, intersection.Message);
        Assert.AreEqual(id, intersection.Result.Single().ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);
        filters["confirmed_round"] = "62441";
        var miss = await provider.LoadHolonsByMetaDataAsync(filters, MetaKeyValuePairMatchMode.All, loadChildren: false);
        Assert.IsFalse(miss.IsError, miss.Message);
        Assert.AreEqual(0, miss.LoadedCount);
        Assert.IsFalse(miss.Result.Any());

        filters.Remove("tx_type");
        filters["confirmed_round"] = "62440";
        var union = await provider.LoadHolonsByMetaDataAsync(filters, MetaKeyValuePairMatchMode.Any, loadChildren: false);
        Assert.IsFalse(union.IsError, union.Message);
        var ids = union.Result.Select(holon => holon.ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]).ToArray();
        Assert.AreEqual(1, ids.Count(key => key == id));
        Assert.AreEqual(ids.Length, ids.Distinct().Count());
        Assert.IsTrue(union.Result.All(holon => holon.ProviderMetaData[ProviderType.AlgorandOASIS]["confirmed_round"] == "62440"));
    }

    [TestMethod]
    public async Task PublicIndexerPaginatesAccountHistoryAndRejectsInvalidAddress()
    {
        const string address = "I3345FUQQ2GRBHFZQPLYQQX5HJMMRZMABCHRLWV6RCJYC6OO4MOLEUBEGU";
        var provider = new AlgorandOASIS("https://mainnet-api.algonode.cloud", "https://mainnet-idx.algonode.cloud");
        var loaded = await provider.LoadHolonsForParentAsync(address, type: HolonType.Holon, loadChildren: false);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.IsTrue(loaded.IsLoaded);
        var keys = loaded.Result.Select(holon => holon.ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]).ToArray();
        Assert.IsTrue(keys.Length > 100, "The fixture must exercise multiple pages.");
        Assert.AreEqual(keys.Length, keys.Distinct().Count());
        Assert.AreEqual(keys.Length, loaded.LoadedCount);
        Assert.AreEqual(loaded.IsWarning ? 1 : 0, loaded.WarningCount);

        var typed = await provider.LoadHolonsForParentAsync(address, type: HolonType.Avatar, loadChildren: false);
        Assert.IsFalse(typed.IsError, typed.Message);
        Assert.IsTrue(typed.IsLoaded);
        Assert.AreEqual(0, typed.LoadedCount);
        Assert.AreEqual(0, typed.Result.Count());

        var rejected = await provider.LoadHolonsForParentAsync("invalid-account-address", loadChildren: false);
        Assert.IsTrue(rejected.IsError);
        Assert.IsFalse(rejected.IsLoaded);
        Assert.IsNull(rejected.Result);
        Assert.IsInstanceOfType<HttpRequestException>(rejected.Exception);
    }

    [TestMethod]
    public async Task PublicActivationRequiresAHealthyIndexerAlongsideAlgod()
    {
        const string algod = "https://mainnet-api.algonode.cloud";
        const string indexer = "https://mainnet-idx.algonode.cloud";
        var healthy = new AlgorandOASIS(algod, indexer);
        var activated = await healthy.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        Assert.IsTrue(activated.Result);
        Assert.IsTrue(healthy.IsProviderActivated);
        StringAssert.Contains(activated.Message, "Algod round");
        StringAssert.Contains(activated.Message, "Indexed round");

        // Algod has its own /health endpoint, but its response cannot prove Indexer readiness.
        var missingIndexer = new AlgorandOASIS(algod, algod);
        var rejected = await missingIndexer.ActivateProviderAsync();
        Assert.IsTrue(rejected.IsError);
        Assert.IsFalse(rejected.Result);
        Assert.IsFalse(missingIndexer.IsProviderActivated);
        Assert.IsNotNull(rejected.Exception);

        var differentChain = new AlgorandOASIS(algod, "https://testnet-idx.algonode.cloud");
        var mismatched = await differentChain.ActivateProviderAsync();
        Assert.IsTrue(mismatched.IsError);
        Assert.IsFalse(differentChain.IsProviderActivated);
        StringAssert.Contains(mismatched.Message, "different Algorand networks");
    }

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
        Assert.IsTrue(loaded.IsLoaded);
        Assert.AreEqual(1, loaded.LoadedCount);
        Assert.IsNotNull(loaded.Result);
        Assert.AreEqual(address, loaded.Result.ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);
        var metadata = loaded.Result.ProviderMetaData[ProviderType.AlgorandOASIS];
        Assert.AreEqual(address, metadata["address"]);
        Assert.IsTrue(ulong.TryParse(metadata["balance_microALGO"], out _));
        Assert.IsTrue(ulong.TryParse(metadata["round"], out var round) && round > 0);
        Assert.IsTrue(metadata["status"] is "Online" or "Offline" or "NotParticipating");

        var synchronous = provider.LoadAvatarByProviderKey(address);
        Assert.IsFalse(synchronous.IsError, synchronous.Message);
        Assert.AreEqual(loaded.Result.Id, synchronous.Result.Id);
        Assert.AreEqual(loaded.Result.CreatedDate, synchronous.Result.CreatedDate);

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
