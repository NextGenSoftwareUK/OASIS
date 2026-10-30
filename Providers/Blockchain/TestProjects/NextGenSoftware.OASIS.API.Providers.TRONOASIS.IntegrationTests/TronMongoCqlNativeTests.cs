using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.IntegrationTests;

public class TronMongoCqlNativeTests
{
    private sealed class ReadOnlySource(TronNativeTransferClient client, TronPrivateStorageBackend storage)
        : TRONOASIS(rpcEndpoint: "http://127.0.0.1:9090", nativeTransfers: client, storageBackend: storage)
    {
        public string CanonicalKey(Guid id) => RecordKey("holon", id);
    }

    [Fact]
    public async Task NativeTronMongoFederationRunsSdkAndHttpWithProviderProvenance()
    {
        
        const string mongoUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string database = "cosmic_cql_mongo_tron_demo";
        Assert.Equal(mongoUri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var state = Environment.GetEnvironmentVariable("OASIS_TRON_CQL_STATE")!;
        using var sourceLease = new FileStream(Path.Combine(state,"source.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        Assert.False(File.Exists(Path.Combine(state,"source.pending.json")));
        using var seed = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "demo-seed.json")));
        var tronIds = seed.RootElement.GetProperty("Ids").EnumerateArray().Select(v => v.GetGuid()).ToArray();
        var actor = seed.RootElement.GetProperty("Actor").GetGuid();
        var journal = Path.Combine(state, "mongo.pending.json");
        Assert.False(File.Exists(journal), "Preserve unfinished Mongo fixture.");
        var client = new MongoClient(mongoUri);
        using (var names = await client.ListDatabaseNamesAsync()) Assert.False((await names.ToListAsync()).Contains(database));
        var records = tronIds.Take(2).Append(Guid.NewGuid()).Select((id,index) => new Holon {
            Id = id, Name = "COSMIC owned Tron Mongo copy " + index, CreatedByAvatarId = actor
        }).ToArray();
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { await JsonSerializer.SerializeAsync(file, new { database, actor, ids = records.Select(r => r.Id) }); file.Flush(true); }

        var key = Convert.FromBase64String(Environment.GetEnvironmentVariable("OASIS_TRON_CQL_STORAGE_KEY")!);
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), "0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f");
        var owner = new TronNet.TronECKey(new string('0', 63) + "1", TronNet.TronNetwork.MainNet).GetPublicAddress();
        using var storage = new TronPrivateStorageBackend(runtime.NativeTransfers, owner,
            seed.RootElement.GetProperty("Contract").GetString()!,
            () => throw new InvalidOperationException("Federation must never request TRON signing material."), () => (byte[])key.Clone());
        var tron = new ReadOnlySource(runtime.NativeTransfers, storage);
        var preparations = Directory.GetFiles(state, "*.prepared.json").ToDictionary(path => path,
            path => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))));
        var canonical = new Dictionary<Guid,string>();
        foreach (var id in tronIds)
        {
            var value = await storage.GetAsync(tron.CanonicalKey(id));
            Assert.NotNull(value);
            canonical.Add(id,value);
        }
        var mongo = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(mongoUri, database) { IsVersionControlEnabled = false };
        try
        {
            var activated = await tron.ActivateProviderAsync(); Assert.False(activated.IsError, activated.Message + activated.Exception);
            Assert.False((await mongo.ActivateProviderAsync()).IsError);
            foreach (var record in records)
            {
                var saved = await mongo.SaveHolonAsync(record, saveChildren: false);
                Assert.False(saved.IsError, saved.Message + saved.Exception);
            }
            var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode = NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                StorageProviders = new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings { LogSwitchingProviders = false }
            } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var registry = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
            registry.RegisterProvider(tron); registry.RegisterProvider(mongo);
            var manager = new HolonManager(null, dna, registry);
            foreach (var selection in new[] { tronIds, tronIds.Take(2).ToArray() })
            {
                var result = await CosmicCqlDemo.IdentityFederationWalkthrough.RunAsync(manager, mongo, tron,
                    selection, records.Select(r => r.Id), tronIds, actor, TextWriter.Null);
                Assert.Equal(selection.Length + 2, result.Rows.Count);
                Assert.Equal(selection.Length + 2, result.ObservedInputRows);
                Assert.Equal(selection.Length, result.Rows.Select(row => row.Selected.Row.Id).Distinct().Count());
                Assert.Equal(selection.Length, result.Rows.Count(r => r.Selected.Provider == ProviderType.TRONOASIS));
                Assert.Equal(2, result.Rows.Count(r => r.Selected.Provider == ProviderType.MongoDBOASIS));
                Assert.False(result.Rows.Any(r => r.Selected.Row.Id == records[2].Id));
            }
            await using (var app = CosmicCqlDemo.IdentityFederationApiHost.Build(manager, mongo, tron, actor,
                tronIds, records.Select(r => r.Id), tronIds, "http://127.0.0.1:0"))
            {
                await app.StartAsync();
                try
                {
                    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var http = new HttpClient { BaseAddress = new Uri(address) };
                    var page = await http.GetStringAsync("/");
                    Assert.Contains("<title>CQL - Mongo + TRON</title>", page);
                    Assert.Contains("TRON", page); Assert.Contains("retained local TRON chain", page);
                    Assert.Contains("Query two-record subset", page);
                    using var forged = await http.PostAsJsonAsync("/api/cql/federation", new { preset = "all", actorId = Guid.NewGuid() });
                    Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
                    using var unknown = await http.PostAsJsonAsync("/api/cql/federation", new { preset = "foreign" });
                    Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
                    foreach (var preset in new[] { "all", "subset" })
                    {
                        using var response = await http.PostAsJsonAsync("/api/cql/federation", new { preset });
                        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
                        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                        var expected = preset == "all" ? 5 : 4;
                        Assert.Equal(expected, body.RootElement.GetProperty("count").GetInt32());
                        Assert.Equal(expected, body.RootElement.GetProperty("observedPhysicalRows").GetInt32());
                        Assert.Equal(expected - 2, body.RootElement.GetProperty("rows").EnumerateArray().Count(r => r.GetProperty("provider").GetString() == "TRONOASIS"));
                    }
                    Assert.False((await tron.DeActivateProviderAsync()).IsError);
                    using var failed = await http.PostAsJsonAsync("/api/cql/federation", new { preset = "all" });
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
                    Assert.False((await failed.Content.ReadAsStringAsync()).Contains("rows"));
                    Assert.False((await tron.ActivateProviderAsync()).IsError);
                }
                finally { await app.StopAsync(); }
            }
            if (Environment.GetEnvironmentVariable("OASIS_TRON_MONGO_SERVE") == "1")
            {
                await using var interactive = CosmicCqlDemo.IdentityFederationApiHost.Build(manager, mongo, tron, actor,
                    tronIds, records.Select(r => r.Id), tronIds, "http://127.0.0.1:5108");
                await interactive.StartAsync();
                var ready = Path.Combine(state, "demo-ready.json");
                await File.WriteAllTextAsync(ready, JsonSerializer.Serialize(new { url = "http://127.0.0.1:5108/", providers = new[] { "MongoDBOASIS", "TRONOASIS" } }));
                await interactive.WaitForShutdownAsync(); File.Delete(ready);
            }
            // This exact database was absent before the journal. Verify all owned
            // records and reject unexplained data before dropping the fixture.
            var native = client.GetDatabase(database);
            Assert.Equal(3L, await native.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            foreach (var record in records)
            {
                var observed = await mongo.LoadHolonAsync(record.Id, loadChildren: false);
                Assert.False(observed.IsError); Assert.Equal(record.Name, observed.Result.Name);
                Assert.Equal(actor, observed.Result.CreatedByAvatarId);
            }
            using (var names = await native.ListCollectionNamesAsync())
            foreach (var name in await names.ToListAsync())
            {
                if (name == "Holon") continue;
                var rows = await native.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).Limit(5).ToListAsync();
                Assert.True(rows.Count == 0 || name == "CosmicMigrationRecordGate" && rows.Count == 1
                    && rows[0]["_id"].AsString == "cosmic-migration-record-gate-schema-v1", "Unexplained native fixture data: " + name);
            }
            Assert.False((await mongo.DeActivateProviderAsync()).IsError);
            foreach (var entry in canonical)
                Assert.Equal(entry.Value, await storage.GetAsync(tron.CanonicalKey(entry.Key)));
            Assert.Equal(preparations.Count, Directory.GetFiles(state, "*.prepared.json").Length);
            foreach (var entry in preparations)
                Assert.Equal(entry.Value, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(entry.Key))));
            await client.DropDatabaseAsync(database);
            using (var names = await client.ListDatabaseNamesAsync()) Assert.False((await names.ToListAsync()).Contains(database));
            File.Delete(journal);
        }
        finally { await mongo.DeActivateProviderAsync(); await tron.DeActivateProviderAsync(); System.Security.Cryptography.CryptographicOperations.ZeroMemory(key); }
    }
}
