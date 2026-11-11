using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.IntegrationTests;

public sealed class IPFSConcurrentPublicationTests
{
    [Fact]
    public Task ConcurrentAcknowledgedSavesRemainVisibleFromPersistedCatalogue()
        => VerifyPublicationAsync(independentWriters: false);

    [Fact]
    public Task IndependentWritersPreserveEveryAcknowledgedRecord()
        => VerifyPublicationAsync(independentWriters: true);

    private static async Task VerifyPublicationAsync(bool independentWriters)
    {
        var endpoint = Environment.GetEnvironmentVariable("IPFSOASIS_TEST_API");
        var path = Environment.GetEnvironmentVariable("IPFSOASIS_CONCURRENT_DNA");
        Assert.False(string.IsNullOrWhiteSpace(endpoint));
        Assert.False(string.IsNullOrWhiteSpace(path));
        var dnaPath = independentWriters ? path + ".independent" : path;
        Assert.False(File.Exists(dnaPath));

        OASISDNA NewDna()
        {
            var dna = new OASISDNA();
            dna.OASIS.StorageProviders = new StorageProviderSettings
            {
                IPFSOASIS = new IPFSOASISSettings { ConnectionString = endpoint }
            };
            return dna;
        }
        var holons = Enumerable.Range(0, 8).Select(index => new Holon
        {
            Id = Guid.NewGuid(),
            Version = 1,
            HolonType = HolonType.Holon,
            Name = $"concurrent holon {index}"
        }).ToArray();

        var writer = new IPFSOASIS(NewDna(), dnaPath);
        var secondWriter = independentWriters ? new IPFSOASIS(NewDna(), dnaPath) : writer;
        try
        {
            var active = await writer.ActivateProviderAsync();
            Assert.True(active.Result && !active.IsError, active.Message);
            if (independentWriters)
            {
                active = await secondWriter.ActivateProviderAsync();
                Assert.True(active.Result && !active.IsError, active.Message);
                var first = await writer.SaveHolonAsync(holons[0], saveChildren: false);
                Assert.True(first.IsSaved && !first.IsError, first.Message);
            }
            var saves = await Task.WhenAll(holons
                .Skip(independentWriters ? 1 : 0)
                .Select((holon, index) => (index % 2 == 0 ? secondWriter : writer).SaveHolonAsync(holon, saveChildren: false)));
            Assert.All(saves, saved => Assert.True(saved.IsSaved && !saved.IsError, saved.Message));
        }
        finally
        {
            if (independentWriters)
                await secondWriter.DeActivateProviderAsync();
            await writer.DeActivateProviderAsync();
        }

        var persisted = OASISDNAManager.LoadDNA(dnaPath);
        Assert.False(persisted.IsError, persisted.Message);
        var reader = new IPFSOASIS(Assert.IsType<OASISDNA>(persisted.Result), dnaPath);
        try
        {
            var active = await reader.ActivateProviderAsync();
            Assert.True(active.Result && !active.IsError, active.Message);
            var loaded = await reader.LoadAllHolonsAsync(loadChildren: false);
            Assert.True(loaded.IsLoaded && !loaded.IsError, loaded.Message);
            Assert.Equal(holons.Select(holon => holon.Id).Order(), loaded.Result.Select(holon => holon.Id).Order());
        }
        finally
        {
            await reader.DeActivateProviderAsync();
        }
    }
}
