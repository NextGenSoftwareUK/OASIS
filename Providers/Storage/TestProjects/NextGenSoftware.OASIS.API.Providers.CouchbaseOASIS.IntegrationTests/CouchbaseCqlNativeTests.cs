using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Couchbase;
using Couchbase.Query;
using Couchbase.KeyValue;
using Couchbase.Core.IO.Transcoders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class CouchbaseCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    [TestMethod]
    public async Task NativeSelectionPagingAndBudgetsPreserveOriginalJsonAndUnsignedCasAcrossRestart()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_COUCHBASE_CQL_STATE")
            ?? throw new InvalidOperationException("Retained original environment path required.");
        Assert.IsTrue(OperatingSystem.IsLinux());
        Assert.IsTrue(File.Exists(Path.Combine(state, "environment.json")));
        using var lease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        var baseline = Path.Combine(state, "source-snapshot.json");
        Assert.IsFalse(File.Exists(pending), "Preserve unknown writes; never replay.");
        Assert.AreEqual(File.Exists(manifest), File.Exists(baseline), "Missing original baseline is never replaced.");
        using var credentials = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "credentials.json")));
        var user = credentials.RootElement.GetProperty("username").GetString()!;
        var password = credentials.RootElement.GetProperty("password").GetString()!;
        using var cluster = await Cluster.ConnectAsync("couchbase://127.0.0.1", user, password);
        var bucket = await cluster.BucketAsync("oasis");
        // This scope uses KV and SQL++; legacy Views is deliberately not exposed.
        await bucket.WaitUntilReadyAsync(TimeSpan.FromSeconds(30),
            new Couchbase.Diagnostics.WaitUntilReadyOptions().ServiceTypes(ServiceType.KeyValue, ServiceType.Query));
        var provider = new CouchbaseOASIS("couchbase://127.0.0.1", user, password);

        async Task<long> Count()
        {
            using var result = await cluster.QueryAsync<long>("SELECT RAW COUNT(*) FROM `oasis`.`_default`.`holons`",
                new QueryOptions().ScanConsistency(QueryScanConsistency.RequestPlus).Readonly(true).Timeout(TimeSpan.FromSeconds(15)));
            var counts = new List<long>();
            await foreach (var count in result.Rows) counts.Add(count);
            Assert.AreEqual(QueryStatus.Success, result.MetaData.Status);
            return counts.Single();
        }

        Seed seed;
        if (!File.Exists(manifest))
        {
            var scopes = await bucket.Collections.GetAllScopesAsync();
            Assert.IsFalse(scopes.SelectMany(s => s.Collections).Any(c => c.Name == "holons"), "Never adopt pre-existing business collection.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray());
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }

            var active = await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message + active.Exception);
            Assert.AreEqual(0L, await Count());
            var names = new[] { "Espresso", "Latte", "Tea" };
            for (var i = 0; i < 3; i++)
            {
                var saved = await provider.SaveHolonAsync(new Holon { Id = seed.Ids[i], Name = names[i],
                    CreatedByAvatarId = seed.Actor, IsNewHolon = true }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved && saved.SavedCount == 1);
                var cas = saved.MetaData["Couchbase.Cas"];
                Assert.IsTrue(ulong.TryParse(cas, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed > 0);
                using var receipt = new FileStream(Path.Combine(state, $"source-write-{i}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(receipt, new { id = seed.Ids[i], cas }); receipt.Flush(true);
            }
            File.Move(pending, manifest);
        }
        else
        {
            seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }

        var collection = await (await bucket.DefaultScopeAsync()).CollectionAsync("holons");
        async Task<string[]> Snapshot()
        {
            Assert.AreEqual(3L, await Count(), "Only original three records may exist.");
            var rows = new List<string>();
            for (var i = 0; i < seed.Ids.Length; i++)
            {
                using var document = await collection.GetAsync(seed.Ids[i].ToString("D"),
                    new GetOptions().Transcoder(new RawJsonTranscoder()));
                // SDK KV CAS is ulong; SQL++ numeric META().cas could lose precision.
                using var receipt = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, $"source-write-{i}.ack.json")));
                Assert.AreEqual(receipt.RootElement.GetProperty("cas").GetString(), document.Cas.ToString(CultureInfo.InvariantCulture));
                var json = document.ContentAs<string>();
                using var payload = JsonDocument.Parse(json);
                Assert.AreEqual(seed.Ids[i].ToString("D"), payload.RootElement.GetProperty("id").GetString());
                rows.Add(seed.Ids[i].ToString("D")+"|"+document.Cas.ToString(CultureInfo.InvariantCulture)+"|"+
                    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
            }
            rows.Sort(StringComparer.Ordinal);
            return rows.ToArray();
        }

        var before = await Snapshot();
        if (File.Exists(baseline))
            CollectionAssert.AreEqual(JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(baseline))!, before);
        else
        {
            using var file = new FileStream(baseline, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(file, before); file.Flush(true);
        }

        CosmicQueryPlan<Holon> Plan(Guid[] ids, bool paging = false)
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CouchbaseOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.CouchbaseOASIS, ids).Result).Result
                .Where(h => ids.Contains(h.Id)).Result;
            if (paging) query = query.Skip(1).Result.Take(1).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.CouchbaseOASIS,
                new[] { new CosmicQueryTarget(ProviderType.CouchbaseOASIS, provider.QueryCapabilities) });
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
        Assert.IsFalse(page.IsError, page.Message + page.Exception);
        Assert.AreEqual(seed.Ids.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).Skip(1).First(), page.Result.Single().Id);
        var limited = await provider.ExecuteCosmicQueryAsync(Plan(seed.Ids), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", limited.ErrorCode);
        Assert.IsNull(limited.Result);
        CollectionAssert.AreEqual(before, await Snapshot(), "Query must preserve JSON and exact unsigned CAS.");
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
