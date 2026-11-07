using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.DuckDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class DuckDbRetainedReadOnlySourceTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids, Guid DeletedId);
    private const string SourceHash = "DB649A3DD1A2FCE1694CBA1F9E303505596A3D3289B01F8166A6D232D3FFCA19";

    [TestMethod]
    public async Task OriginalQualifiedDatabaseSupportsReadOnlyCqlAcrossRestart()
    {
        var directory = Environment.GetEnvironmentVariable("OASIS_DUCKDB_RETAINED_SOURCE_STATE")
            ?? throw new InvalidOperationException("Explicit retained-source qualification directory required.");
        var source = Path.Combine(directory, "source.duckdb");
        var seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(Path.Combine(directory, "demo-seed.json")))!;
        Assert.AreEqual(3, seed.Ids.Length);
        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        Assert.IsFalse(seed.Ids.Contains(seed.DeletedId));
        Assert.AreNotEqual(Guid.Empty, seed.Actor);
        Assert.AreEqual(SourceHash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(source))));
        Assert.IsFalse(File.Exists(source + ".wal"));

        for (var start = 0; start < 2; start++)
        {
            var provider = new DuckDBOASIS("Data Source=" + source + ";access_mode=READ_ONLY");
            var activation = await provider.ActivateQuerySourceAsync(CancellationToken.None);
            Assert.IsFalse(activation.IsError, activation.Message + activation.Exception);
            foreach (var ids in new[] { seed.Ids, seed.Ids.Take(2).ToArray(), new[] { seed.DeletedId } })
            {
                var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.DuckDBOASIS).Result)
                    .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.DuckDBOASIS, ids).Result).Result
                    .Where(holon => ids.Contains(holon.Id)).Result;
                var plan = CosmicQueryPlanner.Plan(query, ProviderType.DuckDBOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.DuckDBOASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Message + result.Exception);
                CollectionAssert.AreEquivalent(ids.Length == 1 ? Array.Empty<Guid>() : ids,
                    result.Result.Select(row => row.Id).ToArray());
            }
            Assert.AreEqual(SourceHash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(source))));
            Assert.IsFalse(File.Exists(source + ".wal"), "Read-only activation must not create a WAL.");
        }
    }
}
