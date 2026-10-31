using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class LocalFileCqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    [TestMethod]
    public async Task NativeFilesFilterThreeToTwoAndReuseExactOriginalSource()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_LOCALFILE_CQL_STATE")
            ?? throw new InvalidOperationException("Use the explicit LocalFile CQL runner.");
        Directory.CreateDirectory(state);
        using var lease = new FileStream(Path.Combine(state, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        var source = Path.Combine(state, "source");
        Assert.IsFalse(File.Exists(pending), "Preserve unfinished setup; never replay/reseed.");
        var provider = new LocalFileOASIS(storageDirectory: source);
        Seed seed;
        if (!File.Exists(manifest))
        {
            Assert.IsFalse(Directory.Exists(source), "Never adopt an existing unowned source.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray());
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            var active = await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message);
            var names = new[] { "Espresso", "Latte", "Tea" };
            for (var i = 0; i < 3; i++)
            {
                var saved = await provider.SaveHolonAsync(new Holon { Id = seed.Ids[i], Name = names[i],
                    CreatedByAvatarId = seed.Actor, IsNewHolon = true }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved);
            }
            File.Move(pending, manifest);
        }
        else
        {
            seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
            Assert.IsTrue(Directory.Exists(Path.Combine(source, "Holons")), "Original source missing; never recreate it.");
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        }
        try
        {
            string[] Snapshot() => Directory.GetFiles(source, "*", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => Path.GetRelativePath(source, p) + "|" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)))).ToArray();
            var before = Snapshot();
            Assert.AreEqual(3, before.Length);
            foreach (var selected in new[] { seed.Ids, seed.Ids.Take(2).ToArray(), new[] { Guid.NewGuid() } })
            {
                var catalogue = selected.Length == 1 ? selected : seed.Ids;
                var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.LocalFileOASIS).Result)
                    .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.LocalFileOASIS, catalogue).Result).Result
                    .Where(h => selected.Contains(h.Id)).Result;
                var plan = CosmicQueryPlanner.Plan(query, ProviderType.LocalFileOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.LocalFileOASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Message + result.Exception);
                CollectionAssert.AreEquivalent(selected.Length == 1 ? Array.Empty<Guid>() : selected,
                    result.Result.Select(h => h.Id).ToArray());
            }
            CollectionAssert.AreEqual(before, Snapshot(), "Native CQL must preserve every source byte/path.");
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
