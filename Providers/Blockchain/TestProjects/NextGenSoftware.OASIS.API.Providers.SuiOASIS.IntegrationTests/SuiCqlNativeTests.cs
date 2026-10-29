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
        var source = deployment.RootElement;
        return new SuiOASIS("http://127.0.0.1:9000", "localnet", source.GetProperty("chainId").GetString()!,
            source.GetProperty("packageAddress").GetString()!, source.GetProperty("privateKey").GetString()!,
            source.GetProperty("storageObjectId").GetString()!, storageEncryptionKeyResolver:
                () => SHA256.HashData(Encoding.UTF8.GetBytes("oasis-sui-local-storage-fixture-v1")));
    }

    [TestMethod]
    public async Task OriginalEncryptedSourceFiltersThreeToTwoWithoutNativeMutation()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_SUI_CQL_STATE")
            ?? throw new InvalidOperationException("Explicit Sui CQL state required.");
        Directory.CreateDirectory(state);
        using var lease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve interrupted catalogue capture.");
        using var deployment = JsonDocument.Parse(await File.ReadAllTextAsync(Environment.GetEnvironmentVariable("OASIS_SUI_CQL_DEPLOYMENT")!));
        using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:9000"), Timeout = TimeSpan.FromSeconds(10) };
        async Task<string> Snapshot()
        {
            using var request = new StringContent(JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method = "sui_getObject",
                @params = new object[] { deployment.RootElement.GetProperty("storageObjectId").GetString()!, new { showContent = true } } }), Encoding.UTF8, "application/json");
            using var reply = await http.PostAsync("", request);
            reply.EnsureSuccessStatusCode();
            using var result = JsonDocument.Parse(await reply.Content.ReadAsStringAsync());
            Assert.IsFalse(result.RootElement.TryGetProperty("error", out _));
            return result.RootElement.GetProperty("result").GetProperty("data").GetRawText();
        }
        var before = await Snapshot();
        using var provider = Create();
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message + active.Exception);
        Seed seed;
        if (!File.Exists(manifest))
        {
            // Trusted test-owner catalogue discovery is separate from CQL execution.
            // It reads the original encrypted source and never seeds/replaces it.
            var discovered = await provider.LoadAllHolonsAsync(HolonType.All, loadChildren: false, continueOnError: false);
            Assert.IsFalse(discovered.IsError, discovered.Message + discovered.Exception);
            var ids = discovered.Result.Where(r => !r.IsDeleted).Select(r => r.Id).Distinct()
                .OrderBy(id => id.ToString("D"), StringComparer.Ordinal).Take(3).ToArray();
            Assert.AreEqual(3, ids.Length, "Original source needs three records; do not invent replacements.");
            seed = new Seed(Guid.NewGuid(), ids, true);
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            File.Move(pending, manifest);
        }
        else seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
        Assert.IsTrue(seed.ReadOnlyOriginal);
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
        Assert.AreEqual(before, await Snapshot(), "Original storage object/version/digest/content must remain unchanged.");
    }
}
