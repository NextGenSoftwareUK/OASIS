using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.IntegrationTests;

public class HoloOASISIntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HoloCoreStorageConformance_PersistsOnOfficialHolochainConductor(bool sync)
    {
        var adminUri = Environment.GetEnvironmentVariable("OASIS_HOLOCHAIN_ADMIN_URI")
            ?? throw new InvalidOperationException("Configure the dedicated conductor admin endpoint.");
        var results = Environment.GetEnvironmentVariable("OASIS_HOLOCHAIN_REPORT_DIR")
            ?? throw new InvalidOperationException("Configure the disposable conductor report directory.");
        var reportPath = Path.Combine(results, sync ? "holo-sync.json" : "holo-async.json");
        Assert.False(File.Exists(reportPath), "Use a fresh report directory for each conductor run.");

        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(report));
            File.Move(reportPath + ".next", reportPath, overwrite: true);
        }

        var provider = new HoloOASIS(adminUri, useHoloNetwork: false);
        var report = await CoreStorageConformance.RunAsync(provider, "HoloOASIS", "native", sync, Checkpoint);
        Assert.True(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} [{report.FailureContract}]");
        Assert.Equal(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
        Assert.True(report.FixtureCleanupComplete);
    }

    [Fact]
    public async Task AvatarAndHolonCrud_PersistOnOfficialHolochainConductor()
    {
        string adminUri = Environment.GetEnvironmentVariable("OASIS_HOLOCHAIN_ADMIN_URI")
            ?? throw new InvalidOperationException("Set OASIS_HOLOCHAIN_ADMIN_URI to run the official conductor test.");
        await VerifyCrudAsync(new HoloOASIS(adminUri, useHoloNetwork: false));
    }

    [Fact]
    public async Task DesktopAvatarAndHolonCrud_PersistOnOfficialHolochainConductor()
    {
        string adminUri = Environment.GetEnvironmentVariable("OASIS_HOLOCHAIN_ADMIN_URI")
            ?? throw new InvalidOperationException("Configure the dedicated conductor admin endpoint.");
        var provider = new global::NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop.HoloOASIS(adminUri);
        try
        {
            await VerifyCrudAsync(provider);
        }
        finally
        {
            if (provider.IsProviderActivated)
            {
                var result = await provider.DeActivateProviderAsync();
                Assert.False(result.IsError, result.Message);
                Assert.True(result.Result);
            }
        }
    }

    private static async Task VerifyCrudAsync(HoloOASIS provider)
    {
        var activation = await provider.ActivateProviderAsync();
        Assert.False(activation.IsError, activation.Message);
        Assert.True(activation.Result);

        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = $"holo-{Guid.NewGuid():N}",
            Email = $"holo-{Guid.NewGuid():N}@oasis.test",
            FirstName = "Before"
        };
        var createdAvatar = await provider.SaveAvatarAsync(avatar);
        Assert.False(createdAvatar.IsError, createdAvatar.Message);
        var loadedAvatar = await provider.LoadAvatarAsync(avatar.Id);
        Assert.False(loadedAvatar.IsError, loadedAvatar.Message);
        Assert.Equal("Before", loadedAvatar.Result.FirstName);
        avatar.FirstName = "After";
        var updatedAvatar = await provider.SaveAvatarAsync(avatar);
        Assert.False(updatedAvatar.IsError, updatedAvatar.Message);
        loadedAvatar = await provider.LoadAvatarAsync(avatar.Id);
        Assert.False(loadedAvatar.IsError, loadedAvatar.Message);
        Assert.Equal("After", loadedAvatar.Result.FirstName);

        var holon = new Holon { Id = Guid.NewGuid(), Name = "Before" };
        var createdHolon = await provider.SaveHolonAsync(holon);
        Assert.False(createdHolon.IsError, createdHolon.Message);
        var loadedHolon = await provider.LoadHolonAsync(holon.Id);
        Assert.False(loadedHolon.IsError, loadedHolon.Message);
        Assert.Equal("Before", loadedHolon.Result.Name);
        holon.Name = "After";
        var updatedHolon = await provider.SaveHolonAsync(holon);
        Assert.False(updatedHolon.IsError, updatedHolon.Message);
        loadedHolon = await provider.LoadHolonAsync(holon.Id);
        Assert.False(loadedHolon.IsError, loadedHolon.Message);
        Assert.Equal("After", loadedHolon.Result.Name);

        var deletedAvatar = await provider.DeleteAvatarAsync(avatar.Id, false);
        Assert.False(deletedAvatar.IsError, deletedAvatar.Message);
        var deletedHolon = await provider.DeleteHolonAsync(holon.Id);
        Assert.False(deletedHolon.IsError, deletedHolon.Message);
    }
}
