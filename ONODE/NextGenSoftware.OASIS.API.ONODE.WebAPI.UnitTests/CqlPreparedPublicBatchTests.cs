using System.Text.Json;
using Microsoft.Extensions.Configuration;
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
        => await CheckPreparedBatch(7);

    [PublicBatchReviewFact]
    public async Task ActualTenSourceCandidatePlansAllFortyPresetsWithShippingCapabilities()
        => await CheckPreparedBatch(10);

    [PublicBatchReviewFact]
    public async Task ActualOptimismCandidatePlansFourPresetsWithShippingCapabilities()
        => await CheckPreparedBatch(1);

    [PublicBatchReviewFact]
    public async Task ActualLineaScrollCandidatePlansEightPresetsWithShippingCapabilities()
        => await CheckPreparedBatch(2);

    [PublicBatchReviewFact]
    public async Task ActualZkSyncAbstractCandidatePlansEightPresetsWithShippingCapabilities()
        => await CheckPreparedBatch(2);

    [PublicBatchReviewFact]
    public async Task ActualMonadCandidatePlansFourPresetsWithShippingCapabilities()
        => await CheckPreparedBatch(1);

    [PublicBatchReviewFact]
    public async Task ActualSeiCandidatePlansFourPresetsWithShippingCapabilities()
        => await CheckPreparedBatch(1);

    // Validate the complete merged publisher output, including cross-provider
    // resolution against the same shipping Mongo capabilities used by the API.
    [PublicBatchReviewFact]
    public async Task ActualSeventeenSourceCandidatePlansAllSixtyEightPresetsWithShippingCapabilities()
        => await CheckPreparedBatch(17);

    [PublicBatchReviewFact]
    public async Task ActualBerachainCandidatePlansFourPresetsWithShippingCapabilities()
        => await CheckPreparedBatch(1);

    [PublicBatchReviewFact]
    public async Task ActualBitcoinCandidatePlansFourPresetsWithShippingCapabilities()
        => await CheckPreparedBatch(1);

    [PublicBatchReviewFact]
    public async Task ActualXrplCandidatePlansFourPresetsWithShippingCapabilities()
        => await CheckPreparedBatch(1);

    [PublicBatchReviewFact]
    public async Task ActualStellarCandidatePlansFourPresetsWithShippingCapabilities()
        => await CheckPreparedBatch(1);

    [PublicBatchReviewFact]
    public async Task ActualAlgorandCandidatePlansFourPresetsWithShippingCapabilities()
        => await CheckPreparedBatch(1);

    private static async Task CheckPreparedBatch(int expected)
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_CQL_PUBLIC_REVIEW")!;
        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        Assert.InRange(bytes.Length, 1, 262144);
        using var json = JsonDocument.Parse(bytes);
        Assert.Equal(new[] { "examples", "profiles" }, json.RootElement.EnumerateObject().Select(value => value.Name).Order().ToArray());
        var profiles = json.RootElement.GetProperty("profiles");
        Assert.Equal(expected, profiles.EnumerateObject().Count());
        var exampleJson = json.RootElement.GetProperty("examples");
        // Exercise the shipping per-provider configuration layout used by hosted
        // deployment variables, rather than only the unpartitioned JSON parser.
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            exampleJson.EnumerateObject().Select(entry => new KeyValuePair<string, string?>(
                CqlHostedQueryExamples.ProviderConfigurationSection + ":" + entry.Name,
                entry.Value.GetRawText()))).Build();
        var examples = CqlHostedQueryExamples.FromConfiguration(configuration);
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
                    ProviderType.BaseOASIS => Providers.BaseOASIS.BasePublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.AvalancheOASIS => Providers.AvalancheOASIS.AvalanchePublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.BNBChainOASIS => Providers.BNBChainOASIS.BnbPublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.OptimismOASIS => Providers.OptimismOASIS.OptimismPublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.LineaOASIS => Providers.LineaOASIS.LineaPublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.ScrollOASIS => Providers.ScrollOASIS.ScrollPublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.ZkSyncOASIS => Providers.ZkSyncOASIS.ZkSyncPublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.AbstractOASIS => Providers.AbstractOASIS.AbstractPublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.MonadOASIS => Providers.MonadOASIS.MonadPublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.StellarOASIS => Providers.StellarOASIS.StellarPublicHeaderMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.AlgorandOASIS => NextGenSoftware.OASIS.API.Providers.AlgorandOASIS.AlgorandPublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.XRPLOASIS => Providers.XRPLOASIS.XrplPublicHeaderMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.BitcoinOASIS => Providers.BitcoinOASIS.BitcoinPublicHeaderMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.BerachainOASIS => Providers.BerachainOASIS.BerachainPublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
                    ProviderType.SeiOASIS => Providers.SeiOASIS.SeiPublicAccountMetadataSource.FromJson(profile.Value.GetRawText()),
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
            Assert.Equal(expected * 4, total);
        }
        finally { foreach (var disposable in disposables) disposable.Dispose(); }
    }
}
