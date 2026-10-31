using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.LitestreamOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class LitestreamReplicationTests
{
    private static string State => Environment.GetEnvironmentVariable("OASIS_LITESTREAM_CQL_STATE")
        ?? throw new InvalidOperationException("Explicit replication runner required.");

    [TestMethod]
    public async Task WriteOwnedProbeDuringNativeReplication()
    {
        Assert.AreEqual("1", Environment.GetEnvironmentVariable("OASIS_LITESTREAM_REPLICATION_EXEC"));
        var state = State;
        var replica = Path.Combine(state, "file-replica");
        var pending = Path.Combine(state, "replication-probe.pending.json");
        var receipt = Path.Combine(state, "replication-probe.json");
        Assert.IsFalse(File.Exists(pending) || File.Exists(receipt), "Original probe must not be replayed.");
        string[] Files() => Directory.Exists(replica) ? Directory.GetFiles(replica, "*.ltx", SearchOption.AllDirectories) : Array.Empty<string>();
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (Files().Length == 0 && DateTime.UtcNow < deadline) await Task.Delay(200);
        Assert.IsTrue(Files().Length > 0, "Native initial replica must exist before the live probe write.");
        var before = Files().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var marker = Guid.NewGuid().ToString("D");
        using (var file = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        { JsonSerializer.Serialize(file, new { marker }); file.Flush(true); }
        await using var connection = new SqliteConnection("Data Source=" + Path.Combine(state, "source.db") + ";Pooling=false");
        await connection.OpenAsync();
        var inheritedPath = Path.Combine(state, "inherited-replication-probe.json");
        await using (var check = connection.CreateCommand())
        {
            if (File.Exists(inheritedPath))
            {
                using var inherited = JsonDocument.Parse(await File.ReadAllTextAsync(inheritedPath));
                check.CommandText = "SELECT marker FROM cosmic_cql_replication_probe";
                await using var reader = await check.ExecuteReaderAsync();
                Assert.IsTrue(await reader.ReadAsync());
                Assert.AreEqual(inherited.RootElement.GetProperty("marker").GetString(), reader.GetString(0));
                Assert.IsFalse(await reader.ReadAsync(), "Only the admitted inherited probe may exist.");
            }
            else
            {
                check.CommandText = "SELECT count(*) FROM sqlite_master WHERE name='cosmic_cql_replication_probe'";
                Assert.AreEqual(0L, (long)(await check.ExecuteScalarAsync())!, "Never adopt unowned probe state.");
            }
        }
        await using (var transaction = await connection.BeginTransactionAsync())
        {
            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = "CREATE TABLE IF NOT EXISTS cosmic_cql_replication_probe(marker TEXT PRIMARY KEY); INSERT INTO cosmic_cql_replication_probe(marker) VALUES($marker)";
            command.Parameters.AddWithValue("$marker", marker);
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        // A second native LTX object is required after the committed write. This
        // proves live replication, rather than merely an initial file snapshot.
        deadline = DateTime.UtcNow.AddSeconds(30);
        while (!Files().Any(path => !before.Contains(path)) && DateTime.UtcNow < deadline) await Task.Delay(200);
        Assert.IsTrue(Files().Any(path => !before.Contains(path)), "Committed probe did not reach a subsequent native LTX object.");
        File.Move(pending, receipt);
    }

    [TestMethod]
    public async Task RestoredReplicaPreservesOriginalHolonsAndLiveProbe()
    {
        var state = State;
        var restored = Path.Combine(state, "restored.db");
        Assert.IsTrue(File.Exists(restored), "Actual native restore required.");
        Assert.IsFalse(File.Exists(Path.Combine(state, "replication-probe.pending.json")));
        using var seed = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "demo-seed.json")));
        using var probe = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "replication-probe.json")));
        var actor = seed.RootElement.GetProperty("Actor").GetGuid();
        var ids = seed.RootElement.GetProperty("Ids").EnumerateArray().Select(v => v.GetGuid()).ToArray();
        await using var native = new SqliteConnection("Data Source=" + restored + ";Mode=ReadOnly;Pooling=false");
        await native.OpenAsync();
        var rows = new List<string>();
        await using (var command = native.CreateCommand())
        {
            command.CommandText = "SELECT id,data_json,CAST(is_deleted AS TEXT) FROM oasis_holons ORDER BY id";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) rows.Add(reader.GetString(0) + "|" + reader.GetString(2) + "|" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(reader.GetString(1)))));
        }
        CollectionAssert.AreEqual(JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(Path.Combine(state, "source-snapshot.json")))!, rows.ToArray());
        await using (var command = native.CreateCommand())
        {
            command.CommandText = "SELECT marker FROM cosmic_cql_replication_probe ORDER BY marker";
            await using var reader = await command.ExecuteReaderAsync();
            var observedMarkers = new List<string>();
            while (await reader.ReadAsync()) observedMarkers.Add(reader.GetString(0));
            var expectedMarkers = new List<string> { probe.RootElement.GetProperty("marker").GetString()! };
            var inheritedPath = Path.Combine(state, "inherited-replication-probe.json");
            if (File.Exists(inheritedPath))
            {
                using var inherited = JsonDocument.Parse(await File.ReadAllTextAsync(inheritedPath));
                expectedMarkers.Add(inherited.RootElement.GetProperty("marker").GetString()!);
            }
            CollectionAssert.AreEquivalent(expectedMarkers, observedMarkers, "Preserve inherited and new probe identities.");
        }
        // Query through the shipping adapter, against the restored native file.
        // Tables are already verified; activation must not manufacture source data.
        var provider = new LitestreamOASIS(restored);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        foreach (var selected in new[] { ids, ids.Take(2).ToArray() })
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.LitestreamOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.LitestreamOASIS, ids).Result).Result
                .Where(row => selected.Contains(row.Id)).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.LitestreamOASIS,
                new[] { new CosmicQueryTarget(ProviderType.LitestreamOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Message);
            CollectionAssert.AreEquivalent(selected, result.Result.Select(row => row.Id).ToArray());
        }
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
