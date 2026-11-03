using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class AzureBlobCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);
    internal static readonly string[] Containers = { "cosmic-cql-holons", "cosmic-cql-avatars", "cosmic-cql-details" };
    internal static AzureStorageOASIS Create() => CloudProviderFactory.CreateAzureStorage(new AzureStorageOASISProviderSettings
    {
        ConnectionString = "UseDevelopmentStorage=true", HolonContainer = Containers[0],
        AvatarContainer = Containers[1], AvatarDetailContainer = Containers[2], IsVersionControlEnabled = true
    }, null!);

    [TestMethod, TestCategory("LocalNative")]
    public async Task NativeBlobSourceFiltersThreeToTwoAndPreservesEtagsAndContent()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_AZURE_BLOB_CQL_STATE")
            ?? throw new InvalidOperationException("Use the explicit Azure Blob CQL runner.");
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve original setup; never replay or reseed.");
        var native = new BlobServiceClient("UseDevelopmentStorage=true");
        var provider = Create();
        Seed seed;
        if (!File.Exists(manifest))
        {
            foreach (var name in Containers) Assert.IsFalse((await native.GetBlobContainerClient(name).ExistsAsync()).Value,
                "A missing manifest must not adopt existing containers.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray());
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);
            var names = new[] { "Espresso", "Latte", "Tea" };
            for (var i = 0; i < 3; i++)
            {
                var row = new Holon { Id = seed.Ids[i], Name = names[i], CreatedByAvatarId = seed.Actor, IsNewHolon = true };
                var saved = await provider.SaveHolonAsync(row, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Exception?.ToString() ?? saved.Message);
                Assert.IsTrue(saved.IsSaved);
            }
            File.Move(pending, manifest);
        }
        else
        {
            seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
            foreach (var name in Containers) Assert.IsTrue((await native.GetBlobContainerClient(name).ExistsAsync()).Value,
                "Original source container missing; do not recreate it.");
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }
        try
        {
            Assert.AreEqual(3, seed.Ids.Distinct().Count());
            var container = native.GetBlobContainerClient(Containers[0]);
            async Task<string[]> Snapshot()
            {
                var values = new List<string>();
                foreach (var id in seed.Ids)
                {
                    var blob = (await container.GetBlobClient(id.ToString("D")).DownloadContentAsync()).Value;
                    values.Add(blob.Details.ETag + "|" + blob.Content.ToString());
                }
                return values.ToArray();
            }
            var before = await Snapshot();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AzureStorageOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.AzureStorageOASIS, seed.Ids).Result).Result;
            foreach (var ids in new[] { seed.Ids, seed.Ids.Take(2).ToArray() })
            {
                var plan = CosmicQueryPlanner.Plan(query.Where(row => ids.Contains(row.Id)).Result, ProviderType.AzureStorageOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.AzureStorageOASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
                CollectionAssert.AreEquivalent(ids, result.Result.Select(row => row.Id).ToArray());
            }
            var missing = Guid.NewGuid();
            var missingQuery = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AzureStorageOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.AzureStorageOASIS, new[] { missing }).Result).Result;
            var missingPlan = CosmicQueryPlanner.Plan(missingQuery, ProviderType.AzureStorageOASIS,
                new[] { new CosmicQueryTarget(ProviderType.AzureStorageOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(missingPlan.IsError, missingPlan.Message);
            var absent = await provider.ExecuteCosmicQueryAsync(missingPlan.Result);
            Assert.IsFalse(absent.IsError, absent.Exception?.ToString() ?? absent.Message);
            Assert.AreEqual(0, absent.Result.Count);
            CollectionAssert.AreEqual(before, await Snapshot(), "CQL reads must preserve exact native ETags/content.");
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
