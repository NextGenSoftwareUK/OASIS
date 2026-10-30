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

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public partial class OracleCqlContentNativeTests
{
    private static async Task VerifyContentMongoAsync(string root, OracleDBOASIS oracle, Guid actor, Guid[] oracleIds)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string database = "cosmic_cql_mongo_oracle_content_demo";
        var state = Path.Combine(root, "LocalState/COSMIC/OracleContentCql");
        var pending = Path.Combine(state, "mongo-content.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve original Mongo outcome; never reseed.");
        var client = new MongoClient(uri);
        using (var names = await client.ListDatabaseNamesAsync())
            Assert.IsFalse((await names.ToListAsync()).Contains(database), "Never adopt another database.");

        // Two matching physical copies plus a deliberately unauthorized record.
        // The Oracle source is the retained four-record content fixture, unchanged.
        var owner = Guid.NewGuid().ToString("D");
        var records = oracleIds.Take(2).Append(Guid.NewGuid()).Select((id, index) => new Holon
        {
            Id = id,
            Name = new[] { "Espresso", "Latte", "Hidden coffee" }[index],
            Description = "coffee",
            CreatedByAvatarId = actor,
            MetaData = new Dictionary<string, object> { ["category"] = "coffee" }
        }).ToArray();
        using (var file = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        { JsonSerializer.Serialize(file, new { owner, database, actor, ids = records.Select(row => row.Id) }); file.Flush(true); }
        var native = client.GetDatabase(database);
        await native.GetCollection<BsonDocument>("CosmicOwnedFixture").InsertOneAsync(new BsonDocument("_id", owner));
        var mongo = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(uri, database) { IsVersionControlEnabled = false };
        try
        {
            Assert.IsFalse((await mongo.ActivateProviderAsync()).IsError);
            foreach (var record in records)
            {
                var saved = await mongo.SaveHolonAsync(record, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
            }

            var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                StorageProviders = new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings { LogSwitchingProviders = false }
            } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var registry = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
            registry.RegisterProvider(mongo); registry.RegisterProvider(oracle);
            var manager = new HolonManager(null, dna, registry);
            var readableMongo = records.Take(2).Select(row => row.Id).ToArray();
            var presets = CosmicCqlDemo.ContentFederationPresets.Create(ProviderType.OracleDBOASIS);
            var expected = new Dictionary<string, int> { ["name"] = 2, ["description"] = 4, ["metadata"] = 4, ["combined"] = 2 };

            // C#: the same captured LINQ-like predicates run natively on both
            // providers through HolonManager, with independent authorization.
            foreach (var preset in presets)
            {
                var result = await CosmicCqlDemo.IdentityFederationWalkthrough.RunQueryAsync(manager, mongo, oracle,
                    preset.Value, readableMongo, oracleIds, actor, TextWriter.Null);
                Assert.AreEqual(expected[preset.Key], result.Rows.Count);
                Assert.AreEqual(expected[preset.Key], result.ObservedInputRows);
                foreach (var provider in new[] { ProviderType.MongoDBOASIS, ProviderType.OracleDBOASIS })
                    Assert.AreEqual(expected[preset.Key] / 2, result.Rows.Count(row => row.Selected.Provider == provider));
                Assert.IsFalse(result.Rows.Any(row => row.Selected.Row.Id == records[2].Id));
            }

            // HTTP/browser input selects a named example only. It cannot replace
            // the trusted actor, provider scope or source authorization catalogues.
            await using var app = CosmicCqlDemo.IdentityFederationApiHost.Build(manager, mongo, oracle, actor, oracleIds,
                readableMongo, oracleIds, "http://127.0.0.1:0", presets);
            await app.StartAsync();
            try
            {
                var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                using var http = new HttpClient { BaseAddress = new Uri(address) };
                var page = await http.GetStringAsync("/");
                foreach (var preset in presets.Keys) StringAssert.Contains(page, preset);
                using var forged = await http.PostAsJsonAsync("/api/cql/federation", new { preset = "name", actorId = Guid.NewGuid() });
                Assert.AreEqual(HttpStatusCode.BadRequest, forged.StatusCode);
                using var unknown = await http.PostAsJsonAsync("/api/cql/federation", new { preset = "not-a-preset" });
                Assert.AreEqual(HttpStatusCode.BadRequest, unknown.StatusCode);
                foreach (var preset in presets.Keys)
                {
                    using var response = await http.PostAsJsonAsync("/api/cql/federation", new { preset });
                    Assert.IsTrue(response.IsSuccessStatusCode);
                    using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    Assert.AreEqual(preset, body.RootElement.GetProperty("preset").GetString());
                    Assert.AreEqual(expected[preset], body.RootElement.GetProperty("count").GetInt32());
                    var copies = body.RootElement.GetProperty("rows").EnumerateArray().ToArray();
                    foreach (var provider in new[] { "MongoDBOASIS", "OracleDBOASIS" })
                        Assert.AreEqual(expected[preset] / 2, copies.Count(row => row.GetProperty("provider").GetString() == provider));
                }
            }
            finally { await app.StopAsync(); }

            if (Environment.GetEnvironmentVariable("OASIS_ORACLE_MONGO_SERVE") == "1")
            {
                await using var interactive = CosmicCqlDemo.IdentityFederationApiHost.Build(manager, mongo, oracle, actor,
                    oracleIds, readableMongo, oracleIds, "http://127.0.0.1:5090", presets);
                await interactive.StartAsync();
                Console.WriteLine("Oracle content + Mongo ready: http://127.0.0.1:5090/ . Stop with the page button for owned fixture cleanup.");
                await interactive.WaitForShutdownAsync();
            }

            // The database was absent before our durable ownership intent. Require
            // exact records and no unrelated collections before removing only it.
            using (var names = await native.ListCollectionNamesAsync())
                CollectionAssert.AreEquivalent(new[] { "CosmicOwnedFixture", "Avatar", "AvatarDetail", "CosmicHolonOutbox", "Holon" }, (await names.ToListAsync()).ToArray());
            Assert.AreEqual(1L, await native.GetCollection<BsonDocument>("CosmicOwnedFixture").CountDocumentsAsync(new BsonDocument("_id", owner)));
            Assert.AreEqual(3L, await native.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            foreach (var record in records)
            {
                var loaded = await mongo.LoadHolonAsync(record.Id, loadChildren: false);
                Assert.IsFalse(loaded.IsError); Assert.AreEqual(record.Name, loaded.Result.Name); Assert.AreEqual(actor, loaded.Result.CreatedByAvatarId);
            }
            foreach (var empty in new[] { "Avatar", "AvatarDetail", "CosmicHolonOutbox" })
                Assert.AreEqual(0L, await native.GetCollection<BsonDocument>(empty).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.IsFalse((await mongo.DeActivateProviderAsync()).IsError);
            await client.DropDatabaseAsync(database);
            using (var names = await client.ListDatabaseNamesAsync()) Assert.IsFalse((await names.ToListAsync()).Contains(database));
            File.Delete(pending);
            Console.WriteLine("Oracle content federation: four C# presets, four HTTP presets, two rejected requests; native source unchanged.");
        }
        finally { await mongo.DeActivateProviderAsync(); }
    }
}
