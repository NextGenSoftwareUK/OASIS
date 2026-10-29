using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class PolkadotCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids, string Contract);
    private const string Genesis = "0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208";

    [TestMethod, Timeout(300000), TestCategory("LocalNative")]
    public async Task NativeEncryptedRecordsFilterThreeToTwoWithoutChangingSource()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_POLKADOT_CQL_STATE")
            ?? throw new InvalidOperationException("Use the explicit Polkadot CQL native runner.");
        var setupPath = Environment.GetEnvironmentVariable("OASIS_POLKADOT_CQL_CONTRACT")
            ?? throw new InvalidOperationException("A retained contract identity is required.");
        using var setupFile = JsonDocument.Parse(await File.ReadAllTextAsync(setupPath));
        var setup = setupFile.RootElement;
        Assert.AreEqual(Genesis, setup.GetProperty("genesisHash").GetString());
        var config = new PolkadotStorageConfiguration(
            setup.GetProperty("contractAccountId").GetString()!,
            setup.GetProperty("contractCodeHash").GetString()!,
            setup.GetProperty("ownerAccountId").GetString()!);

        // This is the retained synthetic local signer, never a funded public-chain key.
        using var provider = new PolkadotOASIS("http://127.0.0.1:19944", Genesis,
            "0x" + string.Concat(Enumerable.Repeat("02", 32)), null, config);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString() ?? activation.Message);
        var raw = provider.CreateRecordBackend(config.ContractAccountId, config.CodeHash, config.OwnerAccountId);
        await raw.VerifyAsync();
        var pending = Path.Combine(state, "demo-seed.pending.json");
        var manifest = Path.Combine(state, "demo-seed.json");
        Assert.IsFalse(File.Exists(pending), "Preserve uncertain original writes; never replay or reseed.");
        Seed seed;
        try
        {
            if (!File.Exists(manifest))
            {
                seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray(), config.ContractAccountId);
                using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }

                var originals = new List<JsonElement>();
                var names = new[] { "Espresso", "Latte", "Tea" };
                for (var index = 0; index < seed.Ids.Length; index++)
                {
                    var row = new Holon { Id = seed.Ids[index], Name = names[index],
                        CreatedByAvatarId = seed.Actor, IsNewHolon = true, IsActive = true };
                    var saved = await provider.WithRecordStoragePreparationAsync(
                        () => provider.SaveHolonAsync(row, saveChildren: false),
                        (action, key) => new PreparedSubmissionServer((original, token) =>
                        {
                            token.ThrowIfCancellationRequested();
                            Assert.IsTrue(originals.Count < 12, "Bound seed submissions before broadcast.");
                            Assert.AreEqual(action, original.GetProperty("action").GetString());
                            Assert.AreEqual(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key)),
                                original.GetProperty("keyBase64").GetString());
                            using var output = new FileStream(Path.Combine(state, $"original-{originals.Count:D2}.prepared.json"),
                                FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                            output.Write(JsonSerializer.SerializeToUtf8Bytes(original));
                            output.Flush(true); // ACK permits exactly this signed original to be broadcast.
                            originals.Add(original.Clone());
                            return Task.CompletedTask;
                        }));
                    Assert.IsFalse(saved.IsError, saved.Exception?.ToString() ?? saved.Message);
                    Assert.IsTrue(saved.IsSaved);
                }

                for (var index = 0; index < originals.Count; index++)
                {
                    var outcome = await raw.ReadOriginalOutcomeAsync(originals[index]);
                    Assert.IsTrue(outcome.GetProperty("found").GetBoolean());
                    var receipt = outcome.GetProperty("receipt");
                    Assert.IsTrue(receipt.GetProperty("dispatchSucceeded").GetBoolean());
                    Assert.IsTrue(receipt.GetProperty("recordEffectMatchesRequest").GetBoolean());
                    using var output = new FileStream(Path.Combine(state, $"original-{index:D2}.ack.json"),
                        FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                    output.Write(JsonSerializer.SerializeToUtf8Bytes(outcome)); output.Flush(true);
                }
                File.Move(pending, manifest); // Publish reuse only after every original is confirmed.
            }
            else seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;

            Assert.AreEqual(config.ContractAccountId, seed.Contract);
            Assert.AreEqual(3, seed.Ids.Distinct().Count());
            async Task<string[]> Contents()
            {
                var contents = new List<string>();
                foreach (var id in seed.Ids)
                {
                    var loaded = await provider.LoadHolonAsync(id, loadChildren: false);
                    Assert.IsFalse(loaded.IsError, loaded.Exception?.ToString() ?? loaded.Message);
                    Assert.AreEqual(seed.Actor, loaded.Result.CreatedByAvatarId);
                    contents.Add(OasisJson.Serialize(loaded.Result));
                }
                return contents.ToArray();
            }
            var before = await Contents();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.PolkadotOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.PolkadotOASIS, seed.Ids).Result).Result;
            async Task<IReadOnlyList<IHolon>> Read(CosmicQuery<Holon> selection)
            {
                var plan = CosmicQueryPlanner.Plan(selection, ProviderType.PolkadotOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.PolkadotOASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
                return result.Result;
            }
            CollectionAssert.AreEquivalent(new[] { "Espresso", "Latte", "Tea" }, (await Read(query)).Select(row => row.Name).ToArray());
            var coffee = seed.Ids.Take(2).ToArray();
            CollectionAssert.AreEquivalent(new[] { "Espresso", "Latte" },
                (await Read(query.Where(row => coffee.Contains(row.Id)).Result)).Select(row => row.Name).ToArray());
            CollectionAssert.AreEqual(before, await Contents(), "Native CQL reads must preserve source contents and revisions.");
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
