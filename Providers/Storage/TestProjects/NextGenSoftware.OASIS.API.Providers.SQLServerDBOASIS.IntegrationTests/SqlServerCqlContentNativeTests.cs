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
public class SqlServerCqlContentNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    [TestMethod]
    public async Task ContentQueriesUseNativePredicatesAndPreserveRetainedRows()
    {
        var originalConnection = Environment.GetEnvironmentVariable("OASIS_SQLSERVER_CQL_CONNECTION")
            ?? throw new InvalidOperationException("Explicit retained SQL Server runner required.");
        var state = Environment.GetEnvironmentVariable("OASIS_SQLSERVER_CQL_STATE")
            ?? throw new InvalidOperationException("Persistent source directory required.");
        using var sourceLease = new FileStream(Path.Combine(state, "content-source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "content-seed.json");
        var pending = Path.Combine(state, "content-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve uncertain setup; never replay.");
        var builder = new SqlConnectionStringBuilder(originalConnection);
        Assert.AreEqual("cosmic_cql_demo", builder.InitialCatalog);
        await using var original = new SqlConnection(originalConnection);
        await original.OpenAsync();
        async Task<string[]> Snapshot(SqlConnection connection, string table, Guid[]? identities = null)
        {
            // table is exclusively a fixed test constant, never external input.
            var filter = identities == null ? "" : " WHERE Id IN (" + string.Join(",", identities.Select((_, index) => "@id" + index)) + ")";
            await using var command = new SqlCommand("SELECT Id,DataJson,CONVERT(varchar(33),ModifiedDate,126) FROM " + table + filter + " ORDER BY CONVERT(char(36),Id) COLLATE Latin1_General_100_BIN2", connection);
            if (identities != null) for (var index = 0; index < identities.Length; index++) command.Parameters.AddWithValue("@id" + index, identities[index]);
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<string>();
            while (await reader.ReadAsync()) rows.Add(reader.GetGuid(0).ToString("D") + "|" + reader.GetString(2) + "|" +
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(reader.GetString(1)))));
            return rows.ToArray();
        }
        var originalBefore = await Snapshot(original, "dbo.OASISHolons");
        Assert.AreEqual(3, originalBefore.Length, "The original demo must already exist; never recreate it.");
        builder.InitialCatalog = "cosmic_cql_content";
        var provider = new SQLServerDBOASIS(builder.ConnectionString);
        Seed seed;
        const string owner = "cosmic-cql-sqlserver-content-v1";
        if (!File.Exists(manifest))
        {
            var adminOptions = new SqlConnectionStringBuilder(originalConnection) { InitialCatalog = "master" };
            await using var admin = new SqlConnection(adminOptions.ConnectionString);
            await admin.OpenAsync();
            await using (var exists = new SqlCommand("SELECT count(*) FROM sys.databases WHERE name='cosmic_cql_content'", admin))
                Assert.AreEqual(0, Convert.ToInt32(await exists.ExecuteScalarAsync()), "Never adopt an existing database.");
            var dataFile = Path.GetFullPath(Path.Combine(state, "content-source.mdf"));
            var logFile = Path.GetFullPath(Path.Combine(state, "content-source_log.ldf"));
            Assert.IsFalse(File.Exists(dataFile) || File.Exists(logFile), "Never overwrite retained database files.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray());
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            // Database identity is fixed; CREATE DATABASE paths are escaped SQL literals.
            await using (var create = new SqlCommand("CREATE DATABASE cosmic_cql_content ON PRIMARY (NAME=cosmic_cql_content,FILENAME=N'" +
                dataFile.Replace("'", "''") + "',SIZE=8MB) LOG ON (NAME=cosmic_cql_content_log,FILENAME=N'" +
                logFile.Replace("'", "''") + "',SIZE=8MB)", admin)) await create.ExecuteNonQueryAsync();
            await using var contentOwner = new SqlConnection(builder.ConnectionString);
            await contentOwner.OpenAsync();
            await using (var create = new SqlCommand("CREATE TABLE dbo.CosmicTestOwner (IdentityTag nvarchar(100) PRIMARY KEY)", contentOwner))
                await create.ExecuteNonQueryAsync();
            await using (var mark = new SqlCommand("INSERT INTO dbo.CosmicTestOwner VALUES (@owner)", contentOwner))
            { mark.Parameters.AddWithValue("@owner", owner); await mark.ExecuteNonQueryAsync(); }
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
            await using var contentOwner = new SqlConnection(builder.ConnectionString);
            await contentOwner.OpenAsync();
            await using var marker = new SqlCommand("SELECT IdentityTag FROM dbo.CosmicTestOwner", contentOwner);
            Assert.AreEqual(owner, (string?)await marker.ExecuteScalarAsync());
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }
        await using var native = new SqlConnection(builder.ConnectionString);
        await native.OpenAsync();
        var before = await Snapshot(native, "dbo.OASISHolons", seed.Ids);
        Assert.AreEqual(4, before.Length);
        var snapshotFile = Path.Combine(state, "content-source-snapshot.json");
        if (File.Exists(snapshotFile)) CollectionAssert.AreEqual(JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(snapshotFile))!, before);
        else
        {
            using var snapshot = new FileStream(snapshotFile, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(snapshot, before); snapshot.Flush(true);
        }
        // The initial four-row fixture is immutable. Append two separately
        // acknowledged long-value records; never rewrite/reseed its original rows.
        var longManifest = Path.Combine(state, "content-long-seed.json");
        var longPending = Path.Combine(state, "content-long-seed.pending.json");
        Assert.IsFalse(File.Exists(longPending), "Preserve uncertain fixture extension; never replay.");
        Seed longSeed;
        if (!File.Exists(longManifest))
        {
            Assert.AreEqual(4, (await Snapshot(native, "dbo.OASISHolons")).Length, "Never adopt unexplained fixture rows.");
            longSeed = new Seed(seed.Actor, new[] { Guid.NewGuid(), Guid.NewGuid() });
            using (var intent = new FileStream(longPending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, longSeed); intent.Flush(true); }
            for (var index = 0; index < 2; index++)
            {
                var value = new string('x', 4001) + (index == 0 ? "" : " ");
                var saved = await provider.SaveHolonAsync(new Holon { Id = longSeed.Ids[index],
                    Name = index == 0 ? "Long exact" : "Long padded", Description = "",
                    MetaData = new Dictionary<string, object> { ["long"] = value },
                    CreatedByAvatarId = seed.Actor, IsNewHolon = true }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message);
                Assert.IsTrue(saved.IsSaved);
                using var receipt = new FileStream(Path.Combine(state, $"content-long-write-{index}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(receipt, new { id = longSeed.Ids[index], acknowledged = true }); receipt.Flush(true);
            }
            File.Move(longPending, longManifest);
        }
        else longSeed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(longManifest))!;
        Assert.AreEqual(seed.Actor, longSeed.Actor);
        var extendedBefore = await Snapshot(native, "dbo.OASISHolons");
        Assert.AreEqual(6, extendedBefore.Length);
        var extendedSnapshot = Path.Combine(state, "content-extended-source-snapshot.json");
        if (File.Exists(extendedSnapshot)) CollectionAssert.AreEqual(JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(extendedSnapshot))!, extendedBefore);
        else
        {
            using var snapshot = new FileStream(extendedSnapshot, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(snapshot, extendedBefore); snapshot.Flush(true);
        }
        CosmicQuery<Holon> Query() => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SQLServerDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.SQLServerDBOASIS, seed.Ids.Take(3).Concat(longSeed.Ids).ToArray()).Result).Result;
        var queries = 0;
        async Task Check(CosmicQuery<Holon> query, params string[] expected)
        {
            var planned = CosmicQueryPlanner.Plan(query, ProviderType.SQLServerDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.SQLServerDBOASIS, provider.QueryCapabilities) });
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
        await Check(Query().Where(h => h.Name == "Espresso ").Result);
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "category") == "coffee ").Result);
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "category ") == "coffee").Result);
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "Category") == "coffee").Result);
        var longValue = new string('x', 4001);
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "long") == longValue).Result, "Long exact");
        var longValueWithSpace = longValue + " ";
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "long") == longValueWithSpace).Result, "Long padded");
        Assert.AreEqual(23, queries);
        CollectionAssert.AreEqual(extendedBefore, await Snapshot(native, "dbo.OASISHolons"), "Extended fixture changed.");
        CollectionAssert.AreEqual(before, await Snapshot(native, "dbo.OASISHolons", seed.Ids), "Content queries mutated source JSON or modified timestamps.");
        CollectionAssert.AreEqual(originalBefore, await Snapshot(original, "dbo.OASISHolons"), "Original demo changed.");
    }
}
