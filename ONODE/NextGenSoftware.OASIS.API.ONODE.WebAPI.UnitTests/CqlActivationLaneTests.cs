using Microsoft.Extensions.Logging.Abstractions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

/// <summary>A slow source (the HoloOASIS conductor takes minutes on a cold start) must not
/// keep unrelated BootLoader-registered sources NOT_READY on the hosted sandbox.</summary>
public class CqlActivationLaneTests
{
    [Theory]
    [InlineData(ProviderType.HoloOASIS)]
    [InlineData(ProviderType.SolanaOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public void ProductionRegistrySerializesOnlyItsOwnBootLoaderStep(ProviderType provider)
        => Assert.True(new CqlHostedSourceRegistry().HasIndependentActivation(provider));

    [Fact]
    public async Task PendingHoloActivationDoesNotBlockOtherSources()
    {
        var others = new[] { ProviderType.IPFSOASIS, ProviderType.NEAROASIS, ProviderType.SolanaOASIS, ProviderType.TONOASIS };
        var sources = others.ToDictionary(type => type, type =>
        {
            var proxy = System.Reflection.DispatchProxy.Create<CqlHostedSourcesTests.QueryStorage, CqlHostedSourcesTests.Adapter>();
            var state = (CqlHostedSourcesTests.Adapter)(object)proxy;
            state.Type = type; state.Active = true;
            state.Capabilities = CosmicQueryCapabilities.ForStringMetadataKeyLookup("Key", 64, 8);
            return (NextGenSoftware.OASIS.API.Core.Interfaces.IOASISProvider)proxy;
        });
        var holo = new TaskCompletionSource();
        var registry = new HoloPendingRegistry(sources, holo.Task);
        var all = others.Append(ProviderType.HoloOASIS).ToArray();
        var admission = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            Array.Empty<ProviderType>(), all.Select(type => new StorageQueryTariff(type, 0, "demo-v1")), null, all);
        using var hosted = new CqlHostedSources(admission, registry, NullLogger<CqlHostedSources>.Instance);
        await hosted.StartAsync(default);

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (hosted.Describe().Count(row => row.Available) < others.Length && DateTime.UtcNow < deadline)
            await Task.Delay(20);

        var rows = hosted.Describe().ToDictionary(row => row.Provider);
        Assert.All(others, type => Assert.True(rows[type.ToString()].Available, type + " waited behind HoloOASIS"));
        Assert.False(rows["HoloOASIS"].Available);
        Assert.Equal("CQL_SOURCE_NOT_READY", rows["HoloOASIS"].ErrorCode);
        holo.SetCanceled();
        await hosted.StopAsync(default);
    }

    private sealed class HoloPendingRegistry(
        IReadOnlyDictionary<ProviderType, NextGenSoftware.OASIS.API.Core.Interfaces.IOASISProvider> sources,
        Task holo) : ICqlHostedSourceRegistry
    {
        public bool HasIndependentActivation(ProviderType provider) => true;
        public NextGenSoftware.OASIS.API.Core.Interfaces.IOASISProvider Find(ProviderType provider)
            => sources.TryGetValue(provider, out var source) ? source : null!;
        public Task ActivateReadSourceAsync(ProviderType provider, CancellationToken token)
            => provider == ProviderType.HoloOASIS ? holo.WaitAsync(token) : Task.CompletedTask;
    }
}
