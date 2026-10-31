using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class SuiCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids, bool ReadOnlyOriginal);
    internal static SuiOASIS Create()
    {
        using var deployment = JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("OASIS_SUI_CQL_DEPLOYMENT")
            ?? throw new InvalidOperationException("Original deployment path required.")));
        using var demo = JsonDocument.Parse(File.ReadAllText(Path.Combine(Environment.GetEnvironmentVariable("OASIS_SUI_CQL_STATE")!, "demo-deployment.json")));
        var source = demo.RootElement;
        return new SuiOASIS("http://127.0.0.1:9000", "localnet", source.GetProperty("chainId").GetString()!,
            source.GetProperty("packageAddress").GetString()!, deployment.RootElement.GetProperty("privateKey").GetString()!,
            source.GetProperty("storageObjectId").GetString()!, storageEncryptionKeyResolver:
                () => SHA256.HashData(Encoding.UTF8.GetBytes("oasis-sui-local-storage-fixture-v1")));
    }

    [TestMethod]
    public async Task DedicatedEncryptedSourceFiltersThreeToTwoAndPreservesOriginalStorage()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_SUI_CQL_STATE")
            ?? throw new InvalidOperationException("Explicit Sui CQL state required.");
        Directory.CreateDirectory(state);
        using var lease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve interrupted catalogue capture.");
        using var deployment = JsonDocument.Parse(await File.ReadAllTextAsync(Environment.GetEnvironmentVariable("OASIS_SUI_CQL_DEPLOYMENT")!));
        using var demo = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "demo-deployment.json")));
        Assert.IsFalse(File.Exists(Path.Combine(state, "publication.pending.json")), "Preserve original publication intent.");
        Assert.AreEqual("dedicated-cosmic-cql", demo.RootElement.GetProperty("sourceKind").GetString());
        Assert.AreNotEqual(deployment.RootElement.GetProperty("storageObjectId").GetString(), demo.RootElement.GetProperty("storageObjectId").GetString());
        using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:9000"), Timeout = TimeSpan.FromSeconds(10) };
        async Task<string> Snapshot(string storageId)
        {
            using var request = new StringContent(JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method = "sui_getObject",
                @params = new object[] { storageId, new { showContent = true } } }), Encoding.UTF8, "application/json");
            using var reply = await http.PostAsync("", request);
            reply.EnsureSuccessStatusCode();
            using var result = JsonDocument.Parse(await reply.Content.ReadAsStringAsync());
            Assert.IsFalse(result.RootElement.TryGetProperty("error", out _));
            return result.RootElement.GetProperty("result").GetProperty("data").GetRawText();
        }
        var originalBefore = await Snapshot(deployment.RootElement.GetProperty("storageObjectId").GetString()!);
        using var provider = Create();
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message + active.Exception);
        Seed seed;
        if (!File.Exists(manifest))
        {
            // Dedicated package admission is complete before source records exist.
            // Persist each original signed physical write before allowing submission.
            var discovered = await provider.LoadAllHolonsAsync(HolonType.All, loadChildren: false, continueOnError: false);
            Assert.IsFalse(discovered.IsError, discovered.Message + discovered.Exception);
            Assert.AreEqual(0, discovered.Result.Count(), "Never seed an occupied dedicated source.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray(), true);
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            var sdk = typeof(SuiOASIS).GetField("_sdkClient", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(provider)!;
            var scope = sdk.GetType().GetMethod("WithStoragePreparationAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(typeof(OASISResult<IHolon>));
            var invoke = sdk.GetType().GetMethod("InvokeAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var physical = 0;
            var names = new[] { "Espresso", "Latte", "Tea" };
            for (var i = 0; i < 3; i++)
            {
                var row = new Holon { Id = seed.Ids[i], Name = names[i], CreatedByAvatarId = seed.Actor, IsNewHolon = true };
                Func<PreparedSubmissionServer> owner = () => new PreparedSubmissionServer(async (evidence, token) =>
                {
                    Assert.AreEqual(demo.RootElement.GetProperty("chainId").GetString(), evidence.GetProperty("chainId").GetString());
                    Assert.AreEqual(demo.RootElement.GetProperty("address").GetString(), evidence.GetProperty("sender").GetString());
                    var verified = await (Task<JsonElement>)invoke.Invoke(sdk, new object[] { "verifyNativePreparation", new { preparation = evidence }, token })!;
                    Assert.IsTrue(verified.GetProperty("verified").GetBoolean());
                    var number = Interlocked.Increment(ref physical);
                    using var file = new FileStream(Path.Combine(state, $"source-prepared-{number:D3}.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                    await JsonSerializer.SerializeAsync(file, evidence, cancellationToken: token); file.Flush(true);
                });
                Func<Task<OASISResult<IHolon>>> write = () => provider.SaveHolonAsync(row, saveChildren: false);
                var saved = await (Task<OASISResult<IHolon>>)scope.Invoke(sdk, new object[] { write, owner })!;
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved);
                using var file = new FileStream(Path.Combine(state, $"source-write-{i}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(file, new { row.Id, physicalPreparedCount = physical, acknowledged = true }); file.Flush(true);
            }
            Assert.IsTrue(physical >= 3);
            File.Move(pending, manifest);
        }
        else seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
        Assert.IsTrue(seed.ReadOnlyOriginal);
        var before = await Snapshot(demo.RootElement.GetProperty("storageObjectId").GetString()!);
        foreach (var ids in new[] { seed.Ids, seed.Ids.Take(2).ToArray(), new[] { Guid.NewGuid() } })
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SuiOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.SuiOASIS,
                    ids.Length == 1 ? ids : seed.Ids).Result).Result.Where(h => ids.Contains(h.Id)).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.SuiOASIS,
                new[] { new CosmicQueryTarget(ProviderType.SuiOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEquivalent(ids.Length == 1 ? Array.Empty<Guid>() : ids, result.Result.Select(r => r.Id).ToArray());
        }
        Assert.AreEqual(before, await Snapshot(demo.RootElement.GetProperty("storageObjectId").GetString()!), "CQL must preserve dedicated storage object/version/digest/content.");
        Assert.AreEqual(originalBefore, await Snapshot(deployment.RootElement.GetProperty("storageObjectId").GetString()!), "Original deployment Storage must remain untouched.");
    }
}
