using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;
using RadixEngineToolkit;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.IntegrationTests
{
    /// <summary>
    /// End-to-end test of RadixOASIS on-ledger storage on Stokenet (Radix testnet; XRD has no value there).
    ///
    /// It creates a throwaway Ed25519 account, funds it from the Stokenet faucet, publishes the OasisStorage package
    /// (contracts/), instantiates it, then saves and loads avatars and holons through the provider.
    ///
    /// Opt-in: set RADIX_STOKENET_LIVE=1 and RADIX_OASIS_STORAGE_PACKAGE_DIR to the folder holding oasis_storage.wasm and
    /// oasis_storage.rpd (from `scrypto build` or the contract tests). Otherwise the test is inconclusive.
    /// </summary>
    [TestClass]
    public class StokenetOnLedgerStorageTests
    {
        private const string Gateway = "https://stokenet.radixdlt.com";
        private const byte StokenetId = 2;
        private static readonly HttpClient Http = new();

        [TestMethod]
        [Timeout(15 * 60 * 1000)]
        public async Task PublishInstantiateThenSaveAndLoadThroughTheProvider()
        {
            var packageDir = Environment.GetEnvironmentVariable("RADIX_OASIS_STORAGE_PACKAGE_DIR");
            if (Environment.GetEnvironmentVariable("RADIX_STOKENET_LIVE") != "1" || string.IsNullOrEmpty(packageDir))
                Assert.Inconclusive("Set RADIX_STOKENET_LIVE=1 and RADIX_OASIS_STORAGE_PACKAGE_DIR to run the Stokenet test.");

            // 1. Throwaway account, funded by the faucet.
            var seed = RandomNumberGenerator.GetBytes(32);
            var privateKeyHex = Convert.ToHexString(seed).ToLowerInvariant();
            using var key = RadixBridgeHelper.GetPrivateKeyFromHex(privateKeyHex);
            var account = Address.PreallocatedAccountAddressFromPublicKey(key.PublicKey(), StokenetId).AddressString();

            await Submit(key, new ManifestV1Builder()
                .FaucetLockFee()
                .FaucetFreeXrd()
                .AccountTryDepositEntireWorktopOrAbort(new Address(account), null)
                .Build(StokenetId));

            // 2. Publish the package and instantiate the component (returns the owner badge to the account).
            var code = File.ReadAllBytes(Path.Combine(packageDir!, "oasis_storage.wasm"));
            var definition = File.ReadAllBytes(Path.Combine(packageDir!, "oasis_storage.rpd"));
            var published = await Submit(key, new ManifestV1Builder()
                .AccountLockFee(new Address(account), new RadixEngineToolkit.Decimal("500"))
                .PackagePublish(code, definition, new Dictionary<string, MetadataInitEntry>())
                .AccountTryDepositEntireWorktopOrAbort(new Address(account), null)
                .Build(StokenetId));
            var package = (await NewEntities(published)).Single(a => a.StartsWith("package_"));

            var instantiated = await Submit(key, new ManifestV1Builder()
                .AccountLockFee(new Address(account), new RadixEngineToolkit.Decimal("50"))
                .CallFunction(new ManifestBuilderAddress.Static(new Address(package)), "OasisStorage", "instantiate",
                    Array.Empty<ManifestBuilderValue>())
                .AccountTryDepositEntireWorktopOrAbort(new Address(account), null)
                .Build(StokenetId));
            var created = await NewEntities(instantiated);
            var component = created.Single(a => a.StartsWith("component_"));
            var badge = created.Single(a => a.StartsWith("resource_"));

            // 3. The provider, configured for on-ledger storage.
            var provider = new RadixOASIS(new RadixOASISConfig
            {
                HostUri = Gateway,
                NetworkId = StokenetId,
                AccountAddress = account,
                PrivateKey = privateKeyHex,
                OasisBlueprintAddress = component,
                OasisStorageOwnerBadgeAddress = badge
            });

            var suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = $"radix-{suffix}", Email = $"radix-{suffix}@example.com", FirstName = "Ledger" };

            var saved = await provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);

            var byId = await provider.LoadAvatarAsync(avatar.Id);
            Assert.IsFalse(byId.IsError, byId.Message);
            Assert.AreEqual("Ledger", byId.Result.FirstName);
            Assert.AreEqual(avatar.Id, (await provider.LoadAvatarByUsernameAsync(avatar.Username)).Result?.Id);
            Assert.AreEqual(avatar.Id, (await provider.LoadAvatarByEmailAsync(avatar.Email)).Result?.Id);

            // Update moves the username index.
            avatar.Username = $"radix-renamed-{suffix}";
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
            Assert.IsTrue((await provider.LoadAvatarByUsernameAsync($"radix-{suffix}")).IsError);
            Assert.AreEqual(avatar.Id, (await provider.LoadAvatarByUsernameAsync(avatar.Username)).Result?.Id);

            var holon = new Holon { Id = Guid.NewGuid(), Name = $"holon-{suffix}" };
            Assert.IsFalse((await provider.SaveHolonAsync(holon)).IsError);
            var loadedHolon = await provider.LoadHolonAsync(holon.Id.ToString());
            Assert.IsFalse(loadedHolon.IsError, loadedHolon.Message);
            Assert.AreEqual(holon.Name, loadedHolon.Result.Name);

            Assert.IsFalse((await provider.DeleteHolonAsync(holon.Id)).IsError);
            Assert.IsTrue((await provider.LoadHolonAsync(holon.Id)).IsError);
            Assert.IsFalse((await provider.DeleteAvatarAsync(avatar.Id, softDelete: false)).IsError);
            Assert.IsTrue((await provider.LoadAvatarAsync(avatar.Id)).IsError);
        }

        // ------------------------------------------------------------------------------------------- helpers

        private static async Task<string> Submit(PrivateKey key, TransactionManifestV1 manifest)
        {
            using (manifest)
            {
                var epoch = (ulong)(await Post("/status/gateway-status", new JsonObject()))!["ledger_state"]!["epoch"]!;
                var header = new TransactionHeaderV1(StokenetId, epoch, epoch + 10, RadixBridgeHelper.RandomNonce(),
                    key.PublicKey(), true, 0);
                using IntentV1 intent = new(header, manifest, new MessageV1.None());
                using SignedTransactionIntentV1 signed = new(intent, Array.Empty<SignatureWithPublicKeyV1>());
                using NotarizedTransactionV1 tx = new(signed, key.SignToSignature(signed.SignedIntentHash().AsHash()));
                var intentHash = tx.IntentHash().AsStr();
                await Post("/transaction/submit", new JsonObject
                {
                    ["notarized_transaction_hex"] = Convert.ToHexString(tx.ToPayloadBytes()).ToLowerInvariant()
                });

                for (var i = 0; i < 60; i++)
                {
                    var status = (string?)(await Post("/transaction/status", new JsonObject { ["intent_hash"] = intentHash }))?["intent_status"];
                    if (status == "CommittedSuccess") return intentHash;
                    if (status is "CommittedFailure" or "PermanentlyRejected")
                        Assert.Fail($"Stokenet transaction {intentHash} ended as {status}.");
                    await Task.Delay(2000);
                }
                Assert.Fail($"Stokenet transaction {intentHash} did not commit in time.");
                return intentHash;
            }
        }

        /// <summary>Global entities created by a committed transaction.</summary>
        private static async Task<List<string>> NewEntities(string intentHash)
        {
            var details = await Post("/transaction/committed-details", new JsonObject
            {
                ["intent_hash"] = intentHash,
                ["opt_ins"] = new JsonObject { ["receipt_state_changes"] = true }
            });
            return details?["transaction"]?["receipt"]?["state_updates"]?["new_global_entities"]?.AsArray()
                .Select(e => (string)e!["entity_address"]!).ToList() ?? new List<string>();
        }

        private static async Task<JsonNode?> Post(string path, JsonNode body)
        {
            using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
            using var response = await Http.PostAsync(Gateway + path, content);
            var text = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(response.IsSuccessStatusCode, $"{path}: {(int)response.StatusCode} {text}");
            return JsonNode.Parse(text);
        }
    }
}
