using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class TypesenseMongoCqlNativeTests
{
    [TestMethod, TestCategory("TypesenseMongoCqlNative")]
    public async Task NativeTypesenseMongoFederationRunsManagerAndHttpWithoutSourceWrites()
    {
        const string mongoUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string database = "cosmic_cql_mongo_typesense_demo";
        Assert.AreEqual(mongoUri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var path = Environment.GetEnvironmentVariable("COSMIC_TYPESENSE_CQL_SOURCE");
        var key = Environment.GetEnvironmentVariable("COSMIC_TYPESENSE_CQL_KEY");
        Assert.IsFalse(string.IsNullOrWhiteSpace(path));
        Assert.IsFalse(string.IsNullOrWhiteSpace(key));
        var source = JsonSerializer.Deserialize<TypesenseCqlNativeTests.Source>(await File.ReadAllTextAsync(path!))!;
        Assert.AreEqual("http://127.0.0.1:58108", source.Endpoint);
        Assert.AreEqual(3, source.LiveIds.Distinct().Count());
        Assert.AreNotEqual(Guid.Empty, source.Actor);
        var state = Path.GetDirectoryName(path!)!;
        using var lease = new FileStream(Path.Combine(state, "federation.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var journal = Path.Combine(state, "mongo.pending.json");
        Assert.IsFalse(File.Exists(journal), "Preserve an unfinished fixture instead of reseeding.");
        var client = new MongoClient(mongoUri);
        using (var names = await client.ListDatabaseNamesAsync())
            Assert.IsFalse((await names.ToListAsync()).Contains(database), "Never adopt an existing database.");
        var copies = source.LiveIds.Take(2).Append(Guid.NewGuid()).Select((id, i) => new Holon {
            Id = id, Name = "Owned Typesense Mongo snapshot " + i, CreatedByAvatarId = source.Actor, IsPublic = false
        }).ToArray();
        using (var intent = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(intent, new { database, source.Actor, ids = copies.Select(row => row.Id) });
            intent.Flush(true);
        }
        using var nativeHttp = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
        var typesense = new TypesenseOASIS(source.Endpoint, key!, nativeHttp);
        var mongo = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(mongoUri, database) { IsVersionControlEnabled = false };
        async Task<string[]> Observe()
        {
            var hashes = new List<string>();
            foreach (var id in source.LiveIds.Append(source.DeletedId))
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var response = await nativeHttp.GetAsync(source.Endpoint + "/collections/oasis_holons/documents/" + id,
                    HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                response.EnsureSuccessStatusCode();
                using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
                using var body = new MemoryStream();
                var buffer = new byte[8192];
                int count;
                while ((count = await stream.ReadAsync(buffer, deadline.Token)) > 0)
                {
                    Assert.IsTrue(body.Length + count <= 1024 * 1024);
                    body.Write(buffer, 0, count);
                }
                hashes.Add(Convert.ToHexString(SHA256.HashData(body.ToArray())));
            }
            return hashes.ToArray();
        }
        try
        {
            Assert.IsFalse((await typesense.ActivateQuerySourceAsync(default)).IsError);
            var before = await Observe();
            Assert.IsFalse((await mongo.ActivateProviderAsync()).IsError);
            foreach (var copy in copies)
            {
                var saved = await mongo.SaveHolonAsync(copy, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message);
            }
            var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode = NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                StorageProviders = new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings { LogSwitchingProviders = false }
            } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var registry = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
            registry.RegisterProvider(typesense); registry.RegisterProvider(mongo);
            var manager = new HolonManager(null, dna, registry);
            foreach (var selection in new[] { source.LiveIds, source.LiveIds.Take(2).ToArray() })
            {
                var result = await CosmicCqlDemo.IdentityFederationWalkthrough.RunAsync(manager, mongo, typesense,
                    selection, copies.Select(row => row.Id), source.LiveIds, source.Actor, TextWriter.Null);
                Assert.AreEqual(selection.Length + 2, result.Rows.Count);
                Assert.AreEqual(selection.Length + 2, result.ObservedInputRows);
                Assert.AreEqual(selection.Length, result.Rows.Count(row => row.Selected.Provider == ProviderType.TypesenseOASIS));
                Assert.AreEqual(2, result.Rows.Count(row => row.Selected.Provider == ProviderType.MongoDBOASIS));
                Assert.IsFalse(result.Rows.Any(row => row.Selected.Row.Id == copies[2].Id));
            }
            await using (var app = CosmicCqlDemo.IdentityFederationApiHost.Build(manager, mongo, typesense, source.Actor,
                source.LiveIds, copies.Select(row => row.Id), source.LiveIds, "http://127.0.0.1:0"))
            {
                await app.StartAsync();
                try
                {
                    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var http = new HttpClient { BaseAddress = new Uri(address) };
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
                        Assert.AreEqual(expected - 2, body.RootElement.GetProperty("rows").EnumerateArray().Count(row => row.GetProperty("provider").GetString() == "TypesenseOASIS"));
                    }
                    Assert.IsFalse((await typesense.DeActivateProviderAsync()).IsError);
                    using var failed = await http.PostAsJsonAsync("/api/cql/federation", new { preset = "all" });
                    Assert.AreEqual(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
                    Assert.IsFalse((await failed.Content.ReadAsStringAsync()).Contains("rows"));
                }
                finally { await app.StopAsync(); }
            }
            CollectionAssert.AreEqual(before, await Observe(), "Federation must not mutate native Typesense documents.");
            var db = client.GetDatabase(database);
            Assert.AreEqual(3L, await db.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            foreach (var copy in copies)
            {
                var observed = await mongo.LoadHolonAsync(copy.Id, loadChildren: false);
                Assert.IsFalse(observed.IsError);
                Assert.AreEqual(copy.Name, observed.Result.Name);
                Assert.AreEqual(source.Actor, observed.Result.CreatedByAvatarId);
                Assert.IsFalse(observed.Result.IsPublic);
            }
            using (var names = await db.ListCollectionNamesAsync())
            foreach (var name in await names.ToListAsync())
            {
                if (name == "Holon") continue;
                var rows = await db.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).Limit(5).ToListAsync();
                Assert.IsTrue(rows.Count == 0 || name == "CosmicMigrationRecordGate" && rows.Count == 1
                    && rows[0]["_id"].AsString == "cosmic-migration-record-gate-schema-v1", "Unexpected fixture data: " + name);
            }
            Assert.IsFalse((await mongo.DeActivateProviderAsync()).IsError);
            await client.DropDatabaseAsync(database);
            using (var names = await client.ListDatabaseNamesAsync()) Assert.IsFalse((await names.ToListAsync()).Contains(database));
            File.Delete(journal);
            await File.WriteAllTextAsync(Path.Combine(state, "mongo-complete.json"), JsonSerializer.Serialize(new {
                database, managerRows = new[] { 5, 4 }, httpRows = new[] { 5, 4 }, sourceUnchanged = true, privateCopies = 3,
                unrelatedCopyExcluded = true, ownedDatabaseAbsent = true
            }));
        }
        finally { await mongo.DeActivateProviderAsync(); await typesense.DeActivateProviderAsync(); }
    }
}
