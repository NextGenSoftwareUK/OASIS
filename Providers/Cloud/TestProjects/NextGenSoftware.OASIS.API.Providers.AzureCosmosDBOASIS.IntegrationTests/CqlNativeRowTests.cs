using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class CqlNativeRowTests
{
    public sealed class UnconstructibleCountRow : Holon
    {
        public UnconstructibleCountRow() => throw new InvalidOperationException("Native cardinality must not hydrate entities.");
    }
    [TestMethod]
    public async Task JournaledNativeRowsRespectVisibilityWindowsAndBounds()
    {
        Assert.AreEqual("1", Environment.GetEnvironmentVariable("OASIS_COSMOS_CQL_NATIVE"));
        var work = Environment.GetEnvironmentVariable("OASIS_COSMOS_CQL_STATE") ?? throw new InvalidOperationException("Durable fixture directory required.");
        var journal = Path.Combine(work, "rows.pending.json");
        var reviewedCount = Environment.GetEnvironmentVariable("OASIS_COSMOS_CQL_REVIEWED_COUNT") == "1";
        var reviewedProjection = Environment.GetEnvironmentVariable("OASIS_COSMOS_CQL_REVIEWED_PROJECTION") == "1";
        Assert.IsFalse(reviewedCount && reviewedProjection, "Choose one exact original fixture.");
        var reviewed = reviewedCount || reviewedProjection;
        Guid[]? reviewedIds = null;
        var actor = Guid.NewGuid();
        if (reviewed)
        {
            var bytes = await File.ReadAllBytesAsync(journal);
            Assert.AreEqual(reviewedProjection ? "6BA5D0DDC1C2D5D90A4D9F2BBCDCCA6682E817381B18865E718C7DCBD4B01492"
                : "ACEC97647EBBF2041FD0241FEF38A41A28FE3BCB63315081DEDCF80704327470", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
            using var original = JsonDocument.Parse(bytes);
            Assert.AreEqual("oasis-cql-demo", original.RootElement.GetProperty("databaseName").GetString());
            actor = original.RootElement.GetProperty("actor").GetGuid();
            reviewedIds = original.RootElement.GetProperty("ids").EnumerateArray().Select(id => id.GetGuid()).ToArray();
            Assert.AreEqual(4, reviewedIds.Length);
            Assert.AreEqual(4, reviewedIds.Distinct().Count());
        }
        else Assert.IsFalse(File.Exists(journal), "Preserve/reconcile the original intent; never replay creates.");
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
        var other = Guid.NewGuid();
        var seeds = Enumerable.Range(0, 4).Select(i => new Holon
        {
            Id = reviewedIds == null ? Guid.NewGuid() : reviewedIds[i], Name = "cql-coffee-" + i, Version = i == 1 ? 2 : i + 2,
            CreatedByAvatarId = i == 0 ? actor : other, IsPublic = i is 1 or 3, IsActive = i != 3
        }).ToArray();
        const string precise = "9007199254740993.125";
        for (var i = 0; i < seeds.Length; i++)
        {
            seeds[i].MetaData = new()
            {
                ["location"] = new Dictionary<string, object> { ["city"] = i == 0 ? "London" : "Paris" },
                ["flag"] = i == 0,
                ["fixture"] = "cosmic-cloud-rows-demo",
                ["nullable"] = i == 0 ? null! : true
            };
            if (i < 3) seeds[i].MetaData["category"] = i == 0 ? "coffee" : i == 1 ? "tea" : null!;
        }
        seeds[0].MetaData["precise"] = decimal.Parse(precise, System.Globalization.CultureInfo.InvariantCulture);
        seeds[1].MetaData["precise"] = precise;
        if (!reviewed)
        {
            Directory.CreateDirectory(work);
            using var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
            await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new { databaseName, container = "holonItems", actor, ids = seeds.Select(h => h.Id).ToArray(), stage = "before-setup" }));
            file.Flush(true);
        }

        var database = client.GetDatabase(databaseName);
        try { await database.ReadAsync(); }
        catch (CosmosException cause) when (cause.StatusCode == HttpStatusCode.NotFound)
        {
            Assert.IsFalse(reviewed, "Original database must exist; no recovery create.");
            database = (await client.CreateDatabaseAsync(databaseName)).Database;
        }
        var containers = new List<string>();
        using (var iterator = database.GetContainerQueryIterator<ContainerProperties>())
            while (iterator.HasMoreResults) containers.AddRange((await iterator.ReadNextAsync()).Select(c => c.Id));
        Assert.IsTrue(containers.All(c => c is "holonItems" or "avatarItems" or "avatarDetailItems"), "Unexpected retained containers; preserve state.");
        var container = database.GetContainer("holonItems");
        if (!containers.Contains("holonItems"))
        {
            Assert.IsFalse(reviewed, "Original container must exist; no recovery create.");
            container = (await database.CreateContainerAsync(new ContainerProperties("holonItems", "/id"))).Container;
        }
        if (reviewed) Assert.AreEqual(3, containers.Count, "All original provider containers must exist.");
        async Task AssertCount(long expected)
        {
            using var iterator = container.GetItemQueryIterator<long>("SELECT VALUE COUNT(1) FROM c");
            long count = 0;
            while (iterator.HasMoreResults) count += (await iterator.ReadNextAsync()).Single();
            Assert.AreEqual(expected, count, "Unexpected retained fixture data; preserve original state.");
        }
        await AssertCount(reviewed ? 4 : 0);
        if (!reviewed)
        {
            // Explicit owned-fixture deployment setup. Native query execution
            // never changes a container's indexing policy.
            var observed = await container.ReadContainerAsync();
            var properties = observed.Resource;
            var changed = false;
            foreach (var definition in AzureCosmosDBOASIS.GetCqlCompositeIndexDefinitions())
            {
                if (properties.IndexingPolicy.CompositeIndexes.Any(existing =>
                    existing.Count == definition.Count && existing.Zip(definition).All(pair =>
                        pair.First.Path == pair.Second.Path && pair.First.Order == pair.Second.Order))) continue;
                properties.IndexingPolicy.CompositeIndexes.Add(definition);
                changed = true;
            }
            if (changed) await container.ReplaceContainerAsync(properties,
                new ContainerRequestOptions { IfMatchEtag = observed.Headers.ETag });
        }
        using var factory = new CosmosDbClientFactory(client, databaseName, new() { "holonItems" });
        var repository = new HolonRepository(factory);
        if (!reviewed) foreach (var seed in seeds) await repository.AddAsync(seed);
        else foreach (var seed in seeds)
        {
            var original = await repository.GetByIdAsync(seed.Id.ToString("D"));
            Assert.IsNotNull(original);
            Assert.AreEqual(seed.Name, original.Name);
            Assert.AreEqual(seed.Version, original.Version);
            Assert.AreEqual(seed.IsActive, original.IsActive);
            Assert.AreEqual(seed.IsPublic, original.IsPublic);
        }
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

            async Task<TResult> Scalar<T, TResult>(CosmicScalarQuery<T, TResult> query) where T : IHolon
            {
                var plan = CosmicScalarPlanner.Plan(query, ProviderType.AzureCosmosDBOASIS,
                    new[] { new CosmicScalarTarget(ProviderType.AzureCosmosDBOASIS, AzureCosmosDBOASIS.CqlCapabilities, AzureCosmosDBOASIS.CqlScalarCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await provider.ExecuteCosmicScalarAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Message + "; " + result.Exception);
                Assert.IsTrue(result.IsLoaded);
                return result.Result;
            }

            Assert.AreEqual(2, await Scalar(visible.Count().Result));
            Assert.AreEqual(2L, await Scalar(visible.LongCount().Result));
            Assert.IsTrue(await Scalar(visible.Any().Result));
            Assert.AreEqual(1, await Scalar(window.Count().Result));
            Assert.IsFalse(await Scalar(CosmicQuery<Holon>.Create().Where(h => h.Name == "absent-owned-cql").Any().Result));
            Assert.AreEqual(0, await Scalar(CosmicQuery<Holon>.Create().Skip(100).Count().Result));
            Assert.AreEqual(0, await Scalar(CosmicQuery<Holon>.Create().Take(0).Count().Result));
            Assert.AreEqual(1, await Scalar(CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<string>(h, "precise") == precise).Count().Result));
            Assert.AreEqual(3L, await Scalar(CosmicQuery<UnconstructibleCountRow>.Create().Where(h => h.IsActive).LongCount().Result));

            CosmicProjectionPlan<T, TResult> ProjectionPlan<T, TResult>(CosmicProjectionQuery<T, TResult> query) where T : IHolon
            {
                var plan = CosmicProjectionPlanner.Plan(query, ProviderType.AzureCosmosDBOASIS,
                    new[] { new CosmicProjectionTarget(ProviderType.AzureCosmosDBOASIS, AzureCosmosDBOASIS.CqlCapabilities, AzureCosmosDBOASIS.CqlProjectionCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                return plan.Result;
            }
            async Task<System.Collections.ObjectModel.ReadOnlyCollection<TResult>> Project<T, TResult>(CosmicProjectionQuery<T, TResult> query) where T : IHolon
            {
                var result = await provider.ExecuteCosmicProjectionAsync(ProjectionPlan(query));
                Assert.IsFalse(result.IsError, result.Message + "; " + result.Exception);
                var materialized = CosmicProjectionResults.Materialize(query, result.Result);
                Assert.IsFalse(materialized.IsError, materialized.Message);
                return materialized.Result;
            }

            var canonical = await Project(visible.Select(h => new { h.Id, h.Name, h.Version }).Result);
            CollectionAssert.AreEqual(expected, canonical.Select(row => row.Id).ToArray());
            Assert.AreEqual(seeds.Single(h => h.Id == canonical[0].Id).Version, canonical[0].Version);
            Assert.AreEqual(seeds.Single(h => h.Id == expected[1]).Name, (await Project(window.Select(h => h.Name).Result)).Single());
            var metadataProjection = await Project(visible.Select(h => new
            {
                City = CosmicQueryMetadata.Value<string>(h, "location", "city"),
                Flag = CosmicQueryMetadata.Value<bool>(h, "flag"),
                Nullable = CosmicQueryMetadata.Value<bool?>(h, "nullable")
            }).Result);
            Assert.AreEqual(2, metadataProjection.Count);
            Assert.IsNull(metadataProjection.Single(row => row.Flag).Nullable);
            Assert.AreEqual("London", metadataProjection.Single(row => row.Flag).City);
            var firstId = seeds[0].Id;
            var secondId = seeds[1].Id;
            var lastId = seeds[3].Id;
            Assert.IsNull((await Project(CosmicQuery<Holon>.Create().Where(h => h.Id == lastId)
                .Select(h => CosmicQueryMetadata.Value<string>(h, "missing")).Result)).Single());
            var framed = CosmicQuery<Holon>.Create().Where(h => h.Id == firstId)
                .Select(h => CosmicQueryMetadata.Value<string>(h, "precise")).Result;
            Assert.IsTrue((await provider.ExecuteCosmicProjectionAsync(ProjectionPlan(framed))).IsError);
            var wrongType = CosmicQuery<Holon>.Create().Where(h => h.Id == secondId)
                .Select(h => CosmicQueryMetadata.Value<string>(h, "flag")).Result;
            Assert.IsTrue((await provider.ExecuteCosmicProjectionAsync(ProjectionPlan(wrongType))).IsError);
            Assert.AreEqual(3, (await Project(CosmicQuery<UnconstructibleCountRow>.Create().Where(h => h.IsActive)
                .Select(h => new { h.Name }).Result)).Count);
            Assert.IsTrue((await provider.ExecuteCosmicProjectionAsync(ProjectionPlan(visible.Select(h => h.Name).Result), maximumPayloadBytes: 1)).IsError);

            // Version ties exercise the explicit deployment index profile and
            // stable Id tie-breaks in all four direction combinations.
            var ascending = seeds.OrderBy(h => h.Version).ThenBy(h => h.Id.ToString("D"), StringComparer.Ordinal).Select(h => h.Id).ToArray();
            var descending = seeds.OrderByDescending(h => h.Version).ThenBy(h => h.Id.ToString("D"), StringComparer.Ordinal).Select(h => h.Id).ToArray();
            CollectionAssert.AreEqual(ascending, (await Read(CosmicQuery<Holon>.Create().OrderBy(h => h.Version).Result)).Select(h => h.Id).ToArray());
            CollectionAssert.AreEqual(descending, (await Read(CosmicQuery<Holon>.Create().OrderByDescending(h => h.Version).Result)).Select(h => h.Id).ToArray());
            CollectionAssert.AreEqual(seeds.OrderBy(h => h.Version).ThenByDescending(h => h.Id.ToString("D"), StringComparer.Ordinal).Select(h => h.Id).ToArray(),
                (await Read(CosmicQuery<Holon>.Create().OrderBy(h => h.Version).ThenByDescending(h => h.Id).Result)).Select(h => h.Id).ToArray());
            CollectionAssert.AreEqual(seeds.OrderByDescending(h => h.Version).ThenByDescending(h => h.Id.ToString("D"), StringComparer.Ordinal).Select(h => h.Id).ToArray(),
                (await Read(CosmicQuery<Holon>.Create().OrderByDescending(h => h.Version).ThenByDescending(h => h.Id).Result)).Select(h => h.Id).ToArray());
            CollectionAssert.AreEqual(ascending, (await Read(CosmicQuery<Holon>.Create().OrderBy(h => h.Version).ThenByDescending(h => h.Version).Result)).Select(h => h.Id).ToArray());
            CollectionAssert.AreEqual(seeds.OrderBy(h => h.Id.ToString("D"), StringComparer.Ordinal).Select(h => h.Id).ToArray(),
                (await Read(CosmicQuery<Holon>.Create().OrderBy(h => h.Id).ThenByDescending(h => h.Version).Result)).Select(h => h.Id).ToArray());
            CollectionAssert.AreEqual(ascending.Take(2).ToArray(), (await Project(CosmicQuery<Holon>.Create()
                .OrderBy(h => h.Version).Take(2).Select(h => h.Id).Result)).ToArray());

            // A trusted complete catalogue replaces owner/public visibility.
            // This grants its private member and excludes public/owned outsiders.
            var catalogue = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.AzureCosmosDBOASIS,
                new[] { seeds[2].Id });
            Assert.IsFalse(catalogue.IsError, catalogue.Message);
            var catalogued = CosmicQuery<Holon>.Create().WithReadAccess(catalogue.Result).Where(h => h.IsActive);
            Assert.IsFalse(catalogued.IsError, catalogued.Message);
            Assert.AreEqual(seeds[2].Id, (await Read(catalogued.Result)).Single().Id);
            Assert.AreEqual(1, await Scalar(catalogued.Count().Result));
            Assert.AreEqual(seeds[2].Id, (await Project(catalogued.Select(h => h.Id).Result)).Single());
            var emptyCatalogue = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.AzureCosmosDBOASIS, Array.Empty<Guid>());
            Assert.IsFalse(emptyCatalogue.IsError, emptyCatalogue.Message);
            var denied = CosmicQuery<Holon>.Create().WithReadAccess(emptyCatalogue.Result);
            Assert.IsFalse(denied.IsError, denied.Message);
            Assert.AreEqual(0, (await Read(denied.Result)).Count);
            Assert.IsFalse(await Scalar(denied.Any().Result));
            var selectedNames = new[] { seeds[0].Name, seeds[1].Name };
            Assert.AreEqual(2, (await Read(CosmicQuery<Holon>.Create().Where(h => selectedNames.Contains(h.Name)).Result)).Count);
            var inactive = new[] { false };
            Assert.AreEqual(seeds[3].Id, (await Read(CosmicQuery<Holon>.Create().Where(h => inactive.Contains(h.IsActive)).Result)).Single().Id);
            var versions = new[] { 2 };
            Assert.AreEqual(2, (await Read(CosmicQuery<Holon>.Create().Where(h => versions.Contains(h.Version)).Result)).Count);
            var descriptions = new string?[] { null };
            Assert.AreEqual(4, (await Read(CosmicQuery<Holon>.Create().Where(h => descriptions.Contains(h.Description)).Result)).Count);

            // Exercise the same provider-neutral demo source used by developers,
            // through the existing V2 manager and standard IQueryable binding.
            var dna = new OASISDNA
            {
                OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                {
                    HyperDriveMode = HyperDriveModes.V2,
                    StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false }
                }
            };
            var registry = new ProviderManager(null, dna)
            {
                IsAutoFailOverEnabled = false,
                IsAutoReplicationEnabled = false,
                IsAutoLoadBalanceEnabled = false
            };
            var manager = new HolonManager(provider, dna, registry);
            using var demoOutput = new StringWriter();
            var demo = await CosmicCqlDemo.CloudRowsWalkthrough.RunAsync(manager, provider, actor,
                "cosmic-cloud-rows-demo", demoOutput);
            Assert.AreEqual(2, demo.Count);
            Assert.AreEqual(2L, demo.LongCount);
            Assert.IsTrue(demo.Any);
            CollectionAssert.AreEqual(expected, demo.Page.Select(row => row.Id).ToArray());

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
