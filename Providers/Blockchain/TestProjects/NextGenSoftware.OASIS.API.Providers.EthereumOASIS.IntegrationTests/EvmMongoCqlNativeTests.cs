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
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class EvmMongoCqlNativeTests
{
    [TestMethod]
    public async Task NativeEvmMongoFederationRunsSdkAndHttpWithProviderProvenance()
    {
        
        const string mongoUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        var name = Environment.GetEnvironmentVariable("OASIS_EVM_CQL_PROVIDER");
        Assert.IsTrue(name is "Ethereum" or "Polygon" or "Arbitrum" or "Base" or "Avalanche" or "Optimism" or "Rootstock", "The runner must explicitly select an enrolled provider.");
        var authority = name switch { "Polygon" => ProviderType.PolygonOASIS, "Arbitrum" => ProviderType.ArbitrumOASIS, "Base" => ProviderType.BaseOASIS, "Avalanche" => ProviderType.AvalancheOASIS, "Optimism" => ProviderType.OptimismOASIS, "Rootstock" => ProviderType.RootstockOASIS, _ => ProviderType.EthereumOASIS };
        var port = name switch { "Polygon" => 5098, "Arbitrum" => 5099, "Base" => 5104, "Avalanche" => 5105, "Optimism" => 5125, "Rootstock" => 5126, _ => 5097 };
        var database = "cosmic_cql_mongo_" + name.ToLowerInvariant() + "_demo";
        Assert.AreEqual(mongoUri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var state = Environment.GetEnvironmentVariable("OASIS_EVM_CQL_STATE")!;
        using var seed = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "demo-seed.json")));
        var sourceIds = seed.RootElement.GetProperty("Ids").EnumerateArray().Select(v => v.GetGuid()).ToArray();
        var actor = seed.RootElement.GetProperty("Actor").GetGuid();
        var journal = Path.Combine(state, "mongo.pending.json");
        Assert.IsFalse(File.Exists(journal), "Preserve unfinished Mongo fixture.");
        var client = new MongoClient(mongoUri);
        using (var names = await client.ListDatabaseNamesAsync()) Assert.IsFalse((await names.ToListAsync()).Contains(database));
        var records = sourceIds.Take(2).Append(Guid.NewGuid()).Select((id,index) => new Holon {
            Id = id, Name = "COSMIC owned " + name + " Mongo copy " + index, CreatedByAvatarId = actor
        }).ToArray();
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { await JsonSerializer.SerializeAsync(file, new { database, actor, ids = records.Select(r => r.Id) }); file.Flush(true); }

        // The concrete provider owns storage/decryption. Both providers execute
        // the same CQL federation walkthrough; no provider-specific HTTP engine.
        const string devKey = "0x4f3edf983ac636a65a842ce7c78d9aa706d3b113bce9c46f30d7d21715b23b1d";
        var contract = seed.RootElement.GetProperty("Contract").GetString()!;
        OASISStorageProviderBase secondary = name switch
        {
            "Polygon" => new NextGenSoftware.OASIS.API.Providers.PolygonOASIS.PolygonOASIS("http://127.0.0.1:18547", devKey, contract, 31338),
            "Arbitrum" => EvmCqlDemoProviders.CreateArbitrum("http://127.0.0.1:18547", devKey, contract),
            "Base" => EvmCqlDemoProviders.CreateBase("http://127.0.0.1:18547", devKey, contract),
            "Avalanche" => EvmCqlDemoProviders.CreateAvalanche("http://127.0.0.1:18547", devKey, contract),
            "Optimism" => new NextGenSoftware.OASIS.API.Providers.OptimismOASIS.OptimismOASIS("http://127.0.0.1:18547", devKey, contract, 31338),
            "Rootstock" => EvmCqlDemoProviders.CreateRootstock("http://127.0.0.1:18547", devKey, contract),
            _ => new EthereumOASIS("http://127.0.0.1:18547", devKey, 31338, contract)
        };
        var queryAdapter = (ICosmicQueryStorageProvider)secondary;
        var mongo = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(mongoUri, database) { IsVersionControlEnabled = false };
        try
        {
            var activated = await secondary.ActivateProviderAsync(); Assert.IsFalse(activated.IsError, activated.Message + activated.Exception);
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
            registry.RegisterProvider(secondary); registry.RegisterProvider(mongo);
            var manager = new HolonManager(null, dna, registry);
            foreach (var selection in new[] { sourceIds, sourceIds.Take(2).ToArray() })
            {
                var result = await CosmicCqlDemo.IdentityFederationWalkthrough.RunAsync(manager, mongo, queryAdapter,
                    selection, records.Select(r => r.Id), sourceIds, actor, TextWriter.Null);
                Assert.AreEqual(selection.Length + 2, result.Rows.Count);
                Assert.AreEqual(selection.Length + 2, result.ObservedInputRows);
                Assert.AreEqual(selection.Length, result.Rows.Count(r => r.Selected.Provider == authority));
                Assert.AreEqual(2, result.Rows.Count(r => r.Selected.Provider == ProviderType.MongoDBOASIS));
                Assert.IsFalse(result.Rows.Any(r => r.Selected.Row.Id == records[2].Id));
            }
            await using (var app = CosmicCqlDemo.IdentityFederationApiHost.Build(manager, mongo, queryAdapter, actor,
                sourceIds, records.Select(r => r.Id), sourceIds, "http://127.0.0.1:0"))
            {
                await app.StartAsync();
                try
                {
                    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var http = new HttpClient { BaseAddress = new Uri(address) };
                    var page = await http.GetStringAsync("/");
                    StringAssert.Contains(page, "<title>CQL - Mongo + " + name + "</title>");
                    StringAssert.Contains(page, name); StringAssert.Contains(page, "local EVM chain31338");
                    StringAssert.Contains(page, "Query two-record subset");
                    StringAssert.Contains(page, "Reading Mongo and " + name + "...");
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
                        Assert.AreEqual(expected - 2, body.RootElement.GetProperty("rows").EnumerateArray().Count(r => r.GetProperty("provider").GetString() == authority.ToString()));
                    }
                    Assert.IsFalse((await secondary.DeActivateProviderAsync()).IsError);
                    using var failed = await http.PostAsJsonAsync("/api/cql/federation", new { preset = "all" });
                    Assert.AreEqual(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
                    Assert.IsFalse((await failed.Content.ReadAsStringAsync()).Contains("rows"));
                    Assert.IsFalse((await secondary.ActivateProviderAsync()).IsError);
                }
                finally { await app.StopAsync(); }
            }
            if (Environment.GetEnvironmentVariable("OASIS_EVM_MONGO_SERVE") == "1")
            {
                await using var interactive = CosmicCqlDemo.IdentityFederationApiHost.Build(manager, mongo, queryAdapter, actor,
                    sourceIds, records.Select(r => r.Id), sourceIds, "http://127.0.0.1:" + port);
                await interactive.StartAsync();
                var ready = Path.Combine(state, "demo-ready.json");
                await File.WriteAllTextAsync(ready, JsonSerializer.Serialize(new { url = "http://127.0.0.1:" + port + "/", providers = new[] { "MongoDBOASIS", authority.ToString() } }));
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
            foreach (var collectionName in await names.ToListAsync())
            {
                if (collectionName == "Holon") continue;
                var rows = await native.GetCollection<BsonDocument>(collectionName).Find(FilterDefinition<BsonDocument>.Empty).Limit(5).ToListAsync();
                Assert.IsTrue(rows.Count == 0 || collectionName == "CosmicMigrationRecordGate" && rows.Count == 1
                    && rows[0]["_id"].AsString == "cosmic-migration-record-gate-schema-v1", "Unexplained native fixture data: " + collectionName);
            }
            Assert.IsFalse((await mongo.DeActivateProviderAsync()).IsError);
            await client.DropDatabaseAsync(database);
            using (var names = await client.ListDatabaseNamesAsync()) Assert.IsFalse((await names.ToListAsync()).Contains(database));
            File.Delete(journal);
        }
        finally { await mongo.DeActivateProviderAsync(); await secondary.DeActivateProviderAsync(); }
    }
}
