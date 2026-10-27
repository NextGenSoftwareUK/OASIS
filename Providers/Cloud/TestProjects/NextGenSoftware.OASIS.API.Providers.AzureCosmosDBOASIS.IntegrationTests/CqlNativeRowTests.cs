using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class CqlNativeRowTests
{
    [TestMethod]
    public async Task JournaledNativeRowsRespectVisibilityWindowsAndBounds()
    {
        Assert.AreEqual("1", Environment.GetEnvironmentVariable("OASIS_COSMOS_CQL_NATIVE"));
        var work = Environment.GetEnvironmentVariable("OASIS_COSMOS_CQL_STATE") ?? throw new InvalidOperationException("Durable fixture directory required.");
        var journal = Path.Combine(work, "rows.pending.json");
        Assert.IsFalse(File.Exists(journal), "Preserve/reconcile the original intent; never replay creates.");
        const string databaseName = "oasis-cql-demo";
        const string key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        using var client = new CosmosClient("https://127.0.0.1:8081", key, new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway, RequestTimeout = TimeSpan.FromSeconds(10), MaxRetryAttemptsOnRateLimitedRequests = 0,
            HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            })
        });
        var actor = Guid.NewGuid();
        var other = Guid.NewGuid();
        var seeds = Enumerable.Range(0, 4).Select(i => new Holon
        {
            Id = Guid.NewGuid(), Name = "cql-coffee-" + i, Version = i + 2,
            CreatedByAvatarId = i == 0 ? actor : other, IsPublic = i is 1 or 3, IsActive = i != 3
        }).ToArray();
        const string precise = "9007199254740993.125";
        for (var i = 0; i < seeds.Length; i++)
        {
            seeds[i].MetaData = new()
            {
                ["location"] = new Dictionary<string, object> { ["city"] = i == 0 ? "London" : "Paris" },
                ["flag"] = i == 0,
                ["nullable"] = i == 0 ? null! : true
            };
            if (i < 3) seeds[i].MetaData["category"] = i == 0 ? "coffee" : i == 1 ? "tea" : null!;
        }
        seeds[0].MetaData["precise"] = decimal.Parse(precise, System.Globalization.CultureInfo.InvariantCulture);
        seeds[1].MetaData["precise"] = precise;
        Directory.CreateDirectory(work);
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new { databaseName, container = "holonItems", actor, ids = seeds.Select(h => h.Id).ToArray(), stage = "before-setup" }));
            file.Flush(true);
        }

        var database = client.GetDatabase(databaseName);
        try { await database.ReadAsync(); }
        catch (CosmosException cause) when (cause.StatusCode == HttpStatusCode.NotFound)
        {
            database = (await client.CreateDatabaseAsync(databaseName)).Database;
        }
        var containers = new List<string>();
        using (var iterator = database.GetContainerQueryIterator<ContainerProperties>())
            while (iterator.HasMoreResults) containers.AddRange((await iterator.ReadNextAsync()).Select(c => c.Id));
        Assert.IsTrue(containers.All(c => c is "holonItems" or "avatarItems" or "avatarDetailItems"), "Unexpected retained containers; preserve state.");
        var container = database.GetContainer("holonItems");
        if (!containers.Contains("holonItems")) container = (await database.CreateContainerAsync(new ContainerProperties("holonItems", "/id"))).Container;
        async Task AssertCount(long expected)
        {
            using var iterator = container.GetItemQueryIterator<long>("SELECT VALUE COUNT(1) FROM c");
            long count = 0;
            while (iterator.HasMoreResults) count += (await iterator.ReadNextAsync()).Single();
            Assert.AreEqual(expected, count, "Unexpected retained fixture data; preserve original state.");
        }
        await AssertCount(0);
        using var factory = new CosmosDbClientFactory(client, databaseName, new() { "holonItems" });
        var repository = new HolonRepository(factory);
        foreach (var seed in seeds) await repository.AddAsync(seed);
        await AssertCount(4);

        var provider = new AzureCosmosDBOASIS(new Uri("https://127.0.0.1:8081"), key, databaseName,
            new() { "holonItems", "avatarItems", "avatarDetailItems" });
        try
        {
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);
            Assert.IsTrue(activation.Result);

            async Task<IReadOnlyList<IHolon>> Read(CosmicQuery<Holon> query, int maximum = 10)
            {
                var plan = CosmicQueryPlanner.Plan(query, ProviderType.AzureCosmosDBOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.AzureCosmosDBOASIS, CosmosCqlRowCommand.Capabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await provider.ExecuteCosmicQueryAsync(plan.Result, maximum);
                Assert.IsFalse(result.IsError, result.Message + "; " + result.Exception);
                Assert.IsTrue(result.IsLoaded);
                Assert.AreEqual(result.Result.Count, result.LoadedCount);
                return result.Result;
            }
            var access = CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
            var visible = CosmicQuery<Holon>.Create().WithReadAccess(access).Where(h => h.IsActive && h.Version >= 2).OrderBy(h => h.Id);
            Assert.IsFalse(visible.IsError, visible.Message);
            var expected = seeds.Take(2).OrderBy(h => h.Id.ToString("D"), StringComparer.Ordinal).Select(h => h.Id).ToArray();
            CollectionAssert.AreEqual(expected, (await Read(visible.Result)).Select(h => h.Id).ToArray());
            var window = visible.Skip(1).Take(1);
            Assert.IsFalse(window.IsError, window.Message);
            Assert.AreEqual(expected[1], (await Read(window.Result)).Single().Id);
            Assert.AreEqual(0, (await Read(CosmicQuery<Holon>.Create().Where(h => h.Name == "absent-owned-cql").Result)).Count);
            Assert.AreEqual(4, (await Read(CosmicQuery<Holon>.Create().Where(h => h.Description == null).Result)).Count);
            var overflowPlan = CosmicQueryPlanner.Plan(visible.Result, ProviderType.AzureCosmosDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.AzureCosmosDBOASIS, AzureCosmosDBOASIS.CqlCapabilities) });
            Assert.IsFalse(overflowPlan.IsError, overflowPlan.Message);
            var overflow = await provider.ExecuteCosmicQueryAsync(overflowPlan.Result, 1);
            Assert.IsTrue(overflow.IsError);
            Assert.IsNull(overflow.Result);

            // Canonical metadata domains must agree with the exact-number codec,
            // and distinguish absent paths, explicit nulls and wrong types.
            Assert.AreEqual(seeds[0].Id, (await Read(CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<string>(h, "category") == "coffee").Result)).Single().Id);
            Assert.AreEqual(seeds[0].Id, (await Read(CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<string>(h, "location", "city") == "London").Result)).Single().Id);
            Assert.AreEqual(seeds[1].Id, (await Read(CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<string>(h, "precise") == precise).Result)).Single().Id);
            Assert.AreEqual(3, (await Read(CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Exists(h, "category")).Result)).Count);
            Assert.AreEqual(seeds[2].Id, (await Read(CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.IsNull(h, "category")).Result)).Single().Id);
            Assert.AreEqual(seeds[0].Id, (await Read(CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<bool?>(h, "nullable") == null).Result)).Single().Id);
            Assert.AreEqual(2, (await Read(CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<string>(h, "category") != "coffee").Result)).Count);
            Assert.AreEqual(0, (await Read(CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<string>(h, "flag") == "True").Result)).Count);
            Assert.AreEqual(seeds[0].Id, (await Read(CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<bool>(h, "flag") == true).Result)).Single().Id);

            // Delete only these original identities, with independently observed ETags.
            foreach (var seed in seeds)
            {
                var partition = new PartitionKey(seed.Id.ToString("D"));
                using var point = await container.ReadItemStreamAsync(seed.Id.ToString("D"), partition);
                Assert.AreEqual(HttpStatusCode.OK, point.StatusCode);
                using var reader = new StreamReader(point.Content);
                using var body = JsonDocument.Parse(await reader.ReadToEndAsync());
                Assert.AreEqual(seed.Id, body.RootElement.GetProperty("Id").GetGuid());
                Assert.AreEqual(seed.Name, body.RootElement.GetProperty("Name").GetString());
                using var deleted = await container.DeleteItemStreamAsync(seed.Id.ToString("D"), partition, new ItemRequestOptions { IfMatchEtag = point.Headers.ETag });
                Assert.AreEqual(HttpStatusCode.NoContent, deleted.StatusCode);
            }
            await AssertCount(0);
            File.Delete(journal);
        }
        finally
        {
            var deactivation = await provider.DeActivateProviderAsync();
            Assert.IsFalse(deactivation.IsError, deactivation.Message);
        }
    }
}
