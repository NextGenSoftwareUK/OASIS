using Microsoft.Extensions.Configuration;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using IoFile = System.IO.File;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class HoloOasisLinuxConductorFactAttribute : FactAttribute
{
    public HoloOasisLinuxConductorFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("OASIS_CQL_HOLOOASIS_LINUX") != "1")
            Skip = "Requires the dedicated Linux conductor CI fixture.";
    }
}

public sealed class CqlHoloOasisDevelopmentConductorTests
{
    [Fact]
    public void LeavesTheConductorDisabledWithoutExplicitAdmission()
    {
        Assert.Null(HoloOasisDevelopmentConductor.CreateIfEnabled(new ConfigurationBuilder().Build()));
    }

    [HoloOasisLinuxConductorFact]
    public async Task StartsAnOfficialLoopbackConductorAndReleasesTheStateLease()
    {
        Assert.True(OperatingSystem.IsLinux());
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RAILWAY_ENVIRONMENT_NAME"] = "Development",
            ["CQL_HOLOOASIS_LOCAL_DEMO:Enabled"] = "true"
        }).Build();
        var hosted = Assert.IsType<HoloOasisDevelopmentConductor>(
            HoloOasisDevelopmentConductor.CreateIfEnabled(configuration));
        await hosted.StartAsync(default);
        try
        {
            const string endpoint = "ws://127.0.0.1:65464";
            var inspector = new HoloNETClientAdmin(new HoloNETDNA
            {
                HolochainConductorAdminURI = endpoint,
                AutoStartHolochainConductor = false,
                AutoShutdownHolochainConductor = false,
                LogToConsole = false,
                LogToFile = false
            });
            try
            {
                var connection = await inspector.ConnectAsync(endpoint);
                Assert.True(connection.IsConnected && !connection.IsError);
                var listed = await inspector.ListAppsAsync(AppStatusFilter.All);
                Assert.False(listed.IsError);
            }
            finally
            {
                await inspector.DisconnectAsync();
            }
        }
        finally
        {
            await hosted.StopAsync();
        }

        using var lease = IoFile.Open("/data/COSMIC/HoloOasisCql/web4.lock",
            FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }
}
