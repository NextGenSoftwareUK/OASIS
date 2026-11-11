using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.IntegrationTests;

public sealed class IPFSRestartTests
{
    private static readonly Guid RecordId = new("90644865-2be6-4044-bdfb-3e6a394f5b22");

    [Fact]
    public async Task PersistsCurrentAndHistoricalHolonAcrossDaemonRestart()
    {
        var endpoint = Environment.GetEnvironmentVariable("IPFSOASIS_TEST_API");
        var dnaPath = Environment.GetEnvironmentVariable("IPFSOASIS_RESTART_DNA");
        var phase = Environment.GetEnvironmentVariable("IPFSOASIS_RESTART_PHASE");
        Assert.False(string.IsNullOrWhiteSpace(endpoint));
        Assert.False(string.IsNullOrWhiteSpace(dnaPath));
        Assert.True(phase is "write" or "read", "Select the disposable Kubo restart fixture phase.");

        OASISDNA dna;
        if (phase == "write")
        {
            Assert.False(File.Exists(dnaPath));
            dna = new OASISDNA();
            dna.OASIS.StorageProviders = new StorageProviderSettings
            {
                IPFSOASIS = new IPFSOASISSettings { ConnectionString = endpoint }
            };
        }
        else
        {
            Assert.True(File.Exists(dnaPath));
            var loaded = OASISDNAManager.LoadDNA(dnaPath);
            Assert.False(loaded.IsError, loaded.Message);
            dna = Assert.IsType<OASISDNA>(loaded.Result);
            Assert.Equal(endpoint, dna.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
            Assert.False(string.IsNullOrWhiteSpace(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress));
        }

        var provider = new IPFSOASIS(dna, dnaPath);
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.True(active.Result && !active.IsError, active.Message);
            if (phase == "write")
            {
                var holon = new Holon { Id = RecordId, Name = "before update", Version = 1, HolonType = HolonType.Holon };
                var saved = await provider.SaveHolonAsync(holon, saveChildren: false);
                Assert.True(saved.IsSaved && !saved.IsError, saved.Message);
                holon.Name = "after update";
                holon.Version = 2;
                saved = await provider.SaveHolonAsync(holon, saveChildren: false);
                Assert.True(saved.IsSaved && !saved.IsError, saved.Message);
                Assert.True(File.Exists(dnaPath));
            }
            else
            {
                var current = await provider.LoadHolonAsync(RecordId, loadChildren: false);
                Assert.True(current.IsLoaded && !current.IsError, current.Message);
                Assert.Equal("after update", current.Result.Name);
                Assert.Equal(2, current.Result.Version);
                var history = await provider.LoadHolonAsync(RecordId, loadChildren: false, version: 1);
                Assert.True(history.IsLoaded && !history.IsError, history.Message);
                Assert.Equal("before update", history.Result.Name);
                Assert.Equal(1, history.Result.Version);
            }
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }
}
