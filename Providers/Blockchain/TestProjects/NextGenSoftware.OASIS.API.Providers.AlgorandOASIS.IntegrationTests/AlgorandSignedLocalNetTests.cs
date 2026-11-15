using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS.IntegrationTests;

[TestClass]
public sealed class AlgorandSignedLocalNetTests
{
    [TestMethod]
    public async Task SubmitsSignedPaymentToAuthenticatedLocalNet() => VerifySignedSubmission(false);

    [TestMethod]
    public Task PreservesPartialBatchReceiptsOnAuthenticatedLocalNet() => VerifySignedSubmission(true);

    private static async Task VerifySignedSubmission(bool batch)
    {
        string Required(string name) => Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException($"Missing owned LocalNet fixture variable {name}.");

        const string algod = "http://127.0.0.1:4001";
        var provider = new AlgorandOASIS(algod, "http://127.0.0.1:8980", new string('a', 64), null);
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);
        Assert.IsTrue(provider.IsProviderActivated);
        try
        {
            var receiver = Required("OASIS_ALGORAND_LOCALNET_RECEIVER");
            var txId = Required("OASIS_ALGORAND_LOCALNET_TX");
            var holon = new Holon();
            holon.MetaData["SignedTxnBase64"] = Required("OASIS_ALGORAND_LOCALNET_SIGNED");
            if (batch)
            {
                var invalid = new Holon();
                var stopped = await provider.SaveHolonsAsync(new[] { invalid, holon }, saveChildren: false, continueOnError: false);
                Assert.IsTrue(stopped.IsError);
                Assert.AreEqual(1, stopped.ErrorCount);
                Assert.IsFalse(stopped.Result.Any());

                var partial = await provider.SaveHolonsAsync(new[] { invalid, holon }, saveChildren: false, continueOnError: true);
                Assert.IsTrue(partial.IsError);
                Assert.AreEqual(1, partial.ErrorCount);
                Assert.AreEqual(txId, partial.Result.Single().ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);
                Assert.AreEqual(1, partial.InnerMessages.Count);
            }
            else
            {
                var submitted = await provider.SaveHolonAsync(holon, saveChildren: false);
                Assert.IsFalse(submitted.IsError, submitted.Message);
                Assert.AreEqual(txId, submitted.Result.ProviderUniqueStorageKey[ProviderType.AlgorandOASIS]);
            }

            using var confirmationClient = new HttpClient { BaseAddress = new Uri(algod), Timeout = TimeSpan.FromSeconds(5) };
            confirmationClient.DefaultRequestHeaders.Add("X-Algo-API-Token", new string('a', 64));
            ulong confirmedRound = 0;
            for (var attempt = 0; attempt < 20 && confirmedRound == 0; attempt++)
            {
                using var response = await confirmationClient.GetAsync($"/v2/transactions/pending/{txId}");
                response.EnsureSuccessStatusCode();
                using var body = await response.Content.ReadAsStreamAsync();
                using var confirmation = await JsonDocument.ParseAsync(body);
                Assert.AreEqual(string.Empty, confirmation.RootElement.GetProperty("pool-error").GetString());
                if (confirmation.RootElement.TryGetProperty("confirmed-round", out var round))
                    confirmedRound = round.GetUInt64();
                if (confirmedRound == 0)
                    await Task.Delay(250);
            }
            Assert.IsTrue(confirmedRound > 0, "The native node did not confirm the signed payment.");

            var loaded = await provider.LoadAvatarByProviderKeyAsync(receiver);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual("200000", loaded.Result.ProviderMetaData[ProviderType.AlgorandOASIS]["balance_microALGO"]);
        }
        finally
        {
            Assert.IsTrue((await provider.DeActivateProviderAsync()).Result);
        }
    }
}
