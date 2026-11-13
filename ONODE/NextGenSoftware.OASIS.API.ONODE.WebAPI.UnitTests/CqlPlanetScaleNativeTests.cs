using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class PlanetScaleNativeFactAttribute : FactAttribute
{
    public PlanetScaleNativeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_PLANETSCALE_HOSTED_CQL_SOURCE") is null)
            Skip = "Requires an explicitly reviewed PlanetScale teaching source.";
    }
}

public sealed class CqlPlanetScaleNativeTests
{
    private sealed record Source(Guid Actor, Guid[] LiveIds, Guid DeletedId, Guid MissingId, string ConnectionSha256);

    [PlanetScaleNativeFact]
    public async Task ReviewedSourceReadsExactPublicRowsThroughWeb4Registration()
    {
        const ProviderType authority = ProviderType.PlanetScaleOASIS;
        var (source, reader) = CreateReviewedSource();
        var ids = source.LiveIds.Append(source.DeletedId).Append(source.MissingId).ToArray();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var active = await reader.ActivateQuerySourceAsync(deadline.Token);
        Assert.False(active.IsError, active.Message);
        foreach (var selection in new[] { ids, source.LiveIds[..2], ids[3..] })
        {
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(source.Actor, authority, ids).Result).Result
                .Where(row => selection.Contains(row.Id)).Result;
            var plan = CosmicQueryPlanner.Plan(query, authority, new[] { new CosmicQueryTarget(authority, reader.QueryCapabilities) });
            Assert.False(plan.IsError, plan.Message);
            var result = await reader.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: deadline.Token);
            Assert.False(result.IsError, result.Message);
            Assert.All(result.Result, row => Assert.True(row.IsPublic && !row.IsDeleted && row.CreatedByAvatarId == source.Actor));
            Assert.Equal(selection.Intersect(source.LiveIds).Order(), result.Result.Select(row => row.Id).Order());
        }
    }

    [ReviewedPublicMongoNativeFact("COSMIC_PLANETSCALE_HOSTED_CQL_SOURCE")]
    public Task ReviewedSourceAndPrivateMongoCopiesOverHttp() =>
        ReviewedPublicMongoHttpFixture.VerifyHttpAsync(ProviderType.PlanetScaleOASIS, _ =>
        {
            var (source, reader) = CreateReviewedSource();
            return (reader, source.LiveIds);
        });

    private static (Source Catalogue, NextGenSoftware.OASIS.API.Providers.PlanetScaleOASIS.PlanetScaleQuerySource Reader)
        CreateReviewedSource()
    {
        const string connectionKey = "CQL_PLANETSCALE_QUERY_CONNECTION_STRING";
        const string probeKey = "CQL_PLANETSCALE_QUERY_PROBE_ID";
        var path = Environment.GetEnvironmentVariable("COSMIC_PLANETSCALE_HOSTED_CQL_SOURCE")!;
        var connection = Environment.GetEnvironmentVariable(connectionKey);
        var probe = Environment.GetEnvironmentVariable(probeKey);
        Assert.False(string.IsNullOrWhiteSpace(connection));
        Assert.InRange(new FileInfo(path).Length, 1L, 16384L);
        var source = JsonSerializer.Deserialize<Source>(System.IO.File.ReadAllBytes(path));
        Assert.NotNull(source);
        Assert.NotEqual(Guid.Empty, source.Actor);
        Assert.Equal(3, source.LiveIds.Length);
        var ids = source.LiveIds.Append(source.DeletedId).Append(source.MissingId).ToArray();
        Assert.Equal(5, ids.Distinct().Count());
        Assert.DoesNotContain(Guid.Empty, ids);
        Assert.Equal(source.LiveIds[0].ToString("D"), probe);
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(connection!))).ToLowerInvariant();
        Assert.True(source.ConnectionSha256 == digest, "Connection differs from the reviewed descriptor.");

        var services = new ServiceCollection();
        CqlPlanetScaleReadSourceRegistration.AddPlanetScaleReadSource(services,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                { [connectionKey] = connection, [probeKey] = probe }).Build());
        using var container = services.BuildServiceProvider();
        var reader = Assert.IsType<NextGenSoftware.OASIS.API.Providers.PlanetScaleOASIS.PlanetScaleQuerySource>(
            container.GetRequiredService<CqlReadSourceProfile>().Storage);
        return (source, reader);
    }
}
