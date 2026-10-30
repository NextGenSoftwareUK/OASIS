using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class InfluxDBMongoCqlNativeTests
{
    [TestMethod]
    public async Task NativeInfluxDBMongoFederationRunsSdkAndHttpWithProviderProvenance()
    {

        const string mongoUri = "mongodb://127.0.0.1:27039/?replicaSet=cosmicLinuxDemoRs&serverSelectionTimeoutMS=3000";
        const string database = "cosmic_cql_mongo_influxdb_demo";
        Assert.AreEqual(mongoUri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        Assert.IsTrue(OperatingSystem.IsLinux(), "This preset certifies the admitted Linux provider source.");
        var state = Environment.GetEnvironmentVariable("OASIS_INFLUXDB_CQL_STATE")!;
        Assert.AreEqual("/var/tmp/oasis-influxdb-cql", state);
        Assert.IsTrue(File.Exists(Path.Combine(state, "environment.json")), "An admitted retained native InfluxDB environment is required.");
        Assert.IsTrue(File.Exists(Path.Combine(state, "source-snapshot.json")));
        using var seed = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "demo-seed.json")));
        var influxdbIds = seed.RootElement.GetProperty("Ids").EnumerateArray().Select(v => v.GetGuid()).Take(3).ToArray();
        var actor = seed.RootElement.GetProperty("Actor").GetGuid();
        using var sourceLease = new FileStream(Path.Combine(state, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        Assert.IsFalse(File.Exists(Path.Combine(state, "demo-seed.pending.json")));
        var journal = Path.Combine(state, "mongo.pending.json");
        Assert.IsFalse(File.Exists(journal), "Preserve unfinished Mongo fixture.");
        var client = new MongoClient(mongoUri);
        using (var names = await client.ListDatabaseNamesAsync()) Assert.IsFalse((await names.ToListAsync()).Contains(database));
        var records = influxdbIds.Take(2).Append(Guid.NewGuid()).Select((id,index) => new Holon {
            Id = id, Name = "COSMIC owned InfluxDB Mongo copy " + index, CreatedByAvatarId = actor
        }).ToArray();
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { await JsonSerializer.SerializeAsync(file, new { database, actor, ids = records.Select(r => r.Id) }); file.Flush(true); }

        using var credentials = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "credentials.json")));
        Assert.AreEqual(Path.Combine(state, "ca.pem"), Environment.GetEnvironmentVariable("SSL_CERT_FILE"));
        var influxdb = new InfluxDBOASIS("https://localhost:18086",
            credentials.RootElement.GetProperty("token").GetString()!,
            credentials.RootElement.GetProperty("org").GetString()!,
            credentials.RootElement.GetProperty("bucket").GetString()!);
        var mongo = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(mongoUri, database) { IsVersionControlEnabled = false };
        try
        {
            var activated = await influxdb.ActivateProviderAsync(); Assert.IsFalse(activated.IsError, activated.Message + activated.Exception);
            Assert.IsFalse((await mongo.ActivateProviderAsync()).IsError);
            foreach (var record in records)
            {
                var saved = await mongo.SaveHolonAsync(record, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
            }
            var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode = NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                StorageProviders = new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings { LogSwitchingProviders = false }
            } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var registry = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
            registry.RegisterProvider(influxdb); registry.RegisterProvider(mongo);
            var manager = new HolonManager(null, dna, registry);
            foreach (var selection in new[] { influxdbIds, influxdbIds.Take(2).ToArray() })
            {
                var result = await CosmicCqlDemo.IdentityFederationWalkthrough.RunAsync(manager, mongo, influxdb,
                    selection, records.Select(r => r.Id), influxdbIds, actor, TextWriter.Null);
                Assert.AreEqual(selection.Length + 2, result.Rows.Count);
                Assert.AreEqual(selection.Length + 2, result.ObservedInputRows);
                Assert.AreEqual(selection.Length, result.Rows.Count(r => r.Selected.Provider == ProviderType.InfluxDBOASIS));
                Assert.AreEqual(2, result.Rows.Count(r => r.Selected.Provider == ProviderType.MongoDBOASIS));
                Assert.IsFalse(result.Rows.Any(r => r.Selected.Row.Id == records[2].Id));
            }
            await using (var app = CosmicCqlDemo.IdentityFederationApiHost.Build(manager, mongo, influxdb, actor,
                influxdbIds, records.Select(r => r.Id), influxdbIds, "http://127.0.0.1:0"))
            {
                await app.StartAsync();
                try
                {
                    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var http = new HttpClient { BaseAddress = new Uri(address) };
                    var page = await http.GetStringAsync("/");
                    StringAssert.Contains(page, "<title>CQL - Mongo + InfluxDB</title>");
                    StringAssert.Contains(page, "InfluxDB"); StringAssert.Contains(page, "retained authenticated native InfluxDB");
                    StringAssert.Contains(page, "Query two-record subset");
                    using var forged = await http.PostAsJsonAsync("/api/cql/federation", new { preset = "all", actorId = Guid.NewGuid() });
                    Assert.AreEqual(HttpStatusCode.BadRequest, forged.StatusCode);
                    using var unknown = await http.PostAsJsonAsync("/api/cql/federation", new { preset = "foreign" });
                    Assert.AreEqual(HttpStatusCode.BadRequest, unknown.StatusCode);
                    foreach (var preset in new[] { "all", "subset" })
                    {
                        using var response = await http.PostAsJsonAsync("/api/cql/federation", new { preset });
                        Assert.IsTrue(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
                        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                        var expected = preset == "all" ? 5 : 4;
                        Assert.AreEqual(expected, body.RootElement.GetProperty("count").GetInt32());
                        Assert.AreEqual(expected, body.RootElement.GetProperty("observedPhysicalRows").GetInt32());
                        Assert.AreEqual(expected - 2, body.RootElement.GetProperty("rows").EnumerateArray().Count(r => r.GetProperty("provider").GetString() == "InfluxDBOASIS"));
                    }
                    Assert.IsFalse((await influxdb.DeActivateProviderAsync()).IsError);
                    using var failed = await http.PostAsJsonAsync("/api/cql/federation", new { preset = "all" });
                    Assert.AreEqual(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
                    Assert.IsFalse((await failed.Content.ReadAsStringAsync()).Contains("rows"));
                    Assert.IsFalse((await influxdb.ActivateProviderAsync()).IsError);
                }
                finally { await app.StopAsync(); }
            }
            if (Environment.GetEnvironmentVariable("COSMIC_INFLUXDB_MONGO_SERVE") == "1")
            {
                await using var interactive = CosmicCqlDemo.IdentityFederationApiHost.Build(manager, mongo, influxdb, actor,
                    influxdbIds, records.Select(r => r.Id), influxdbIds, "http://127.0.0.1:5144");
                await interactive.StartAsync();
                var ready = Path.Combine(state, "demo-ready.json");
                await File.WriteAllTextAsync(ready, JsonSerializer.Serialize(new { url = "http://127.0.0.1:5144/", providers = new[] { "MongoDBOASIS", "InfluxDBOASIS" } }));
                await interactive.WaitForShutdownAsync(); File.Delete(ready);
            }
            // This exact database was absent before the journal. Verify all owned
            // records and reject unexplained data before dropping the fixture.
            var native = client.GetDatabase(database);
            Assert.AreEqual(3L, await native.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            foreach (var record in records)
            {
                var observed = await mongo.LoadHolonAsync(record.Id, loadChildren: false);
                Assert.IsFalse(observed.IsError); Assert.AreEqual(record.Name, observed.Result.Name);
                Assert.AreEqual(actor, observed.Result.CreatedByAvatarId);
            }
            using (var names = await native.ListCollectionNamesAsync())
            foreach (var name in await names.ToListAsync())
            {
                if (name == "Holon") continue;
                var rows = await native.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).Limit(5).ToListAsync();
                Assert.IsTrue(rows.Count == 0 || name == "CosmicMigrationRecordGate" && rows.Count == 1
                    && rows[0]["_id"].AsString == "cosmic-migration-record-gate-schema-v1", "Unexplained native fixture data: " + name);
            }
            Assert.IsFalse((await mongo.DeActivateProviderAsync()).IsError);
            await client.DropDatabaseAsync(database);
            using (var names = await client.ListDatabaseNamesAsync()) Assert.IsFalse((await names.ToListAsync()).Contains(database));
                File.Delete(journal);
        }
        finally { await mongo.DeActivateProviderAsync(); await influxdb.DeActivateProviderAsync(); }
    }
}
