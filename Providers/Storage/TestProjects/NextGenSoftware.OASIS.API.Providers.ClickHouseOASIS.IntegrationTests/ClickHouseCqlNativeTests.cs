using ClickHouse.Client.ADO;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class ClickHouseCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    [TestMethod]
    public async Task NativeSelectionPagingAndBudgetsPreserveOriginalJsonAndUnsignedVersionAcrossRestart()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_CLICKHOUSE_CQL_STATE")
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
        Assert.AreEqual(Path.Combine(state, "ca.pem"), Environment.GetEnvironmentVariable("SSL_CERT_FILE"));
        var connectionString = $"Host=localhost;Port=18443;Protocol=https;Database=default;Username={user};Password={password}";
        using var connection = new ClickHouseConnection(connectionString);
        await connection.OpenAsync();
        var provider = new ClickHouseOASIS(connectionString);
        async Task<string> Scalar(string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 20;
            return Convert.ToString(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture)!;
        }
        using var environment = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "environment.json")));
        Assert.AreEqual(environment.RootElement.GetProperty("identity").GetProperty("toString(serverUUID())").GetString(),
            await Scalar("SELECT toString(serverUUID())"));
        async Task<long> Count() => long.Parse(await Scalar("SELECT count() FROM oasis.holons FINAL"), CultureInfo.InvariantCulture);

        Seed seed;
        if (!File.Exists(manifest))
        {
            Assert.AreEqual("0", await Scalar("SELECT count() FROM system.databases WHERE name='oasis'"), "Never adopt pre-existing business schema.");
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
                using var receipt = new FileStream(Path.Combine(state, $"source-write-{i}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(receipt, new { id = seed.Ids[i], acknowledged = true }); receipt.Flush(true);
            }
            File.Move(pending, manifest);
        }
        else
        {
            seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }

        async Task<string[]> Snapshot()
        {
            Assert.AreEqual(3L, await Count(), "Only original three source records may exist.");
            var rows = new List<string>();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id,data_json,_version FROM oasis.holons FINAL ORDER BY id LIMIT 4";
            command.CommandTimeout = 20;
            using (var reader = await command.ExecuteReaderAsync())
            while (await reader.ReadAsync())
            {
                var id = reader.GetGuid(0);
                Assert.IsTrue(seed.Ids.Contains(id));
                var json = reader.GetString(1);
                Assert.IsInstanceOfType(reader.GetValue(2), typeof(ulong), "Native UInt64 must never pass through floating point.");
                var version = (ulong)reader.GetValue(2);
                Assert.IsTrue(version > 0);
                using var payload = JsonDocument.Parse(json);
                Assert.AreEqual(id, payload.RootElement.GetProperty("Id").GetGuid());
                rows.Add(id.ToString("D")+"|"+version.ToString(CultureInfo.InvariantCulture)+"|"+
                    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
            }
            Assert.AreEqual(3, rows.Count);
            rows.Sort(StringComparer.Ordinal);
            rows.Insert(0, "table|"+await Scalar("SELECT toString(uuid) FROM system.tables WHERE database='oasis' AND name='holons'"));
            rows.Insert(0, "database|"+await Scalar("SELECT toString(uuid) FROM system.databases WHERE name='oasis'"));
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
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ClickHouseOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.ClickHouseOASIS, ids).Result).Result
                .Where(h => ids.Contains(h.Id)).Result;
            if (paging) query = query.Skip(1).Result.Take(1).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.ClickHouseOASIS,
                new[] { new CosmicQueryTarget(ProviderType.ClickHouseOASIS, provider.QueryCapabilities) });
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
        CollectionAssert.AreEqual(before, await Snapshot(), "Query must preserve JSON and exact unsigned version.");
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
