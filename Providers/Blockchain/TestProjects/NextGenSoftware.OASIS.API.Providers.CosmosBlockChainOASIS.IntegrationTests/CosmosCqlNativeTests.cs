using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class CosmosCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids, string Contract);
    private const string Chain = "oasis-cosmos-local";
    private const string Owner = "wasm1r5v5srda7xfth3hn2s26txvrcrntldju3hkw75";
    private const string HistoricalContract = "wasm14hj2tavq8fpesdwxxcu44rty3hh90vhujrvcmstl4zr3txmfvw9s0phg4d";

    internal static CosmosBlockChainOASIS Create(string contract)
        => CosmosBlockChainOASIS.CreateWithNativeFeeLimits(
            new CosmosNativeFeeLimits(new Dictionary<string,string> { ["stake"] = "10000" }),
            rpcEndpoint: "http://127.0.0.1:26657", chainId: Chain,
            privateKey: Environment.GetEnvironmentVariable("OASIS_COSMOS_CQL_SIGNING_KEY")
                ?? throw new InvalidOperationException("Use the retained Cosmos CQL runner."),
            contractAddress: contract, addressPrefix: "wasm", gasPrice: "0.025stake",
            nativeDenom: "stake", nativeDecimals: 0);

    [TestMethod, Timeout(240000)]
    public async Task NativeCosmosFiltersOwnedThreeRecordSeedToTwoWithoutChangingSource()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_COSMOS_CQL_STATE")
            ?? throw new InvalidOperationException("Use the retained Cosmos CQL runner.");
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Original setup requires reconciliation, never replay or reseeding.");
        Seed seed;
        if (!File.Exists(manifest))
        {
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0,3).Select(_ => Guid.NewGuid()).ToArray(), "");
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed);intent.Flush(true); }

            var sequence = 0;
            async Task<T> Prepared<T>(CosmosBlockChainOASIS provider, string step, Func<Task<T>> operation)
            {
                var originals = new List<string>();
                Func<string,string,PreparedSubmissionServer> prepare = (kind,key) => new(async (evidence,token) =>
                {
                    Assert.AreEqual(Chain, evidence.GetProperty("chainId").GetString());
                    Assert.AreEqual(Owner, evidence.GetProperty("sender").GetString());
                    await provider.VerifyOriginalSignedPreparationAsync(evidence, token);
                    var number = sequence++;
                    Assert.IsTrue(number < 32, "Bound setup submissions before any further broadcast.");
                    var path = Path.Combine(state, $"original-{number:D2}.prepared.json");
                    using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                    await JsonSerializer.SerializeAsync(file, new { step, kind, key, preparation = evidence }, cancellationToken: token);
                    file.Flush(true); // The SDK may broadcast only after this callback acknowledges persistence.
                    originals.Add(evidence.GetProperty("transactionHash").GetString()!);
                });
                var backend = typeof(CosmosBlockChainOASIS).GetField("_sdkBackend", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(provider)!;
                var scope = backend.GetType().GetMethod("WithStoragePreparationAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .MakeGenericMethod(typeof(T));
                var result = await (Task<T>)scope.Invoke(backend, new object[] { operation, prepare })!;
                Assert.IsTrue(originals.Count > 0, "Every setup mutation must persist its actual signed identity.");
                foreach (var hash in originals)
                {
                    var receipt = await provider.GetNativeFeeReceiptAsync(hash);
                    Assert.IsFalse(receipt.IsError, receipt.Message);
                    Assert.AreEqual(hash, receipt.Result.GetProperty("transactionHash").GetString());
                    Assert.IsTrue(receipt.Result.GetProperty("success").GetBoolean());
                    var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { step, hash, receipt = receipt.Result }));
                    using var file = new FileStream(Path.Combine(state, hash + ".ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                    file.Write(bytes);file.Flush(true);
                }
                return result;
            }

            using (var deployer = Create(HistoricalContract))
            {
                // Instantiate existing reviewed local code once; do not upload or
                // mutate the historical contract or bypass its encryption checks.
                var created = await Prepared(deployer, "instantiate", () => deployer.InstantiateContractAsync(
                    1, new { }, "cosmic-cql-demo"));
                Assert.IsFalse(created.IsError, created.Exception?.ToString() ?? created.Message);
                seed = seed with { Contract = created.Result.ContractAddress };
                Assert.AreNotEqual(HistoricalContract, seed.Contract);
                using var update = new FileStream(pending, FileMode.Truncate, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(update, seed);update.Flush(true);
            }
            using (var writer = Create(seed.Contract))
            {
                var activation = await writer.ActivateProviderAsync();
                Assert.IsFalse(activation.IsError, activation.Message);
                var names = new[] { "Espresso", "Latte", "Tea" };
                for (var index = 0; index < seed.Ids.Length; index++)
                {
                    var row = new Holon { Id = seed.Ids[index], Name = names[index],
                        CreatedByAvatarId = seed.Actor, IsNewHolon = true, IsActive = true };
                    var saved = await Prepared(writer, "save-" + index, () => writer.SaveHolonAsync(row, saveChildren: false));
                    Assert.IsFalse(saved.IsError, saved.Exception?.ToString() ?? saved.Message);
                    Assert.IsTrue(saved.IsSaved);
                }
                await writer.DeActivateProviderAsync();
            }
            File.Move(pending, manifest);
        }
        else seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;

        using var source = Create(seed.Contract);
        var ready = await source.ActivateProviderAsync();
        Assert.IsFalse(ready.IsError, ready.Message);
        try
        {
            async Task<string[]> Contents()
            {
                var rows = new List<string>();
                foreach (var id in seed.Ids)
                {
                    var loaded = await source.LoadHolonAsync(id, loadChildren: false);
                    Assert.IsFalse(loaded.IsError, loaded.Message);
                    rows.Add(OasisJson.Serialize(loaded.Result));
                }
                return rows.ToArray();
            }
            var before = await Contents();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CosmosBlockChainOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.CosmosBlockChainOASIS, seed.Ids).Result).Result;
            async Task<IReadOnlyList<IHolon>> Read(CosmicQuery<Holon> selection)
            {
                var plan = CosmicQueryPlanner.Plan(selection, ProviderType.CosmosBlockChainOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.CosmosBlockChainOASIS, source.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await source.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
                return result.Result;
            }
            CollectionAssert.AreEquivalent(new[] { "Espresso", "Latte", "Tea" }, (await Read(query)).Select(row => row.Name).ToArray());
            var coffee = seed.Ids.Take(2).ToArray();
            CollectionAssert.AreEquivalent(new[] { "Espresso", "Latte" }, (await Read(query.Where(row => coffee.Contains(row.Id)).Result)).Select(row => row.Name).ToArray());
            CollectionAssert.AreEqual(before, await Contents(), "CQL must preserve the seeded source content.");
        }
        finally { await source.DeActivateProviderAsync(); }
    }
}
