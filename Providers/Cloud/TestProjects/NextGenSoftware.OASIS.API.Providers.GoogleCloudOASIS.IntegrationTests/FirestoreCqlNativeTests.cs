using System.Text.Json;
using Google.Api.Gax;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class FirestoreCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    [TestMethod]
    public async Task NativeFirestoreFiltersThreeRecordsToTwoAndDoesNotWriteDuringQueries()
    {
        Assert.AreEqual("127.0.0.1:18084", Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var state = Environment.GetEnvironmentVariable("OASIS_FIRESTORE_CQL_STATE")
            ?? throw new InvalidOperationException("Use the retained Firestore CQL runner.");
        var seedPath = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Reconcile the original pending fixture; never replay its writes.");
        var native = new FirestoreDbBuilder { ProjectId = "oasis-cql-demo",
            EmulatorDetection = EmulatorDetection.EmulatorOnly }.Build();
        var provider = new GoogleCloudOASIS(projectId: "oasis-cql-demo",
            enableStorage: false, enableFirestore: true, enableBigQuery: false);
        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        try
        {
            Seed seed;
            if (!File.Exists(seedPath))
            {
                Assert.AreEqual(0, (await native.Collection("holons").Limit(1).GetSnapshotAsync()).Count,
                    "A fresh owned project must be empty; do not adopt unexplained native data.");
                seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray());
                using (var journal = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { JsonSerializer.Serialize(journal, seed); journal.Flush(true); }
                var names = new[] { "Espresso", "Latte", "Tea" };
                for (var index = 0; index < seed.Ids.Length; index++)
                {
                    var saved = await provider.SaveHolonAsync(new Holon { Id = seed.Ids[index], Name = names[index],
                        CreatedByAvatarId = seed.Actor, IsNewHolon = true, IsActive = true }, saveChildren: false);
                    Assert.IsFalse(saved.IsError, saved.Exception?.ToString() ?? saved.Message);
                    Assert.IsTrue(saved.IsSaved);
                }
                File.Move(pending, seedPath);
            }
            else seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(seedPath))!;

            Assert.AreEqual(3, seed.Ids.Distinct().Count());
            var references = seed.Ids.Select(id => native.Collection("holons").Document(id.ToString("D"))).ToArray();
            var before = await native.GetAllSnapshotsAsync(references);
            Assert.IsTrue(before.All(row => row.Exists));
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.GoogleCloudOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.GoogleCloudOASIS, seed.Ids).Result).Result;
            async Task<IReadOnlyList<IHolon>> Read(CosmicQuery<Holon> selection)
            {
                var plan = CosmicQueryPlanner.Plan(selection, ProviderType.GoogleCloudOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.GoogleCloudOASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
                return result.Result;
            }
            var all = await Read(query);
            CollectionAssert.AreEquivalent(new[] { "Espresso", "Latte", "Tea" }, all.Select(row => row.Name).ToArray());
            var coffee = seed.Ids.Take(2).ToArray();
            var filtered = await Read(query.Where(row => coffee.Contains(row.Id)).Result);
            CollectionAssert.AreEquivalent(new[] { "Espresso", "Latte" }, filtered.Select(row => row.Name).ToArray());
            var missing = Guid.NewGuid();
            var absent = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.GoogleCloudOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.GoogleCloudOASIS, new[] { missing }).Result).Result;
            Assert.AreEqual(0, (await Read(absent)).Count, "Explicit native absence is not a source failure.");
            var after = await native.GetAllSnapshotsAsync(references);
            CollectionAssert.AreEqual(before.Select(row => row.UpdateTime).ToArray(), after.Select(row => row.UpdateTime).ToArray(),
                "Native query must not change any seeded document revision.");
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
