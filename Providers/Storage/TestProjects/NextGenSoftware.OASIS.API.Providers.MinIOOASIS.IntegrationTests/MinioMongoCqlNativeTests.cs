using System.Net;
using Amazon.S3;
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

namespace NextGenSoftware.OASIS.API.Providers.MinIOOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class MinIOMongoCqlNativeTests
{
    [TestMethod]
    [TestCategory("MinIOMongoCqlNative")]
    public async Task NativeMinIOMongoFederationRunsSdkAndHttpWithProviderProvenance()
    {

        const string mongoUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string database = "cosmic_cql_mongo_minio_demo";
        Assert.AreEqual(mongoUri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var state = Path.GetDirectoryName(Environment.GetEnvironmentVariable("COSMIC_MINIO_CQL_SOURCE"))!;
        using var seed = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "source.json")));
        var minioIds = seed.RootElement.GetProperty("LiveIds").EnumerateArray().Select(v => v.GetGuid()).ToArray();
        var actor = seed.RootElement.GetProperty("Actor").GetGuid();
        using var sourceLease = new FileStream(Path.Combine(state, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        Assert.IsTrue(File.Exists(Path.Combine(state, "bootstrap.ack.json")));
        var journal = Path.Combine(state, "mongo.pending.json");
        Assert.IsFalse(File.Exists(journal), "Preserve unfinished Mongo fixture.");
        var sourcePath = Path.Combine(state, "source.json");
        var source = JsonSerializer.Deserialize<MinioCqlSource>(seed.RootElement.GetRawText())!;
        Assert.AreEqual("http://127.0.0.1:19700", source.Endpoint); Assert.AreEqual("cosmic-cql-minio-recovery", source.Bucket);
        using var credentials = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "credentials.json")));
        var username = credentials.RootElement.GetProperty("username").GetString()!;
        var password = credentials.RootElement.GetProperty("password").GetString()!;
        using var observer = new AmazonS3Client(username, password, new AmazonS3Config { ServiceURL = source.Endpoint,
            ForcePathStyle = true, UseHttp = true, AuthenticationRegion = "us-east-1", MaxErrorRetry = 0 });
        var before = await MinioCqlNativeTests.ObserveOriginalAsync(sourcePath, source, observer);
        var minio = new MinIOOASIS(source.Endpoint, username, password, source.Bucket, false, minioIds[0]);
        var client = new MongoClient(mongoUri);
        using (var names = await client.ListDatabaseNamesAsync()) Assert.IsFalse((await names.ToListAsync()).Contains(database));
        var records = minioIds.Take(2).Append(Guid.NewGuid()).Select((id,index) => new Holon {
            Id = id, Name = "COSMIC owned MinIO Mongo copy " + index, CreatedByAvatarId = actor
        }).ToArray();
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        { await JsonSerializer.SerializeAsync(file, new { database, actor, ids = records.Select(r => r.Id) }); file.Flush(true); }

        var mongo = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(mongoUri, database) { IsVersionControlEnabled = false };
        try
        {
            var activated = await minio.ActivateQuerySourceAsync(default); Assert.IsFalse(activated.IsError, activated.Message + activated.Exception);
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
            registry.RegisterProvider(minio); registry.RegisterProvider(mongo);
            var manager = new HolonManager(null, dna, registry);
            foreach (var selection in new[] { minioIds, minioIds.Take(2).ToArray() })
            {
                var result = await CosmicCqlDemo.IdentityFederationWalkthrough.RunAsync(manager, mongo, minio,
                    selection, records.Select(r => r.Id), minioIds, actor, TextWriter.Null);
                Assert.AreEqual(selection.Length + 2, result.Rows.Count);
                Assert.AreEqual(selection.Length + 2, result.ObservedInputRows);
                Assert.AreEqual(selection.Length, result.Rows.Count(r => r.Selected.Provider == ProviderType.MinIOOASIS));
                Assert.AreEqual(2, result.Rows.Count(r => r.Selected.Provider == ProviderType.MongoDBOASIS));
                Assert.IsFalse(result.Rows.Any(r => r.Selected.Row.Id == records[2].Id));
            }
            await using (var app = CosmicCqlDemo.IdentityFederationApiHost.Build(manager, mongo, minio, actor,
                minioIds, records.Select(r => r.Id), minioIds, "http://127.0.0.1:0"))
            {
                await app.StartAsync();
                try
                {
                    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var http = new HttpClient { BaseAddress = new Uri(address) };
                    var page = await http.GetStringAsync("/");
                    StringAssert.Contains(page, "<title>CQL - Mongo + MinIO</title>");
                    StringAssert.Contains(page, "MinIO"); StringAssert.Contains(page, "native MinIO");
                    StringAssert.Contains(page, "authenticated native MinIO");
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
                        Assert.AreEqual(expected - 2, body.RootElement.GetProperty("rows").EnumerateArray().Count(r => r.GetProperty("provider").GetString() == "MinIOOASIS"));
                    }
                    Assert.IsFalse((await minio.DeActivateProviderAsync()).IsError);
                    using var failed = await http.PostAsJsonAsync("/api/cql/federation", new { preset = "all" });
                    Assert.AreEqual(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
                    Assert.IsFalse((await failed.Content.ReadAsStringAsync()).Contains("rows"));
                    Assert.IsFalse((await minio.ActivateQuerySourceAsync(default)).IsError);
                }
                finally { await app.StopAsync(); }
            }
            if (Environment.GetEnvironmentVariable("OASIS_MINIO_MONGO_SERVE") == "1")
            {
                await using var interactive = CosmicCqlDemo.IdentityFederationApiHost.Build(manager, mongo, minio, actor,
                    minioIds, records.Select(r => r.Id), minioIds, "http://127.0.0.1:5142");
                await interactive.StartAsync();
                var ready = Path.Combine(state, "demo-ready.json");
                await File.WriteAllTextAsync(ready, JsonSerializer.Serialize(new { url = "http://127.0.0.1:5142/", providers = new[] { "MongoDBOASIS", "MinIOOASIS" } }));
                await interactive.WaitForShutdownAsync(); File.Delete(ready);
            }
            CollectionAssert.AreEqual(before, await MinioCqlNativeTests.ObserveOriginalAsync(sourcePath, source, observer));
            await VerifyAndRemoveFixtureAsync(client, mongo, database, records, actor, journal);
        }
        finally { await mongo.DeActivateProviderAsync(); await minio.DeActivateProviderAsync(); }
    }

    private static void Durable(string path, object value)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, value); stream.Flush(true);
    }

    private static async Task VerifyAndRemoveFixtureAsync(MongoClient client,
        NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS mongo, string database, Holon[] records, Guid actor, string journal)
    {
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
        var cleanupId = Guid.NewGuid().ToString("N");
        var root = Path.GetDirectoryName(journal)!;
        var intent = new { database, actor, ids = records.Select(row => row.Id).ToArray(), journalSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(journal))) };
        Durable(Path.Combine(root, "mongo-cleanup-" + cleanupId + ".intent.json"), intent);
        Assert.IsFalse((await mongo.DeActivateProviderAsync()).IsError);
        await client.DropDatabaseAsync(database);
        using (var names = await client.ListDatabaseNamesAsync()) Assert.IsFalse((await names.ToListAsync()).Contains(database));
        Durable(Path.Combine(root, "mongo-cleanup-" + cleanupId + ".ack.json"), new { database, removed = true, intent.journalSha256 });
        File.Delete(journal);
    }

    [TestMethod]
    [TestCategory("MinioMongoReconciliation")]
    public async Task ReconcileOnlyTheOriginalJournalledTemporaryMongoFixtureWithoutSaving()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var path = Environment.GetEnvironmentVariable("COSMIC_MINIO_CQL_SOURCE")!;
        var state = Path.GetDirectoryName(path)!;
        using var sourceLease = new FileStream(Path.Combine(state, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var source = JsonSerializer.Deserialize<MinioCqlSource>(await File.ReadAllTextAsync(path))!;
        var journal = Path.Combine(state, "mongo.pending.json");
        using var pending = JsonDocument.Parse(await File.ReadAllTextAsync(journal));
        const string database = "cosmic_cql_mongo_minio_demo";
        Assert.AreEqual(database, pending.RootElement.GetProperty("database").GetString());
        var actor = pending.RootElement.GetProperty("actor").GetGuid(); Assert.AreEqual(source.Actor, actor);
        var ids = pending.RootElement.GetProperty("ids").EnumerateArray().Select(item => item.GetGuid()).ToArray();
        Assert.AreEqual(3, ids.Length); Assert.AreEqual(3, ids.Distinct().Count()); Assert.IsFalse(ids.Contains(Guid.Empty));
        CollectionAssert.AreEqual(source.LiveIds.Take(2).ToArray(), ids.Take(2).ToArray()); Assert.IsFalse(source.LiveIds.Contains(ids[2]));
        var records = ids.Select((id, index) => new Holon { Id = id, Name = "COSMIC owned MinIO Mongo copy " + index, CreatedByAvatarId = actor }).ToArray();
        var client = new MongoClient(uri);
        using (var names = await client.ListDatabaseNamesAsync()) Assert.IsTrue((await names.ToListAsync()).Contains(database));
        var mongo = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(uri, database) { IsVersionControlEnabled = false };
        try
        {
            Assert.IsFalse((await mongo.ActivateProviderAsync()).IsError);
            await VerifyAndRemoveFixtureAsync(client, mongo, database, records, actor, journal);
        }
        finally { await mongo.DeActivateProviderAsync(); }
    }
}
