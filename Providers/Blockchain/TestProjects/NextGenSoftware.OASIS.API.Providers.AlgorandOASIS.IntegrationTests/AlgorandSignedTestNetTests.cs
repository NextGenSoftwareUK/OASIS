using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS.IntegrationTests;

[TestClass]
public sealed class AlgorandSignedTestNetTests
{
    [TestMethod]
    public async Task SubmitsAndReadsSignedPaymentOnPublicTestNet()
    {
        static string Required(string name) => Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException($"Missing TestNet fixture variable {name}.");

        var sender = Required("OASIS_ALGORAND_TESTNET_SENDER");
        var txId = Required("OASIS_ALGORAND_TESTNET_TX");
        var holon = new Holon();
        holon.MetaData["SignedTxnBase64"] = Required("OASIS_ALGORAND_TESTNET_SIGNED");
        var provider = new AlgorandOASIS("https://testnet-api.algonode.cloud", "https://testnet-idx.algonode.cloud");
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);
        try
        {
            var sent = await provider.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsFalse(sent.IsError, sent.Message);
            Assert.AreEqual(txId, sent.Result.ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);

            // A transaction acknowledged by Algod becomes readable once the Indexer processes its block.
            var indexed = await provider.LoadHolonAsync(txId, loadChildren: false);
            for (var attempt = 0; attempt < 20 && indexed.Exception is HttpRequestException
                { StatusCode: HttpStatusCode.NotFound }; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                indexed = await provider.LoadHolonAsync(txId, loadChildren: false);
            }
            Assert.IsFalse(indexed.IsError, indexed.Message);
            Assert.AreEqual(txId, indexed.Result.ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);
            var metadata = indexed.Result.ProviderMetaData[ProviderType.AlgorandOASIS];
            Assert.AreEqual(sender, metadata["sender"]);
            Assert.AreEqual(sender, metadata["receiver"]);
            Assert.AreEqual("0", metadata["amount_microALGO"]);
            Assert.IsTrue(ulong.TryParse(metadata["confirmed_round"], out var round) && round > 0);
        }
        finally
        {
            Assert.IsTrue((await provider.DeActivateProviderAsync()).Result);
        }
    }
}
