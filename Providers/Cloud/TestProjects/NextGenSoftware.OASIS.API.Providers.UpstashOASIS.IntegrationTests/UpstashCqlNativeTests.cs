using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.UpstashOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class UpstashCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids, string EndpointHash);

    [TestMethod]
    public async Task HostedNativeSelectionPagingAndBudgetsPreserveOriginalValues()
    {
        var url = Environment.GetEnvironmentVariable("OASIS_UPSTASH_CQL_URL") ?? throw new InvalidOperationException("Protected endpoint required.");
        var token = Environment.GetEnvironmentVariable("OASIS_UPSTASH_CQL_TOKEN") ?? throw new InvalidOperationException("Protected credential required.");
        var endpointHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
        var state = Environment.GetEnvironmentVariable("OASIS_UPSTASH_CQL_STATE")
            ?? throw new InvalidOperationException("Persistent source directory required.");
        Directory.CreateDirectory(state);
        using var lease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve uncertain setup; never replay.");
        using var native = new HttpClient { BaseAddress = new Uri(url.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(15) };
        native.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        async Task<JsonElement> Command(params string[] args)
        {
            using var response = await native.PostAsJsonAsync("", args);
            response.EnsureSuccessStatusCode();
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.IsFalse(body.RootElement.TryGetProperty("error", out _), "Native command rejected.");
            return body.RootElement.GetProperty("result").Clone();
        }
        var provider = new UpstashOASIS(url, token);
        Seed seed;
        if (!File.Exists(manifest))
        {
            
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray(), endpointHash);
            foreach (var id in seed.Ids) Assert.AreEqual(JsonValueKind.Null, (await Command("GET", "holon:" + id.ToString("D"))).ValueKind, "Do not overwrite an existing key.");
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }

            var active = await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message);
            var names = new[] { "Espresso", "Latte", "Tea" };
            for (var i = 0; i < 3; i++)
            {
                var saved = await provider.SaveHolonAsync(new Holon { Id = seed.Ids[i], Name = names[i],
                    CreatedByAvatarId = seed.Actor, IsNewHolon = true }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message);
                Assert.IsTrue(saved.IsSaved, "Source write requires acknowledgement.");
                Assert.AreEqual(1, saved.SavedCount);
                var loaded = await provider.LoadHolonAsync(seed.Ids[i], loadChildren: false);
                Assert.IsFalse(loaded.IsError, loaded.Message);
                Assert.AreEqual(names[i], loaded.Result.Name);
                Assert.AreEqual(seed.Actor, loaded.Result.CreatedByAvatarId);
                using var receipt = new FileStream(Path.Combine(state, $"source-write-{i}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(receipt, new { id = seed.Ids[i], acknowledged = true }); receipt.Flush(true);
            }
            File.Move(pending, manifest);
        }
        else
        {
            seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
            Assert.AreEqual(endpointHash, seed.EndpointHash, "Original source endpoint required.");
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }
        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        async Task<string[]> Snapshot()
        {
            var rows = new List<string>();
            foreach (var id in seed.Ids.OrderBy(id => id.ToString("D"), StringComparer.Ordinal))
            {
                var value = await Command("GET", "holon:" + id.ToString("D"));
                Assert.AreEqual(JsonValueKind.String, value.ValueKind, "Original source missing; never reseed.");
                rows.Add(id.ToString("D") + "|" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.GetString()!))));
                var member = await Command("SISMEMBER", "holons:all", id.ToString("D"));
                Assert.AreEqual(1, member.GetInt32(), "Source index acknowledgement must persist.");
            }
            return rows.ToArray();
        }
        var before = await Snapshot();
        var baseline = Path.Combine(state, "source-snapshot.json");
        if (File.Exists(baseline))
            CollectionAssert.AreEqual(JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(baseline))!, before);
        else
        {
            using var file = new FileStream(baseline, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(file, before); file.Flush(true);
        }
        CosmicQueryPlan<Holon> Plan(Guid[] ids, bool paging = false)
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.UpstashOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.UpstashOASIS, ids).Result).Result
                .Where(h => ids.Contains(h.Id)).Result;
            if (paging) query = query.Skip(1).Result.Take(1).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.UpstashOASIS,
                new[] { new CosmicQueryTarget(ProviderType.UpstashOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            return plan.Result;
        }
        foreach (var ids in new[] { seed.Ids, seed.Ids.Take(2).ToArray(), new[] { Guid.NewGuid() } })
        {
            var result = await provider.ExecuteCosmicQueryAsync(Plan(ids));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEquivalent(ids.Length == 1 ? Array.Empty<Guid>() : ids, result.Result.Select(r => r.Id).ToArray());
        }
        var page = await provider.ExecuteCosmicQueryAsync(Plan(seed.Ids, true));
        Assert.IsFalse(page.IsError, page.Message);
        Assert.AreEqual(seed.Ids.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).Skip(1).First(), page.Result.Single().Id);
        var limited = await provider.ExecuteCosmicQueryAsync(Plan(seed.Ids), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", limited.ErrorCode);
        Assert.IsNull(limited.Result, "Budget failure must not publish partial rows.");
        CollectionAssert.AreEqual(before, await Snapshot(), "Native record values must remain unchanged.");
    }
}
