using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.CockroachDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class CockroachDbCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    [TestMethod]
    public async Task NativeSelectionPagingAndBudgetsPreserveOriginalRowsAcrossRestart()
    {
        var connectionString = Environment.GetEnvironmentVariable("OASIS_COCKROACH_CQL_CONNECTION")
            ?? throw new InvalidOperationException("Explicit retained CockroachDB CQL runner required.");
        var state = Environment.GetEnvironmentVariable("OASIS_COCKROACH_CQL_STATE")
            ?? throw new InvalidOperationException("Persistent source directory required.");
        Assert.IsTrue(Directory.Exists(state) && File.Exists(Path.Combine(state, "environment.json")), "Original admitted environment required; never create a source from a test.");
        using var lease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve uncertain setup; never replay.");
        var baseline = Path.Combine(state, "source-snapshot.json");
        if (File.Exists(manifest))
            Assert.IsTrue(File.Exists(baseline), "Original snapshot missing; never bless replacement state.");
        else
            Assert.IsFalse(File.Exists(baseline), "Orphan original snapshot; reconcile without reseeding.");
        var options = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.AreEqual("cosmic_cql,pg_catalog", options.SearchPath);
        await using var native = new NpgsqlConnection(connectionString);
        await native.OpenAsync();
        async Task<long> Count(string sql)
        {
            await using var command = new NpgsqlCommand(sql, native);
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        var provider = new CockroachDBOASIS(connectionString);
        Seed seed;
        if (!File.Exists(manifest))
        {
            Assert.AreEqual(0L, await Count("SELECT count(*) FROM pg_namespace WHERE nspname='cosmic_cql'"), "Never adopt existing schema.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray());
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            await using (var command = new NpgsqlCommand("CREATE SCHEMA cosmic_cql AUTHORIZATION root", native))
                await command.ExecuteNonQueryAsync();
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
            Assert.AreEqual(3L, await Count("SELECT count(*) FROM information_schema.tables WHERE table_schema='cosmic_cql' AND table_name IN ('oasis_holons','oasis_avatars','oasis_avatar_details')"), "Original tables required before activation.");
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }
        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        async Task<string[]> Snapshot()
        {
            await using var command = new NpgsqlCommand("SELECT id,data_json,crdb_internal_mvcc_timestamp::STRING FROM cosmic_cql.oasis_holons ORDER BY id", native);
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<string>();
            while (await reader.ReadAsync())
            {
                Assert.IsTrue(rows.Count < 3, "Only dedicated source rows may exist.");
                rows.Add(reader.GetGuid(0).ToString("D") + "|" + reader.GetString(2) + "|" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(reader.GetString(1)))));
            }
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
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.CockroachDBOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.CockroachDBOASIS, ids).Result).Result
                .Where(h => ids.Contains(h.Id)).Result;
            if (paging) query = query.Skip(1).Result.Take(1).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.CockroachDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.CockroachDBOASIS, provider.QueryCapabilities) });
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
        CollectionAssert.AreEqual(before, await Snapshot(), "Native JSON and row versions must remain unchanged.");
    }
}
