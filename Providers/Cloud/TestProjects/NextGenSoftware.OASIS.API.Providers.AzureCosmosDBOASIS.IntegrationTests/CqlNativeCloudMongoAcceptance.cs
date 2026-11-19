using System.Text.Json;
using System.Net;
using System.Net.Http.Json;
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

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

internal static class CqlNativeCloudMongoAcceptance
{
    internal static async Task VerifyAsync(string root, HolonManager manager, AzureCosmosDBOASIS cosmos, Guid[] cosmosIds, Guid actor, bool hosted = false)
    {
        const string database = "cosmic_cql_mongo_cosmos_demo";
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        var journal = Path.Combine(root, "LocalState/COSMIC/CosmosCqlCurrent/mongo.pending.json");
        Assert.IsFalse(File.Exists(journal), "Preserve original Mongo fixture; no replay.");
        var client = new MongoClient(uri);
        using (var names = await client.ListDatabaseNamesAsync())
            Assert.IsFalse((await names.ToListAsync()).Contains(database));

        var records = cosmosIds.Take(2).Append(Guid.NewGuid()).Select((id, index) => new Holon
        {
            Id = id, Name = "COSMIC owned Mongo Cosmos copy " + index, CreatedByAvatarId = actor
        }).ToArray();
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(file, new { database, actor, ids = records.Select(row => row.Id) });
            file.Flush(true);
        }

        var mongo = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(uri, database)
        { IsVersionControlEnabled = false };
        try
        {
            Assert.IsFalse((await mongo.ActivateProviderAsync()).IsError);
            foreach (var record in records)
                Assert.IsFalse((await mongo.SaveHolonAsync(record, saveChildren: false)).IsError);

            using var output = new StringWriter();
            foreach (var selection in new[] { cosmosIds, cosmosIds.Take(2).ToArray() })
            {
                var result = await CosmicCqlDemo.CloudFederationWalkthrough.RunAsync(
                    manager, mongo, cosmos, selection, records.Select(row => row.Id), cosmosIds, actor, output);
                Assert.AreEqual(selection.Length + 2, result.ObservedInputRows);
                Assert.AreEqual(selection.Length + 2, result.Rows.Count);
                Assert.AreEqual(2, result.Rows.Count(row => row.Selected.Provider == ProviderType.MongoDBOASIS));
                Assert.AreEqual(selection.Length, result.Rows.Count(row => row.Selected.Provider == ProviderType.AzureCosmosDBOASIS));
                Assert.IsFalse(result.Rows.Any(row => row.Selected.Row.Id == records[2].Id));
            }

            // HTTP uses the same walkthrough with server-owned identity catalogues.
            await using (var app = CosmicCqlDemo.CloudFederationApiHost.Build(
                manager, mongo, cosmos, actor, cosmosIds, records.Select(row => row.Id), cosmosIds,
                "http://127.0.0.1:0", hosted: hosted))
            {
                await app.StartAsync();
                try
                {
                    var address = app.Services.GetRequiredService<IServer>()
                        .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var http = new HttpClient { BaseAddress = new Uri(address) };
                    var page = await http.GetStringAsync("/");
                    StringAssert.Contains(page, "Query two-record subset");
                    StringAssert.Contains(page, hosted ? "hosted Azure Cosmos DB" : "local Cosmos emulator");
                    using var invalid = await http.PostAsJsonAsync("/api/cql/federation", new { preset = "foreign" });
                    Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
                    using var forged = await http.PostAsJsonAsync("/api/cql/federation", new { preset = "all", actorId = Guid.NewGuid() });
                    Assert.AreEqual(HttpStatusCode.BadRequest, forged.StatusCode);
                    foreach (var preset in new[] { "all", "subset" })
                    {
                        using var response = await http.PostAsJsonAsync("/api/cql/federation", new { preset });
                        Assert.IsTrue(response.IsSuccessStatusCode);
                        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                        var expected = preset == "all" ? 5 : 4;
                        Assert.AreEqual(expected, body.RootElement.GetProperty("count").GetInt32());
                        Assert.AreEqual(expected, body.RootElement.GetProperty("observedPhysicalRows").GetInt32());
                        var copies = body.RootElement.GetProperty("rows").EnumerateArray().ToArray();
                        Assert.AreEqual(2, copies.Count(row => row.GetProperty("provider").GetString() == "MongoDBOASIS"));
                        Assert.AreEqual(expected - 2, copies.Count(row => row.GetProperty("provider").GetString() == "AzureCosmosDBOASIS"));
                    }
                }
                finally { await app.StopAsync(); }
            }

            // Interactive use follows timed acceptance; human demo time is unbounded.
            if (Environment.GetEnvironmentVariable("OASIS_COSMOS_MONGO_SERVE") == "1")
            {
                await using var interactive = CosmicCqlDemo.CloudFederationApiHost.Build(
                    manager, mongo, cosmos, actor, cosmosIds, records.Select(row => row.Id), cosmosIds, hosted: hosted);
                await interactive.StartAsync();
                Console.WriteLine("Mongo + Cosmos demo ready: http://127.0.0.1:5092/ ; stop with the page button for exact fixture cleanup.");
                await interactive.WaitForShutdownAsync();
            }

            // This exact database was absent before intent publication. Verify its
            // complete physical ownership before dropping only this test database.
            var native = client.GetDatabase(database);
            using (var names = await native.ListCollectionNamesAsync())
                CollectionAssert.AreEquivalent(new[] { "Avatar", "AvatarDetail", "CosmicHolonOutbox", "Holon" },
                    (await names.ToListAsync()).ToArray());
            foreach (var empty in new[] { "Avatar", "AvatarDetail", "CosmicHolonOutbox" })
                Assert.AreEqual(0L, await native.GetCollection<BsonDocument>(empty).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.AreEqual(3L, await native.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            foreach (var record in records)
            {
                var loaded = await mongo.LoadHolonAsync(record.Id, loadChildren: false);
                Assert.IsFalse(loaded.IsError);
                Assert.AreEqual(actor, loaded.Result.CreatedByAvatarId);
                Assert.AreEqual(record.Name, loaded.Result.Name);
            }
            Assert.IsFalse((await mongo.DeActivateProviderAsync()).IsError);
            await client.DropDatabaseAsync(database);
            using (var names = await client.ListDatabaseNamesAsync())
                Assert.IsFalse((await names.ToListAsync()).Contains(database));
            File.Delete(journal);
        }
        finally { await mongo.DeActivateProviderAsync(); }
    }
}
