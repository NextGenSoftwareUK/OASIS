using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests;

[TestClass]
[DoNotParallelize]
public class TonRetainedCqlTests
{
    [TestMethod]
    public async Task RetainedPrivateSourceRunsNativeQueriesWithoutReseeding()
    {
        if (Environment.GetEnvironmentVariable("OASIS_TON_CQL_MODE") != "owned-local-tvm-demo")
            Assert.Inconclusive("Explicit retained local TVM runner required.");
        using var ownership = new FileStream(Path.Combine(TonRetainedDemoSource.State, "demo-source.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var key = PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_TON_CQL_STORAGE_KEY");
        try
        {
            var seed = await TonRetainedDemoSource.OpenOrCreateAsync(key);
            var before = TonRetainedDemoSource.Capture();
            using var provider = new TONOASIS(TonRetainedDemoSource.Options(seed, true), () => (byte[])key.Clone());
            var active = await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message + active.Exception);
            try
            {
                foreach (var selected in new[] { seed.Ids, seed.Ids.Take(2).ToArray(), new[] { Guid.NewGuid() } })
                {
                    var access = CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.TONOASIS, seed.Ids.Concat(selected).Distinct());
                    Assert.IsFalse(access.IsError, access.Message);
                    var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TONOASIS).Result)
                        .WithReadAccess(access.Result).Result.Where(row => selected.Contains(row.Id)).Result;
                    var plan = CosmicQueryPlanner.Plan(query, ProviderType.TONOASIS,
                        new[] { new CosmicQueryTarget(ProviderType.TONOASIS, provider.QueryCapabilities) });
                    Assert.IsFalse(plan.IsError, plan.Message);
                    var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
                    Assert.IsFalse(result.IsError, result.Message + result.Exception);
                    CollectionAssert.AreEquivalent(seed.Ids.Intersect(selected).ToArray(), result.Result.Select(row => row.Id).ToArray());
                }
            }
            finally { await provider.DeActivateProviderAsync(); }
            TonRetainedDemoSource.Unchanged(before);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
