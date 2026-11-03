using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class NearCqlNativeTests
{
    private sealed record Catalogue(Guid Actor, Guid[] Ids, string Contract);

    [TestMethod, TestCategory("LocalNative"), Timeout(120000)]
    public async Task RetainedEncryptedSourceSupportsThreeRecordAndTwoRecordNativeQueries()
    {
        var state = Environment.GetEnvironmentVariable("OASIS_NEAR_CQL_STATE")
            ?? throw new InvalidOperationException("Use the explicit retained NEAR CQL runner.");
        var manifest = Path.Combine(state, "native-source.json");
        var provider = new NEAROASIS("http://127.0.0.1:3030", "oasis-near-local", "oasis-near-local", "test.near");
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString() ?? activation.Message);
        try
        {
            Catalogue catalogue;
            if (File.Exists(manifest)) catalogue = JsonSerializer.Deserialize<Catalogue>(await File.ReadAllTextAsync(manifest))!;
            else
            {
                // One-time read-only fixture discovery, separate from the query path.
                // Select only explicitly synthetic retained test records. No signer,
                // source mutation, replacement record or historical replay is allowed.
                var sdk = typeof(NEAROASIS).GetField("_sdk", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(provider)!;
                var entries = await (Task<IReadOnlyList<KeyValuePair<string, string>>>)sdk.GetType()
                    .GetMethod("EntriesAsync")!.Invoke(sdk, new object[] { "holon:" })!;
                var candidates = entries.Select(entry => OasisJson.DeserializeStored<Holon>(entry.Value))
                    .Where(row => row != null && row.Id != Guid.Empty && !row.IsDeleted && row.Name == "TestHolon")
                    .Select(row => row.Id).Distinct().OrderBy(id => id.ToString("D"), StringComparer.Ordinal).Take(3).ToArray();
                Assert.AreEqual(3, candidates.Length, "Retained synthetic source is insufficient; do not manufacture replacements.");
                catalogue = new Catalogue(Guid.NewGuid(), candidates, "test.near");
                using var output = new FileStream(manifest, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(output, catalogue); output.Flush(true);
            }
            Assert.AreEqual("test.near", catalogue.Contract);
            Assert.AreEqual(3, catalogue.Ids.Distinct().Count());
            async Task<string[]> Contents()
            {
                var values = new List<string>();
                foreach (var id in catalogue.Ids)
                {
                    var result = await provider.LoadHolonAsync(id, loadChildren: false);
                    Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
                    Assert.IsNotNull(result.Result); Assert.AreEqual(id, result.Result.Id);
                    Assert.AreEqual("TestHolon", result.Result.Name);
                    values.Add(OasisJson.Serialize(result.Result));
                }
                return values.ToArray();
            }
            var before = await Contents();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.NEAROASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(catalogue.Actor, ProviderType.NEAROASIS, catalogue.Ids).Result).Result;
            async Task<IReadOnlyList<IHolon>> Read(CosmicQuery<Holon> selected)
            {
                var plan = CosmicQueryPlanner.Plan(selected, ProviderType.NEAROASIS,
                    new[] { new CosmicQueryTarget(ProviderType.NEAROASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
                return result.Result;
            }
            CollectionAssert.AreEquivalent(catalogue.Ids, (await Read(query)).Select(row => row.Id).ToArray());
            var subset = catalogue.Ids.Take(2).ToArray();
            CollectionAssert.AreEquivalent(subset, (await Read(query.Where(row => subset.Contains(row.Id)).Result)).Select(row => row.Id).ToArray());
            CollectionAssert.AreEqual(before, await Contents(), "Read-only CQL must preserve source contents and revisions.");
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
