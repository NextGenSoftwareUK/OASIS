using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.IntegrationTests;

[TestClass]
public class RedisFamilyHostedCqlNativeTests
{
    private sealed record Source(Guid Actor, Guid[] LiveIds, Guid DeletedId, Guid MissingId);

    [TestMethod]
    [TestCategory("RedisHostedCqlNative")]
    public Task RedisRestrictedReaderUsesWeb4Registration() => ReadCatalogue("REDIS", ProviderType.RedisOASIS);

    [TestMethod]
    [TestCategory("ValkeyHostedCqlNative")]
    public Task ValkeyRestrictedReaderUsesWeb4Registration() => ReadCatalogue("VALKEY", ProviderType.ValKeyOASIS);

    [TestMethod]
    [TestCategory("GarnetHostedCqlNative")]
    public Task GarnetRestrictedReaderUsesWeb4Registration() => ReadCatalogue("GARNET", ProviderType.GarnetOASIS);

    [TestMethod]
    [TestCategory("KeyDbHostedCqlNative")]
    public Task KeyDbRestrictedReaderUsesWeb4Registration() => ReadCatalogue("KEYDB", ProviderType.KeyDBOASIS);

    [TestMethod]
    [TestCategory("DragonflyHostedCqlNative")]
    public Task DragonflyRestrictedReaderUsesWeb4Registration() => ReadCatalogue("DRAGONFLY", ProviderType.DragonflyOASIS);

    private static async Task ReadCatalogue(string product, ProviderType authority)
    {
        var path = Environment.GetEnvironmentVariable($"COSMIC_{product}_HOSTED_CQL_SOURCE")
            ?? throw new InvalidOperationException("An explicit hosted teaching catalogue is required.");
        Assert.IsTrue(new FileInfo(path).Length is > 0 and <= 16384);
        var source = JsonSerializer.Deserialize<Source>(await File.ReadAllBytesAsync(path))!;
        Assert.IsNotNull(source);
        Assert.AreNotEqual(Guid.Empty, source.Actor);
        Assert.AreEqual(3, source.LiveIds.Length);
        var ids = source.LiveIds.Append(source.DeletedId).Append(source.MissingId).ToArray();
        Assert.AreEqual(5, ids.Distinct().Count());
        Assert.IsFalse(ids.Contains(Guid.Empty));

        var prefix = $"CQL_{product}_QUERY_";
        var settings = new[] { "ADDRESS", "PORT", "TLS_HOST", "USERNAME", "PASSWORD", "STORAGE_KEY", "PROBE_ID" }
            .ToDictionary(name => prefix + name, name => Environment.GetEnvironmentVariable(prefix + name));
        Assert.IsTrue(Guid.TryParseExact(settings[prefix + "PROBE_ID"], "D", out var probe) && source.LiveIds.Contains(probe));
        var services = new ServiceCollection();
        services.AddRedisFamilyReadSources(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        using var container = services.BuildServiceProvider();
        var provider = (RedisOASIS)container.GetRequiredService<CqlReadSourceProfile>().Storage;
        Assert.AreEqual(authority, provider.QueryProviderType);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var active = await provider.ActivateQuerySourceAsync(deadline.Token);
        Assert.IsFalse(active.IsError, active.Message);

        CosmicQuery<Holon> Query() => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(source.Actor, authority, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        async Task<IHolon[]> Read(CosmicQuery<Holon> query)
        {
            var plan = CosmicQueryPlanner.Plan(query, authority, new[] { new CosmicQueryTarget(authority, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: deadline.Token);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsTrue(result.Result.All(row => row.CreatedByAvatarId == source.Actor && row.IsPublic &&
                !row.IsDeleted && !row.Children.Any()));
            return result.Result.ToArray();
        }

        static byte[] Snapshot(IHolon[] rows) => JsonSerializer.SerializeToUtf8Bytes(rows.Select(row => new
        {
            row.Id, row.Name, row.Description, row.Version, Metadata = row.MetaData.OrderBy(entry => entry.Key, StringComparer.Ordinal)
        }));
        var before = await Read(Query());
        var ordered = source.LiveIds.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(ordered, before.Select(row => row.Id).ToArray());
        var subset = new[] { ordered[0], ordered[2] };
        CollectionAssert.AreEqual(subset, (await Read(Query().Where(row => subset.Contains(row.Id)).Result)).Select(row => row.Id).ToArray());
        CollectionAssert.AreEqual(new[] { ordered[1] }, (await Read(Query().Skip(1).Result.Take(1).Result)).Select(row => row.Id).ToArray());
        var excluded = new[] { source.DeletedId, source.MissingId };
        Assert.AreEqual(0, (await Read(Query().Where(row => excluded.Contains(row.Id)).Result)).Length);
        CollectionAssert.AreEqual(Snapshot(before), Snapshot(await Read(Query())));
    }
}
