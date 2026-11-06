using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Providers.UpstashOASIS.IntegrationTests;

[TestClass]
public class UpstashHostedCqlNativeTests
{
    private sealed record Catalogue(Guid Actor, Guid[] LiveIds, Guid DeletedId, Guid MissingId);

    [TestMethod]
    [TestCategory("UpstashHostedCqlNative")]
    public async Task RestrictedReaderUsesTheWeb4ProfileForPublicTeachingRecords()
    {
        static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value : throw new InvalidOperationException("Explicit native fixture setting required: " + name);
        var path = Required("COSMIC_UPSTASH_HOSTED_CQL_SOURCE");
        Assert.IsTrue(new FileInfo(path).Length is > 0 and <= 16384);
        var original = await File.ReadAllBytesAsync(path);
        var catalogue = JsonSerializer.Deserialize<Catalogue>(original,
            new JsonSerializerOptions { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow })!;
        Assert.IsNotNull(catalogue);
        Assert.AreNotEqual(Guid.Empty, catalogue.Actor);
        Assert.AreEqual(3, catalogue.LiveIds.Length);
        var ids = catalogue.LiveIds.Append(catalogue.DeletedId).Append(catalogue.MissingId).ToArray();
        Assert.AreEqual(5, ids.Distinct().Count());
        Assert.IsFalse(ids.Contains(Guid.Empty));

        var settings = new[] { "ORIGIN", "READER_TOKEN", "PROBE_ID" }
            .ToDictionary(name => "CQL_UPSTASH_QUERY_" + name, name => Required("CQL_UPSTASH_QUERY_" + name));
        Assert.AreEqual(catalogue.LiveIds[0].ToString("D"), settings["CQL_UPSTASH_QUERY_PROBE_ID"]);
        var services = new ServiceCollection();
        services.AddUpstashReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        using var container = services.BuildServiceProvider();
        var provider = (UpstashOASIS)container.GetRequiredService<CqlReadSourceProfile>().Storage;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var active = await provider.ActivateQuerySourceAsync(deadline.Token);
        Assert.IsFalse(active.IsError, active.Message);

        const ProviderType authority = ProviderType.UpstashOASIS;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(catalogue.Actor, authority, ids).Result).Result;
        async Task<IHolon[]> Read(CosmicQuery<Holon> selection)
        {
            var plan = CosmicQueryPlanner.Plan(selection, authority,
                new[] { new CosmicQueryTarget(authority, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: deadline.Token);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsTrue(result.Result.All(row => row.IsPublic && !row.IsDeleted &&
                row.CreatedByAvatarId == catalogue.Actor && !row.Children.Any()));
            return result.Result.ToArray();
        }

        static byte[] Snapshot(IHolon[] rows) => JsonSerializer.SerializeToUtf8Bytes(rows.Select(row =>
            new { row.Id, row.Name, row.Description, row.Version, Metadata = row.MetaData.OrderBy(entry => entry.Key, StringComparer.Ordinal) }));
        var before = await Read(query);
        var ordered = catalogue.LiveIds.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(ordered, before.Select(row => row.Id).ToArray());
        var subset = new[] { ordered[0], ordered[2] };
        CollectionAssert.AreEqual(subset, (await Read(query.Where(row => subset.Contains(row.Id)).Result)).Select(row => row.Id).ToArray());
        CollectionAssert.AreEqual(new[] { ordered[1] }, (await Read(query.Skip(1).Result.Take(1).Result)).Select(row => row.Id).ToArray());
        var excluded = ids[3..];
        Assert.AreEqual(0, (await Read(query.Where(row => excluded.Contains(row.Id)).Result)).Length);
        CollectionAssert.AreEqual(Snapshot(before), Snapshot(await Read(query)));
        CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(path));
    }
}
