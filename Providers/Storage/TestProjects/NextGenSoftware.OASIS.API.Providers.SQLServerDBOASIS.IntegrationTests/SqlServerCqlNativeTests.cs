using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.SqlClient;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class SqlServerCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    [TestMethod]
    public async Task NativeSelectionPagingAndBudgetsPreserveOriginalRowsAcrossRestart()
    {
        var connectionString = Environment.GetEnvironmentVariable("OASIS_SQLSERVER_CQL_CONNECTION")
            ?? throw new InvalidOperationException("Explicit retained SQL Server CQL runner required.");
        var state = Environment.GetEnvironmentVariable("OASIS_SQLSERVER_CQL_STATE")
            ?? throw new InvalidOperationException("Persistent source directory required.");
        Directory.CreateDirectory(state);
        using var lease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve uncertain setup; never replay.");
        var options = new SqlConnectionStringBuilder(connectionString);
        Assert.AreEqual("cosmic_cql_demo", options.InitialCatalog);
        await using var native = new SqlConnection(connectionString);

        async Task<long> Count(string sql)
        {
            await using var command = new SqlCommand(sql, native);
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        var provider = new SQLServerDBOASIS(connectionString);
        Seed seed;
        if (!File.Exists(manifest))
        {
            var adminOptions = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
            await using var admin = new SqlConnection(adminOptions.ConnectionString);
            await admin.OpenAsync();
            await using (var check = new SqlCommand("SELECT count(*) FROM sys.databases WHERE name='cosmic_cql_demo'", admin))
                Assert.AreEqual(0, Convert.ToInt32(await check.ExecuteScalarAsync()), "Never adopt existing database.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray());
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            await using (var command = admin.CreateCommand())
            {
                // CREATE DATABASE file names are SQL string literals, not bindable
                // values. Use fixed owned paths and SQL literal escaping below.
                command.CommandText = "CREATE DATABASE cosmic_cql_demo ON PRIMARY (NAME=cosmic_cql_demo, FILENAME=N'" + Path.Combine(state, "source.mdf").Replace("'", "''") + "') LOG ON (NAME=cosmic_cql_demo_log, FILENAME=N'" + Path.Combine(state, "source_log.ldf").Replace("'", "''") + "')";
                await command.ExecuteNonQueryAsync();
            }
            await native.OpenAsync();
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
                Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception);
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
            await native.OpenAsync();
            Assert.AreEqual(3L, await Count("SELECT count(*) FROM sys.tables WHERE name IN ('OASISHolons','OASISAvatars','OASISAvatarDetails')"), "Original tables required before activation.");
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }
        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        async Task<string[]> Snapshot()
        {
            await using var command = new SqlCommand("SELECT Id,DataJson,CONVERT(varchar(33),ModifiedDate,126) FROM OASISHolons ORDER BY CONVERT(char(36),Id) COLLATE Latin1_General_100_BIN2", native);
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
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SQLServerDBOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.SQLServerDBOASIS, ids).Result).Result
                .Where(h => ids.Contains(h.Id)).Result;
            if (paging) query = query.Skip(1).Result.Take(1).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.SQLServerDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.SQLServerDBOASIS, provider.QueryCapabilities) });
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
