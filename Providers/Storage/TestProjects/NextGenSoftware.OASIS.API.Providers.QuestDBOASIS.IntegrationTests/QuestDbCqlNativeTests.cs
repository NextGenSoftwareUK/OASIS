using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class QuestDbCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);
    private sealed class OriginalWithObsoleteField : Holon { public string ObsoleteField { get; set; } = "removed by replacement"; }

    [TestMethod]
    public async Task NativeBoundedQueriesAndOriginalJsonSurviveRestart()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_QUESTDB_CQL_STATE") ?? throw new InvalidOperationException("Retained source required.");
        Assert.IsTrue(OperatingSystem.IsLinux());
        Assert.IsTrue(File.Exists(Path.Combine(state, "environment.json")));
        using var sourceLock = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        var baseline = Path.Combine(state, "source-snapshot.json");
        Assert.IsFalse(File.Exists(pending), "Unknown write outcome must not cause replay.");
        Assert.AreEqual(File.Exists(manifest), File.Exists(baseline), "Preserve original snapshot; never bless replacement state.");
        using var auth = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "credentials.json")));
        var settings = new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1", Port = 18812, Database = "qdb", Username = auth.RootElement.GetProperty("username").GetString(),
            Password = auth.RootElement.GetProperty("password").GetString(), Pooling = false, Timeout = 10, CommandTimeout = 15
        };
        using var native = new NpgsqlConnection(settings.ConnectionString);
        await native.OpenAsync();
        var provider = new QuestDBOASIS(settings.ConnectionString);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message + activation.Exception);

        async Task WaitForAppliedWal()
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (true)
            {
                using var command = new NpgsqlCommand("SELECT writerTxn,sequencerTxn,suspended FROM wal_tables() WHERE name='oasis_holons';", native);
                using var reader = await command.ExecuteReaderAsync(deadline.Token);
                Assert.IsTrue(await reader.ReadAsync(deadline.Token));
                Assert.IsFalse(reader.GetBoolean(2), "Native WAL writer suspended.");
                var applied = reader.GetInt64(0) == reader.GetInt64(1);
                Assert.IsFalse(await reader.ReadAsync(deadline.Token));
                if (applied) return;
                // Documented WAL visibility observation only; no write resubmission.
                await Task.Delay(100, deadline.Token);
            }
        }

        Seed seed;
        if (!File.Exists(manifest))
        {
            using (var empty = new NpgsqlCommand("SELECT count(*) FROM oasis_holons;", native))
                Assert.AreEqual(0L, (long)(await empty.ExecuteScalarAsync())!);
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray());
            using (var file = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(file, seed); file.Flush(true); }
            var names = new[] { "Espresso", "Latte", "Tea" };
            for (var index = 0; index < 3; index++)
            {
                Holon original = index == 0 ? new OriginalWithObsoleteField() : new Holon();
                original.Id = seed.Ids[index]; original.Name = names[index]; original.CreatedByAvatarId = seed.Actor;
                original.MetaData = new() { ["precise"] = 1234567890.123456789012345678m };
                var saved = await provider.SaveHolonAsync(original, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved);
                using var receipt = new FileStream(Path.Combine(state, $"source-write-{index}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(receipt, new { id = original.Id, acknowledged = true }); receipt.Flush(true);
            }
            var updated = await provider.SaveHolonAsync(new Holon { Id = seed.Ids[0], Name = "Espresso updated", CreatedByAvatarId = seed.Actor,
                MetaData = new() { ["precise"] = 1234567890.123456789012345678m } }, saveChildren: false);
            Assert.IsFalse(updated.IsError, updated.Message + updated.Exception);
            Assert.IsTrue(updated.IsSaved);
            using (var receipt = new FileStream(Path.Combine(state, "source-update.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(receipt, new { id = seed.Ids[0], acknowledged = true }); receipt.Flush(true); }
            await WaitForAppliedWal();
            File.Move(pending, manifest);
        }
        else seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
        await WaitForAppliedWal();

        async Task<string[]> Snapshot()
        {
            var proof = new List<string>();
            using (var command = new NpgsqlCommand("SELECT id,data,cast(ts AS LONG) FROM oasis_holons ORDER BY id LIMIT 4;", native))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var id = Guid.ParseExact(reader.GetString(0), "D");
                    var json = reader.GetString(1);
                    Assert.IsTrue(seed.Ids.Contains(id)); Assert.AreEqual(0L, reader.GetInt64(2));
                    using var payload = JsonDocument.Parse(json);
                    Assert.AreEqual(id, payload.RootElement.GetProperty("id").GetGuid());
                    Assert.AreEqual(1234567890.123456789012345678m, payload.RootElement.GetProperty("metaData").GetProperty("precise").GetDecimal());
                    if (id == seed.Ids[0]) Assert.IsFalse(payload.RootElement.TryGetProperty("obsoleteField", out _));
                    proof.Add(id.ToString("D") + "|" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
                }
            }
            Assert.AreEqual(3, proof.Count); Assert.AreEqual(3, proof.Distinct().Count());
            proof.Sort(StringComparer.Ordinal);
            using (var command = new NpgsqlCommand("SELECT table_name,id FROM tables() WHERE table_name IN ('oasis_avatars','oasis_avatar_details','oasis_holons') ORDER BY table_name;", native))
            using (var reader = await command.ExecuteReaderAsync())
            { while (await reader.ReadAsync()) proof.Add("native-table|" + reader.GetString(0) + "|" + reader.GetInt32(1)); }
            Assert.AreEqual(6, proof.Count);
            return proof.ToArray();
        }
        var before = await Snapshot();
        if (File.Exists(baseline)) CollectionAssert.AreEqual(JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(baseline))!, before);
        else
        {
            using var file = new FileStream(baseline, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(file, before); file.Flush(true);
        }
        CosmicQueryPlan<Holon> Plan(Guid[] selected, bool paging = false)
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.QuestDBOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.QuestDBOASIS, seed.Ids).Result).Result
                .Where(row => selected.Contains(row.Id)).Result;
            if (paging) query = query.Skip(1).Result.Take(1).Result;
            var planned = CosmicQueryPlanner.Plan(query, ProviderType.QuestDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.QuestDBOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(planned.IsError, planned.Message);
            return planned.Result;
        }
        foreach (var selected in new[] { seed.Ids, seed.Ids.Take(2).ToArray(), new[] { Guid.NewGuid() } })
        {
            var result = await provider.ExecuteCosmicQueryAsync(Plan(selected));
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEquivalent(selected.Length == 1 ? Array.Empty<Guid>() : selected, result.Result.Select(row => row.Id).ToArray());
        }
        var page = await provider.ExecuteCosmicQueryAsync(Plan(seed.Ids, true));
        Assert.IsFalse(page.IsError, page.Message + page.Exception);
        Assert.AreEqual(seed.Ids.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).Skip(1).First(), page.Result.Single().Id);
        var limited = await provider.ExecuteCosmicQueryAsync(Plan(seed.Ids), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", limited.ErrorCode); Assert.IsNull(limited.Result);
        CollectionAssert.AreEqual(before, await Snapshot());
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
