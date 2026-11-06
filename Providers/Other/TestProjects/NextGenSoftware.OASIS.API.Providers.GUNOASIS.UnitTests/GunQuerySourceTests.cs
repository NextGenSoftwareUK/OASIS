using System;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.GUNOASIS.UnitTests;

[TestClass]
public class GunQuerySourceTests
{
    [TestMethod, TestCategory("GunCqlNative")]
    public async Task BoundedQueriesUseTheNativeSidecarAfterRestart()
    {
        var descriptor = Environment.GetEnvironmentVariable("COSMIC_GUN_CQL_FIXTURE");
        var reader = Environment.GetEnvironmentVariable("OASIS_GUN_CQL_READER_TOKEN");
        if (descriptor == null && reader == null)
        {
            Assert.Inconclusive("Start the isolated native GUN fixture.");
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
        var endpoint = new Uri("http://127.0.0.1:54870/");
        using var mismatched = new WrongProviderSource(endpoint, reader!, live[0]);
        var rejected = await mismatched.ActivateQuerySourceAsync();
        Assert.IsTrue(rejected.IsError);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", rejected.ErrorCode);
        using var source = new GunQuerySource(endpoint, reader!, live[0]);
        var active = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(active.IsError, active.Message);
        await QueryAsync(readable, live);
        await QueryAsync(live[..2], live[..2]);
        await QueryAsync(inactive, Array.Empty<Guid>());
        await QueryAsync(new[] { live[0], ids[3] }, expected: null);

        async Task QueryAsync(Guid[] selected, Guid[]? expected)
        {
            const ProviderType provider = ProviderType.GUNOASIS;
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

    private sealed class WrongProviderSource(Uri endpoint, string reader, Guid probe)
        : LoopbackPublicHolonQuerySource(PType.OrbitDBOASIS, endpoint, reader, probe)
    {
    }
}
