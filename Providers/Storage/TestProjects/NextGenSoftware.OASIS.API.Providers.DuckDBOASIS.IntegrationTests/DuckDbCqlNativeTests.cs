using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DuckDB.NET.Data;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.DuckDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class DuckDbCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids, Guid DeletedId);

    [TestMethod]
    public async Task NativeSelectionPagingAndBudgetsPreserveOriginalRowsAcrossRestart()
    {
        var connectionString = Environment.GetEnvironmentVariable("OASIS_DUCKDB_CQL_CONNECTION")
            ?? throw new InvalidOperationException("Explicit retained DuckDB CQL runner required.");
        var state = Environment.GetEnvironmentVariable("OASIS_DUCKDB_CQL_STATE")
            ?? throw new InvalidOperationException("Persistent source directory required.");
        Directory.CreateDirectory(state);
        using var lease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve uncertain setup; never replay.");
        var source = Path.Combine(state, "source.duckdb");
        Assert.AreEqual("Data Source=" + source, connectionString);
        DuckDBConnection? native = null;
        var provider = new DuckDBOASIS(connectionString);
        Seed seed;
        if (!File.Exists(manifest))
        {
            Assert.IsFalse(File.Exists(source), "Never adopt an existing file.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray(), Guid.NewGuid());
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }

            var active = await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message);
            var names = new[] { "Espresso", "Latte", "Tea" };
            for (var i = 0; i < 4; i++)
            {
                var saved = await provider.SaveHolonAsync(new Holon { Id = i < 3 ? seed.Ids[i] : seed.DeletedId, Name = i < 3 ? names[i] : "Deleted source", DeletedDate = i == 3 ? DateTime.UtcNow : DateTime.MinValue,
                    CreatedByAvatarId = seed.Actor, IsNewHolon = true }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message);
                Assert.IsTrue(saved.IsSaved, "Source write requires acknowledgement.");
                Assert.AreEqual(1, saved.SavedCount);
                if (i < 3)
                {
                    var loaded = await provider.LoadHolonAsync(seed.Ids[i], loadChildren: false);
                    Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception);
                    Assert.AreEqual(names[i], loaded.Result.Name);
                    Assert.AreEqual(seed.Actor, loaded.Result.CreatedByAvatarId);
                }
                using var receipt = new FileStream(Path.Combine(state, $"source-write-{i}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(receipt, new { id = i < 3 ? seed.Ids[i] : seed.DeletedId, acknowledged = true }); receipt.Flush(true);
            }
            File.Move(pending, manifest);
        }
        else
        {
            seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
            Assert.IsTrue(File.Exists(source), "Original file missing; never recreate it.");
            await using var original = new DuckDBConnection(connectionString);
            await original.OpenAsync();
            await using var check = original.CreateCommand();
            check.CommandText = "SELECT count(*) FROM information_schema.tables WHERE table_name IN ('oasis_holons','oasis_avatars','oasis_avatar_details')";
            Assert.AreEqual(3L, Convert.ToInt64(await check.ExecuteScalarAsync()), "Original tables required before activation.");
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }
        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        await using var inspection = new DuckDBConnection(connectionString);
        await inspection.OpenAsync();
        native = inspection;
        async Task<string[]> Snapshot()
        {
            await using var command = native.CreateCommand();
            command.CommandText = "SELECT id,doc,CAST(updated_at AS VARCHAR) FROM oasis_holons ORDER BY id";
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<string>();
            while (await reader.ReadAsync())
            {
                Assert.IsTrue(rows.Count < 4, "Only dedicated source rows may exist.");
                rows.Add(reader.GetString(0) + "|" + reader.GetString(2) + "|" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(reader.GetString(1)))));
            }
            Assert.AreEqual(4, rows.Count, "Missing source is not recreated.");
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
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.DuckDBOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.DuckDBOASIS, ids).Result).Result
                .Where(h => ids.Contains(h.Id)).Result;
            if (paging) query = query.Skip(1).Result.Take(1).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.DuckDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.DuckDBOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            return plan.Result;
        }
        foreach (var ids in new[] { seed.Ids, seed.Ids.Take(2).ToArray(), new[] { Guid.NewGuid() }, new[] { seed.DeletedId } })
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
