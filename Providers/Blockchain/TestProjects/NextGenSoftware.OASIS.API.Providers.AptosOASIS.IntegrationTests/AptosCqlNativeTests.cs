using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class AptosCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    [TestMethod]
    public async Task NativeMoveRecordsFilterThreeToTwoAndKeepOriginalPayloads()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_APTOS_CQL_STATE")
            ?? throw new InvalidOperationException("Explicit retained Aptos CQL environment required.");
        using var lease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        Assert.IsFalse(File.Exists(Path.Combine(state, "setup.pending.json")), "Preserve uncertain native setup.");
        using var environment = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "environment.json")));
        var address = "0x" + environment.RootElement.GetProperty("address").GetString();
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Never replay an uncertain source write.");
        using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:18080/v1/"), Timeout = TimeSpan.FromSeconds(10) };
        using var ledger = await http.GetFromJsonAsync<JsonDocument>("");
        Assert.AreEqual(4, ledger!.RootElement.GetProperty("chain_id").GetInt32());
        using var genesis = await http.GetFromJsonAsync<JsonDocument>("blocks/by_height/0?with_transactions=false");
        Assert.AreEqual(environment.RootElement.GetProperty("genesis").GetString(), genesis!.RootElement.GetProperty("block_hash").GetString());
        using var provider = new AptosOASIS("http://127.0.0.1:18080/v1", "local",
            Environment.GetEnvironmentVariable("OASIS_APTOS_PRIVATE_KEY"), address);
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message + active.Exception);
        Seed seed;
        if (!File.Exists(manifest))
        {
            using var storage = await http.GetFromJsonAsync<JsonDocument>($"accounts/{address}/resource/{address}::oasis::OasisStorage");
            Assert.AreEqual(0, storage!.RootElement.GetProperty("data").GetProperty("keys").GetArrayLength(), "Never adopt populated source.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray());
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            var names = new[] { "Espresso", "Latte", "Tea" };
            for (var i = 0; i < 3; i++)
            {
                var saved = await provider.SaveHolonAsync(new Holon { Id = seed.Ids[i], Name = names[i],
                    CreatedByAvatarId = seed.Actor, IsNewHolon = true }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved, "Acknowledged native save owns its saved result state.");
                Assert.AreEqual(1, saved.SavedCount);
                var hash = saved.MetaData["Aptos.TransactionHash"];
                using var receipt = await http.GetFromJsonAsync<JsonDocument>("transactions/by_hash/" + hash);
                Assert.IsTrue(receipt!.RootElement.GetProperty("success").GetBoolean());
                using var file = new FileStream(Path.Combine(state, $"write-{i}.receipt.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(file, new { id = seed.Ids[i], hash, version = receipt.RootElement.GetProperty("version").GetString() });
                file.Flush(true);
            }
            File.Move(pending, manifest);
        }
        else seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
        async Task<string[]> Snapshot()
        {
            var records = new List<string>();
            foreach (var id in seed.Ids)
            {
                using var request = new StringContent(JsonSerializer.Serialize(new { function = address + "::oasis::get_record",
                    type_arguments = Array.Empty<string>(), arguments = new[] { address, "0x686f6c6f6e", "0x" + Convert.ToHexString(Encoding.UTF8.GetBytes(id.ToString("D"))) } }), Encoding.UTF8, "application/json");
                request.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                using var reply = await http.PostAsync("view", request);
                Assert.IsTrue(reply.IsSuccessStatusCode, await reply.Content.ReadAsStringAsync());
                records.Add(await reply.Content.ReadAsStringAsync());
            }
            return records.ToArray();
        }
        var before = await Snapshot();
        foreach (var ids in new[] { seed.Ids, seed.Ids.Take(2).ToArray(), new[] { Guid.NewGuid() } })
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AptosOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.AptosOASIS,
                    ids.Length == 1 ? ids : seed.Ids).Result).Result.Where(h => ids.Contains(h.Id)).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.AptosOASIS,
                new[] { new CosmicQueryTarget(ProviderType.AptosOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEquivalent(ids.Length == 1 ? Array.Empty<Guid>() : ids, result.Result.Select(r => r.Id).ToArray());
        }
        CollectionAssert.AreEqual(before, await Snapshot(), "Native CQL must not mutate original Move payloads.");
    }
}
