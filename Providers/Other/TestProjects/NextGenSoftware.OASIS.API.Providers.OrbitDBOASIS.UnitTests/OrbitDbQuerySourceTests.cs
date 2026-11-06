using System;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.OrbitDBOASIS.UnitTests;

[TestClass]
public class OrbitDbQuerySourceTests
{
    [DataTestMethod]
    [DataRow("http://127.0.0.1:54869/", true)]
    [DataRow("http://127.1:54869/", false)]
    [DataRow("http://localhost:54869/", false)]
    [DataRow("http://127.0.0.1:54869/private", false)]
    [DataRow("http://127.0.0.1:54869/?source=foreign", false)]
    [DataRow("http://remote.example:54869/", false)]
    public void AcceptsOnlyTheExactLoopbackOrigin(string endpoint, bool expected)
    {
        Assert.AreEqual(expected, OrbitDbQuerySource.IsValidConfiguration(new Uri(endpoint), new string('a', 32), Guid.NewGuid()));
    }

    [TestMethod, TestCategory("OrbitDbCqlNative")]
    public async Task BoundedQueriesUseTheNativeSidecarAfterRestart()
    {
        var descriptor = Environment.GetEnvironmentVariable("COSMIC_ORBITDB_CQL_FIXTURE");
        var reader = Environment.GetEnvironmentVariable("OASIS_ORBITDB_CQL_READER_TOKEN");
        if (descriptor == null && reader == null)
        {
            Assert.Inconclusive("Start the isolated native OrbitDB fixture.");
            return;
        }
        Assert.IsFalse(string.IsNullOrWhiteSpace(descriptor) || string.IsNullOrWhiteSpace(reader));
        using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(descriptor!));
        var actor = document.RootElement.GetProperty("actor").GetGuid();
        var ids = document.RootElement.GetProperty("ids").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        Assert.AreEqual(6, ids.Length);
        var live = ids[..3];
        var inactive = ids[4..];
        var readable = live.Concat(inactive).ToArray();
        using var source = new OrbitDbQuerySource(new Uri("http://127.0.0.1:54869/"), reader!, live[0]);
        var active = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(active.IsError, active.Message);
        await QueryAsync(readable, live);
        await QueryAsync(live[..2], live[..2]);
        await QueryAsync(inactive, Array.Empty<Guid>());
        await QueryAsync(new[] { live[0], ids[3] }, expected: null);

        async Task QueryAsync(Guid[] selected, Guid[]? expected)
        {
            const ProviderType provider = ProviderType.OrbitDBOASIS;
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, provider, ids).Result).Result
                .Where(row => selected.Contains(row.Id)).Result;
            var plan = CosmicQueryPlanner.Plan(query, provider, new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result);
            if (expected == null)
            {
                Assert.IsTrue(result.IsError);
                Assert.IsNull(result.Result);
                return;
            }
            Assert.IsFalse(result.IsError, result.Message);
            CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
            Assert.IsTrue(result.Result.All(row => row.IsPublic && row.CreatedByAvatarId == actor &&
                row.Name == $"teaching-{row.Id:D}" && !row.Children.Any()));
        }
    }
}
