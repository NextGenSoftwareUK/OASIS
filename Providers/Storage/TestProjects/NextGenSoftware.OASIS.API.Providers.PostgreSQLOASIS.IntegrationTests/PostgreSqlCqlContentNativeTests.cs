using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class PostgreSqlCqlContentNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    [TestMethod]
    public async Task ContentQueriesUseNativePredicatesAndPreserveRetainedRows()
    {
        var originalConnection = Environment.GetEnvironmentVariable("OASIS_POSTGRES_CQL_CONNECTION")
            ?? throw new InvalidOperationException("Explicit retained PostgreSQL runner required.");
        var state = Environment.GetEnvironmentVariable("OASIS_POSTGRES_CQL_STATE")
            ?? throw new InvalidOperationException("Persistent source directory required.");
        using var sourceLease = new FileStream(Path.Combine(state, "content-source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "content-seed.json");
        var pending = Path.Combine(state, "content-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve uncertain setup; never replay.");
        var builder = new NpgsqlConnectionStringBuilder(originalConnection);
        Assert.AreEqual("cosmic_cql,pg_catalog", builder.SearchPath);
        await using var original = new NpgsqlConnection(originalConnection);
        await original.OpenAsync();
        async Task<string[]> Snapshot(NpgsqlConnection connection, string table)
        {
            // table is exclusively a fixed test constant, never external input.
            await using var command = new NpgsqlCommand("SELECT id,data_json,xmin::text FROM " + table + " ORDER BY id::text COLLATE \"C\"", connection);
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<string>();
            while (await reader.ReadAsync()) rows.Add(reader.GetGuid(0).ToString("D") + "|" + reader.GetString(2) + "|" +
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(reader.GetString(1)))));
            return rows.ToArray();
        }
        var originalBefore = await Snapshot(original, "cosmic_cql.oasis_holons");
        Assert.AreEqual(3, originalBefore.Length, "The original demo must already exist; never recreate it.");
        builder.SearchPath = "cosmic_cql_content,pg_catalog";
        var provider = new PostgreSQLOASIS(builder.ConnectionString);
        Seed seed;
        const string owner = "cosmic-cql-postgresql-content-v1";
        if (!File.Exists(manifest))
        {
            await using (var exists = new NpgsqlCommand("SELECT count(*) FROM pg_namespace WHERE nspname='cosmic_cql_content'", original))
                Assert.AreEqual(0L, Convert.ToInt64(await exists.ExecuteScalarAsync()), "Never adopt an existing schema.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray());
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            await using (var create = new NpgsqlCommand("CREATE SCHEMA cosmic_cql_content AUTHORIZATION oasis_test; CREATE TABLE cosmic_cql_content.cosmic_test_owner (identity text PRIMARY KEY)", original))
                await create.ExecuteNonQueryAsync();
            await using (var mark = new NpgsqlCommand("INSERT INTO cosmic_cql_content.cosmic_test_owner VALUES (@owner)", original))
            { mark.Parameters.AddWithValue("owner", owner); await mark.ExecuteNonQueryAsync(); }
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var names = new[] { "Espresso", "Latte", "Tea", "Hidden coffee" };
            var descriptions = new[] { "Strong coffee 100%_ café'☕", "Milky coffee", "Black tea", "coffee" };
            for (var index = 0; index < 4; index++)
            {
                var metadata = new Dictionary<string, object> {
                    ["category"] = index == 2 ? "tea" : "coffee",
                    ["location"] = new Dictionary<string, object> { ["city"] = index == 1 ? "Paris" : "London" },
                    ["numericKey"] = new Dictionary<string, object> { ["0"] = index == 0 ? "coffee" : "tea" },
                    ["array"] = JsonDocument.Parse("[\"coffee\"]").RootElement.Clone()
                };
                if (index == 1) metadata["optional"] = null!;
                if (index == 2) metadata["optional"] = 123;
                metadata["unicode"] = index == 0 ? "100%_café'☕" : "other";
                var saved = await provider.SaveHolonAsync(new Holon { Id = seed.Ids[index], Name = names[index],
                    Description = descriptions[index], MetaData = metadata,
                    CreatedByAvatarId = index == 3 ? Guid.NewGuid() : seed.Actor, IsNewHolon = true }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message);
                Assert.IsTrue(saved.IsSaved, "Every source write requires acknowledgement.");
                using var receipt = new FileStream(Path.Combine(state, $"content-write-{index}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(receipt, new { id = seed.Ids[index], acknowledged = true }); receipt.Flush(true);
            }
            File.Move(pending, manifest);
        }
        else
        {
            seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
            await using var marker = new NpgsqlCommand("SELECT identity FROM cosmic_cql_content.cosmic_test_owner", original);
            Assert.AreEqual(owner, (string?)await marker.ExecuteScalarAsync());
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }
        await using var native = new NpgsqlConnection(builder.ConnectionString);
        await native.OpenAsync();
        var before = await Snapshot(native, "cosmic_cql_content.oasis_holons");
        Assert.AreEqual(4, before.Length);
        var snapshotFile = Path.Combine(state, "content-source-snapshot.json");
        if (File.Exists(snapshotFile)) CollectionAssert.AreEqual(JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(snapshotFile))!, before);
        else
        {
            using var snapshot = new FileStream(snapshotFile, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(snapshot, before); snapshot.Flush(true);
        }
        CosmicQuery<Holon> Query() => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.PostgreSQLOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.PostgreSQLOASIS, seed.Ids.Take(3).ToArray()).Result).Result;
        var queries = 0;
        async Task Check(CosmicQuery<Holon> query, params string[] expected)
        {
            var planned = CosmicQueryPlanner.Plan(query, ProviderType.PostgreSQLOASIS,
                new[] { new CosmicQueryTarget(ProviderType.PostgreSQLOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(planned.IsError, planned.Message);
            var loaded = await provider.ExecuteCosmicQueryAsync(planned.Result);
            Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception);
            CollectionAssert.AreEquivalent(expected, loaded.Result.Select(holon => holon.Name).ToArray());
            queries++;
        }
        await Check(Query().Where(h => h.Name == "Espresso").Result, "Espresso");
        await Check(Query().Where(h => h.Name.Contains("ss")).Result, "Espresso");
        await Check(Query().Where(h => h.Description.Contains("coffee")).Result, "Espresso", "Latte");
        await Check(Query().Where(h => h.Description.Contains("%" )).Result, "Espresso");
        await Check(Query().Where(h => h.Description.Contains("_")).Result, "Espresso");
        await Check(Query().Where(h => h.Description.Contains("é'☕")).Result, "Espresso");
        await Check(Query().Where(h => h.Name == "espresso").Result);
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "category") == "coffee").Result, "Espresso", "Latte");
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "location", "city") == "London").Result, "Espresso", "Tea");
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "numericKey", "0") == "coffee").Result, "Espresso");
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "array", "0") == "coffee").Result);
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "optional") == "123").Result);
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "missing") == "coffee").Result);
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "unicode") == "100%_café'☕").Result, "Espresso");
        await Check(Query().Where(h => (h.Name == "Espresso" || h.Name == "Tea") &&
            CosmicQueryMetadata.Value<string>(h, "location", "city") == "London").Result, "Espresso", "Tea");
        await Check(Query().Where(h => h.Name == "Hidden coffee" || CosmicQueryMetadata.Value<string>(h, "category") == "coffee").Result, "Espresso", "Latte");
        var namesById = new Dictionary<Guid, string> { [seed.Ids[0]] = "Espresso", [seed.Ids[1]] = "Latte" };
        var secondCoffee = seed.Ids.Take(2).OrderBy(id => id.ToString("D"), StringComparer.Ordinal).Skip(1).First();
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "category") == "coffee").Result.Skip(1).Result.Take(1).Result, namesById[secondCoffee]);
        Assert.AreEqual(17, queries);
        CollectionAssert.AreEqual(before, await Snapshot(native, "cosmic_cql_content.oasis_holons"), "Content queries mutated source JSON or row versions.");
        CollectionAssert.AreEqual(originalBefore, await Snapshot(original, "cosmic_cql.oasis_holons"), "Original demo changed.");
    }
}
