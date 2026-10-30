using InfluxDB.Client;
using WritePrecision = InfluxDB.Client.Api.Domain.WritePrecision;
using InfluxDB.Client.Writes;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class InfluxDBCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids, Guid TiedId);

    [TestMethod]
    public async Task NativeSelectionPagingAndBudgetsPreserveOriginalJsonAndNativeTimestampsAcrossRestart()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_INFLUXDB_CQL_STATE")
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
        Assert.AreEqual(Path.Combine(state, "ca.pem"), Environment.GetEnvironmentVariable("SSL_CERT_FILE"));
        var token = credentials.RootElement.GetProperty("token").GetString()!;
        var org = credentials.RootElement.GetProperty("org").GetString()!;
        var bucket = credentials.RootElement.GetProperty("bucket").GetString()!;
        using var client = InfluxDBClientFactory.Create("https://localhost:18086", token.ToCharArray());
        var provider = new InfluxDBOASIS("https://localhost:18086", token, org, bucket);
        var literal = "${die(msg: \"never execute\")} 日本語☕";
        var echoed = await client.GetQueryApi().QueryAsync("import \"array\"\narray.from(rows: [{_value: "+InfluxDBCqlCompiler.FluxStringLiteral(literal)+"}])", org);
        Assert.AreEqual(literal, echoed.SelectMany(t => t.Records).Single().GetValue());
        string RawFlux() => "from(bucket: "+InfluxDBCqlCompiler.FluxStringLiteral(bucket)+")"+
            " |> range(start: 0, stop: time(v: 9223372036854775806))"+
            " |> filter(fn: (r) => r._measurement == \"oasis_holons\" and r._field == \"data_json\")";
        async Task<List<InfluxDB.Client.Core.Flux.Domain.FluxRecord>> RawRows()
            => (await client.GetQueryApi().QueryAsync(RawFlux(), org)).SelectMany(t => t.Records).ToList();
        async Task<long> Count() => (await RawRows()).Count;

        Seed seed;
        if (!File.Exists(manifest))
        {
            Assert.AreEqual(0L, await Count(), "Never adopt pre-existing business points.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray(), Guid.NewGuid());
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
            // One future head proves the query does not hide native clock skew.
            // Two tied heads with distinct tag sets must fail rather than pick a winner.
            var future = new Holon { Id = seed.Ids[0], Name = "Future Espresso", CreatedByAvatarId = seed.Actor };
            var tie = new Holon { Id = seed.TiedId, Name = "Ambiguous control", CreatedByAvatarId = seed.Actor };
            var stamp = DateTime.UtcNow;
            var controls = new[] { (future, Guid.Empty, stamp.AddDays(1)),
                (tie, Guid.NewGuid(), stamp), (tie, Guid.NewGuid(), stamp) };
            for (var i = 0; i < controls.Length; i++)
            {
                var (holon, parent, time) = controls[i];
                var json = JsonSerializer.Serialize(holon, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                await client.GetWriteApiAsync().WritePointAsync(PointData.Measurement("oasis_holons")
                    .Tag("id", holon.Id.ToString("D")).Tag("parent_holon_id", parent.ToString("D"))
                    .Tag("holon_type", ((int)holon.HolonType).ToString(CultureInfo.InvariantCulture))
                    .Field("is_deleted", false).Field("data_json", json).Timestamp(time, WritePrecision.Ns), bucket, org);
                using var receipt = new FileStream(Path.Combine(state, $"source-control-{i}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(receipt, new { id = holon.Id, parent, time, acknowledged = true }); receipt.Flush(true);
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
            var observed = await RawRows();
            Assert.AreEqual(6, observed.Count, "Only original six points (three normal, future head, two tied controls) may exist.");
            var rows = new List<string>();
            foreach (var record in observed)
            {
                var id = Guid.Parse((string)record.Values["id"]);
                Assert.IsTrue(seed.Ids.Contains(id) || id == seed.TiedId);
                var json = (string)record.GetValue();
                using var payload = JsonDocument.Parse(json);
                Assert.AreEqual(id, payload.RootElement.GetProperty("id").GetGuid());
                rows.Add(id.ToString("D")+"|"+record.Values["parent_holon_id"]+"|"+record.GetTime().ToString()+"|"+
                    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
            }
            rows.Sort(StringComparer.Ordinal);
            using var environment = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "environment.json")));
            var nativeBucket = await client.GetBucketsApi().FindBucketByNameAsync(bucket);
            Assert.AreEqual(environment.RootElement.GetProperty("identity").GetProperty("bucketId").GetString(), nativeBucket.Id);
            Assert.AreEqual(environment.RootElement.GetProperty("identity").GetProperty("orgId").GetString(), nativeBucket.OrgID);
            Assert.IsTrue(nativeBucket.RetentionRules.All(rule => rule.EverySeconds == 0));
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
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.InfluxDBOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.InfluxDBOASIS, ids).Result).Result
                .Where(h => ids.Contains(h.Id)).Result;
            if (paging) query = query.Skip(1).Result.Take(1).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.InfluxDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.InfluxDBOASIS, provider.QueryCapabilities) });
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
        var futureHead = await provider.ExecuteCosmicQueryAsync(Plan(new[] { seed.Ids[0] }));
        Assert.IsFalse(futureHead.IsError, futureHead.Message + futureHead.Exception);
        Assert.AreEqual("Future Espresso", futureHead.Result.Single().Name);
        var ambiguous = await provider.ExecuteCosmicQueryAsync(Plan(new[] { seed.TiedId }));
        Assert.AreEqual("CQL_QUERY_FAILED", ambiguous.ErrorCode);
        Assert.IsNull(ambiguous.Result, "Tied latest heads must never publish an arbitrary winner.");
        var limited = await provider.ExecuteCosmicQueryAsync(Plan(seed.Ids), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", limited.ErrorCode);
        Assert.IsNull(limited.Result);
        CollectionAssert.AreEqual(before, await Snapshot(), "Query must preserve JSON and native point timestamps.");
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
