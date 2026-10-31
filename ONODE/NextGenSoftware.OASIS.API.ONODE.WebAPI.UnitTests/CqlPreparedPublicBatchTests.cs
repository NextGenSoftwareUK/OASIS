using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class PublicBatchReviewFactAttribute : FactAttribute
{
    public PublicBatchReviewFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COSMIC_CQL_PUBLIC_REVIEW")))
            Skip = "Explicit public-only export of the protected prepared batch required; no hosted mutation.";
    }
}

public sealed class CqlPreparedPublicBatchTests
{
    [PublicBatchReviewFact]
    public async Task ActualSevenSourceCandidatePlansAllTwentyEightPresetsWithShippingCapabilities()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_CQL_PUBLIC_REVIEW")!;
        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        Assert.InRange(bytes.Length, 1, 262144);
        using var json = JsonDocument.Parse(bytes);
        Assert.Equal(new[] { "examples", "profiles" }, json.RootElement.EnumerateObject().Select(value => value.Name).Order().ToArray());
        var profiles = json.RootElement.GetProperty("profiles");
        Assert.Equal(7, profiles.EnumerateObject().Count());
        var examples = CqlHostedQueryExamples.FromJson(json.RootElement.GetProperty("examples").GetRawText());
        var disposables = new List<IDisposable>();
        var sources = new Dictionary<ProviderType, ICosmicQueryStorageProvider>();
        try
        {
            foreach (var profile in profiles.EnumerateObject())
            {
                var type = Enum.Parse<ProviderType>(profile.Name);
                ICosmicQueryStorageProvider source = type switch
                {
                    ProviderType.GoogleCloudOASIS => Providers.GoogleCloudOASIS.GoogleCloudPublicMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.EthereumOASIS => Providers.EthereumOASIS.EthereumPublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.PolygonOASIS => Providers.PolygonOASIS.PolygonPublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.ArbitrumOASIS => Providers.ArbitrumOASIS.ArbitrumPublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.CosmosBlockChainOASIS => Providers.CosmosBlockChainOASIS.CosmosPublicValidatorMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.PolkadotOASIS => Providers.PolkadotOASIS.PolkadotPublicHeaderMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.NEAROASIS => Providers.NEAROASIS.NearPublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    _ => throw new InvalidOperationException("Unexpected source in the explicit reviewed batch.")
                };
                disposables.Add((IDisposable)source);
                Assert.Equal(type, source.QueryProviderType);
                Assert.False(((IOASISStorageProvider)source).IsProviderActivated);
                sources.Add(type, source);
            }
            CosmicQueryCapabilities Resolve(ProviderType type) => type == ProviderType.MongoDBOASIS
                ? Providers.MongoDBOASIS.MongoDBOASIS.CqlCapabilities : sources[type].QueryCapabilities;
            int total = 0;
            foreach (var (type, source) in sources)
            {
                // The actual publisher output, not a second hand-built request set.
                var planned = examples.For(type, source.QueryCapabilities, Resolve);
                Assert.Equal(4, planned.Count);
                Assert.Equal(2, planned.Count(row => row.Operation == "Rows"));
                Assert.Equal(2, planned.Count(row => row.Operation == "Federation"));
                Assert.All(planned, row => Assert.True(row.Available, type + ":" + row.Id + ":" + row.ErrorCode));
                Assert.False(((IOASISStorageProvider)source).IsProviderActivated);
                total += planned.Count;
            }
            Assert.Equal(28, total);
        }
        finally { foreach (var disposable in disposables) disposable.Dispose(); }
    }
}
