using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cassandra;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ScyllaDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class ScyllaDbCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    [TestMethod]
    public async Task NativeSelectionPagingAndBudgetsPreserveOriginalRowsAcrossRestart()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_SCYLLADB_CQL_STATE")
            ?? throw new InvalidOperationException("Persistent source directory required.");
        Assert.IsTrue(Directory.Exists(state) && File.Exists(Path.Combine(state, "environment.json")), "Original admitted environment required; never create a source from a test.");
        using var lease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        var resumePrewrite = File.Exists(pending) && Environment.GetEnvironmentVariable("OASIS_SCYLLADB_RESUME_PREWRITE") == "1";
        Assert.IsTrue(!File.Exists(pending) || resumePrewrite, "Preserve uncertain setup; explicit prewrite readback required.");
        if (resumePrewrite)
        {
            Assert.IsFalse(File.Exists(manifest));
            Assert.AreEqual(0, Directory.GetFiles(state, "source-write-*.ack.json").Length);
        }
        var baseline = Path.Combine(state, "source-snapshot.json");
        if (File.Exists(manifest))
            Assert.IsTrue(File.Exists(baseline), "Original snapshot missing; never bless replacement state.");
        else
            Assert.IsFalse(File.Exists(baseline), "Orphan original snapshot; reconcile without reseeding.");
        using var credentials = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "credentials.json")));
        using var cluster = Cluster.Builder().AddContactPoint("127.0.0.1").WithPort(19043)
            .WithCredentials(credentials.RootElement.GetProperty("username").GetString(),
                credentials.RootElement.GetProperty("password").GetString()).Build();
        using var native = await cluster.ConnectAsync();
        var product = (await native.ExecuteAsync(new SimpleStatement("SELECT release_version FROM system.local"))).Single();
        Assert.IsFalse(string.IsNullOrWhiteSpace(product.GetValue<string>("release_version")));
        using var environment = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "environment.json")));
        StringAssert.StartsWith(environment.RootElement.GetProperty("observations")[0].GetProperty("productVersion").GetString(), "2026.3");
        async Task<long> Count(string sql)
        {
            var row = (await native.ExecuteAsync(new SimpleStatement(sql))).Single();
            return row.GetValue<long>("count");
        }
        var provider = new ScyllaDBOASIS("127.0.0.1", port: 19043,
            username: credentials.RootElement.GetProperty("username").GetString(),
            password: credentials.RootElement.GetProperty("password").GetString());
        Seed seed;
        if (!File.Exists(manifest))
        {
            Assert.AreEqual(0L, await Count("SELECT count(*) FROM system_schema.keyspaces WHERE keyspace_name='oasis'"), "Never adopt existing schema.");
            if (resumePrewrite)
            {
                seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(pending))!;
                using var receipt = new FileStream(Path.Combine(state, "demo-seed.prewrite-resume.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(receipt, new { sourceIntentSha256 = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(pending))), keyspaceAbsent = true, acknowledgedWrites = 0 });
                receipt.Flush(true);
            }
            else
            {
                seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray());
                using var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(intent, seed); intent.Flush(true);
            }
            var active = await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message);
            var names = new[] { "Espresso", "Latte", "Tea" };
            for (var i = 0; i < 3; i++)
            {
                var saved = await provider.SaveHolonAsync(new Holon { Id = seed.Ids[i], Name = names[i],
                    CreatedByAvatarId = seed.Actor, IsNewHolon = true }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message);
                Assert.IsTrue(saved.IsSaved, "Source write requires acknowledgement.");
                using var receipt = new FileStream(Path.Combine(state, $"source-write-{i}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(receipt, new { id = seed.Ids[i], acknowledged = true }); receipt.Flush(true);
            }
            File.Move(pending, manifest);
        }
        else
        {
            seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
            Assert.AreEqual(3L, await Count("SELECT count(*) FROM system_schema.tables WHERE keyspace_name='oasis'"), "Original tables required before activation.");
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }
        var replication = (await native.ExecuteAsync(new SimpleStatement("SELECT replication FROM system_schema.keyspaces WHERE keyspace_name='oasis'"))).Single().GetValue<IDictionary<string, string>>("replication");
        StringAssert.EndsWith(replication["class"], "NetworkTopologyStrategy");
        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        async Task<string[]> Snapshot()
        {
            var records = await native.ExecuteAsync(new SimpleStatement(
                "SELECT id,data_json,writetime(data_json) AS revision FROM oasis.oasis_holons"));
            var rows = new List<string>();
            foreach (var record in records)
            {
                Assert.IsTrue(rows.Count < 3, "Only dedicated source rows may exist.");
                rows.Add(record.GetValue<Guid>("id").ToString("D") + "|" + record.GetValue<long>("revision") + "|" +
                    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(record.GetValue<string>("data_json")))));
            }
            rows.Sort(StringComparer.Ordinal);
            Assert.AreEqual(3, rows.Count, "Missing source is not recreated.");
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
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ScyllaDBOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.ScyllaDBOASIS, ids).Result).Result
                .Where(h => ids.Contains(h.Id)).Result;
            if (paging) query = query.Skip(1).Result.Take(1).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.ScyllaDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.ScyllaDBOASIS, provider.QueryCapabilities) });
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
        CollectionAssert.AreEqual(before, await Snapshot(), "Native JSON and write times must remain unchanged.");
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
