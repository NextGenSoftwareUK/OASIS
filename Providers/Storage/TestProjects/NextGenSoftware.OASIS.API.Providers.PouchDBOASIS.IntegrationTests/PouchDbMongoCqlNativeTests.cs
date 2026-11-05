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
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class PouchDBMongoCqlNativeTests
{
    [TestMethod]
    [TestCategory("PouchDBMongoCqlNative")]
    public async Task NativePouchDBMongoFederationRunsSdkAndHttpWithProviderProvenance()
    {
        var state = Path.GetDirectoryName(Environment.GetEnvironmentVariable("COSMIC_POUCHDB_CQL_SOURCE"))!;
        using var seed = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "source.json")));
        Assert.IsTrue(File.Exists(Path.Combine(state, "bootstrap.ack.json")));
        var pouchdb = new PouchDBOASIS(seed.RootElement.GetProperty("Endpoint").GetString()!, null, null);
        await RunPairedAsync(seed.RootElement, pouchdb, state, "cosmic_cql_mongo_pouchdb_demo",
            Environment.GetEnvironmentVariable("OASIS_POUCHDB_MONGO_SERVE") == "1");
    }

    [TestMethod]
    public async Task NativeHostedPouchDBMongoFederationRunsSdkAndHttpWithProviderProvenance()
    {
        var descriptor = Environment.GetEnvironmentVariable("COSMIC_POUCHDB_HOSTED_CQL_SOURCE")
            ?? throw new InvalidOperationException("Explicit retained hosted PouchDB source descriptor required.");
        Assert.IsTrue(new FileInfo(descriptor).Length is > 0 and <= 16384);
        var phase = Directory.GetParent(Path.GetFullPath(descriptor))!;
        Assert.IsTrue(phase.Name.StartsWith("hosted-paired-", StringComparison.Ordinal));
        var state = phase.Parent!;
        Assert.AreEqual("hosted-preparation", state.Name);
        using var seed = JsonDocument.Parse(await File.ReadAllBytesAsync(descriptor));
        var endpoint = seed.RootElement.GetProperty("Endpoint").GetString()!;
        Assert.AreEqual("http://127.0.0.1:13800", endpoint);
        var digest = seed.RootElement.GetProperty("Digest").GetString()!;
        using var http = new HttpClient(new CqlPouchDbSourceHttpHandler(digest,
            new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, UseProxy = false }));
        var pouchdb = new PouchDBOASIS(endpoint, null, null, http);
        await RunPairedAsync(seed.RootElement, pouchdb, state.FullName, "cosmic_cql_mongo_pouchdb_hosted", false,
            Path.Combine(phase.FullName, "mongo-cleanup.ack.json"));
    }

    private static async Task RunPairedAsync(JsonElement seed, PouchDBOASIS pouchdb, string state,
        string database, bool serve, string? cleanupReceipt = null)
    {
        const string mongoUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(mongoUri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var pouchdbIds = seed.GetProperty("LiveIds").EnumerateArray().Select(v => v.GetGuid()).ToArray();
        var actor = seed.GetProperty("Actor").GetGuid();
        Assert.AreEqual(3, pouchdbIds.Distinct().Count());
        Assert.IsFalse(pouchdbIds.Contains(Guid.Empty));
        Assert.AreNotEqual(Guid.Empty, actor);
        using var sourceLease = new FileStream(Path.Combine(state, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var journal = Path.Combine(state, "mongo.pending.json");
        Assert.IsFalse(File.Exists(journal), "Preserve unfinished Mongo fixture.");
        var client = new MongoClient(mongoUri);
        using (var names = await client.ListDatabaseNamesAsync()) Assert.IsFalse((await names.ToListAsync()).Contains(database));
        var records = pouchdbIds.Take(2).Append(Guid.NewGuid()).Select((id,index) => new Holon {
            Id = id, Name = "COSMIC owned PouchDB Mongo copy " + index, CreatedByAvatarId = actor
        }).ToArray();
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { await JsonSerializer.SerializeAsync(file, new { database, actor, ids = records.Select(r => r.Id) }); file.Flush(true); }

        var mongo = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(mongoUri, database) { IsVersionControlEnabled = false };
        try
        {
            var activated = await pouchdb.ActivateQuerySourceAsync(default); Assert.IsFalse(activated.IsError, activated.Message + activated.Exception);
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
            registry.RegisterProvider(pouchdb); registry.RegisterProvider(mongo);
            var manager = new HolonManager(null, dna, registry);
            foreach (var selection in new[] { pouchdbIds, pouchdbIds.Take(2).ToArray() })
            {
                var result = await CosmicCqlDemo.IdentityFederationWalkthrough.RunAsync(manager, mongo, pouchdb,
                    selection, records.Select(r => r.Id), pouchdbIds, actor, TextWriter.Null);
                Assert.AreEqual(selection.Length + 2, result.Rows.Count);
                Assert.AreEqual(selection.Length + 2, result.ObservedInputRows);
                Assert.AreEqual(selection.Length, result.Rows.Count(r => r.Selected.Provider == ProviderType.PouchDBOASIS));
                Assert.AreEqual(2, result.Rows.Count(r => r.Selected.Provider == ProviderType.MongoDBOASIS));
                Assert.IsFalse(result.Rows.Any(r => r.Selected.Row.Id == records[2].Id));
            }
            await using (var app = CosmicCqlDemo.IdentityFederationApiHost.Build(manager, mongo, pouchdb, actor,
                pouchdbIds, records.Select(r => r.Id), pouchdbIds, "http://127.0.0.1:0"))
            {
                await app.StartAsync();
                try
                {
                    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var http = new HttpClient { BaseAddress = new Uri(address) };
                    var page = await http.GetStringAsync("/");
                    StringAssert.Contains(page, "<title>CQL - Mongo + PouchDB</title>");
                    StringAssert.Contains(page, "PouchDB"); StringAssert.Contains(page, "native PouchDB");
                    Assert.IsFalse(page.Contains("authenticated native PouchDB"), "Loopback fixture uses no PouchDB account.");
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
                        Assert.AreEqual(expected - 2, body.RootElement.GetProperty("rows").EnumerateArray().Count(r => r.GetProperty("provider").GetString() == "PouchDBOASIS"));
                    }
                    Assert.IsFalse((await pouchdb.DeActivateProviderAsync()).IsError);
                    using var failed = await http.PostAsJsonAsync("/api/cql/federation", new { preset = "all" });
                    Assert.AreEqual(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
                    Assert.IsFalse((await failed.Content.ReadAsStringAsync()).Contains("rows"));
                    Assert.IsFalse((await pouchdb.ActivateQuerySourceAsync(default)).IsError);
                }
                finally { await app.StopAsync(); }
            }
            if (serve)
            {
                await using var interactive = CosmicCqlDemo.IdentityFederationApiHost.Build(manager, mongo, pouchdb, actor,
                    pouchdbIds, records.Select(r => r.Id), pouchdbIds, "http://127.0.0.1:5141");
                await interactive.StartAsync();
                var ready = Path.Combine(state, "demo-ready.json");
                await File.WriteAllTextAsync(ready, JsonSerializer.Serialize(new { url = "http://127.0.0.1:5141/", providers = new[] { "MongoDBOASIS", "PouchDBOASIS" } }));
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
            if (cleanupReceipt != null)
            {
                using var receipt = new FileStream(cleanupReceipt, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await JsonSerializer.SerializeAsync(receipt, new { database, actor, ids = records.Select(r => r.Id), dropped = true });
                receipt.Flush(true);
            }
            File.Delete(journal);
        }
        finally { await mongo.DeActivateProviderAsync(); await pouchdb.DeActivateProviderAsync(); }
    }
}
