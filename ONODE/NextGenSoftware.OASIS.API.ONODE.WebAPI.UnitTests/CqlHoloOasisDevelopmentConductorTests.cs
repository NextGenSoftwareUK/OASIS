using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
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
        var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RAILWAY_ENVIRONMENT_NAME"] = "dev",
            ["RAILWAY_ENVIRONMENT_ID"] = "a31011a2-d047-4057-b5e8-57d52ebcfefe",
            ["RAILWAY_SERVICE_ID"] = "5123199a-43c4-4439-9921-e10e2aec7841",
            ["CQL_HOLOOASIS_LOCAL_DEMO:Enabled"] = "true",
            [CqlPublicReadCatalogue.ConfigurationKey] = JsonSerializer.Serialize(
                new Dictionary<string, Guid[]> { [nameof(ProviderType.HoloOASIS)] = ids })
        }).Build();
        configuration["RAILWAY_ENVIRONMENT_ID"] = Guid.Empty.ToString();
        Assert.Throws<InvalidOperationException>(() => HoloOasisDevelopmentConductor.CreateIfEnabled(configuration));
        configuration["RAILWAY_ENVIRONMENT_ID"] = "a31011a2-d047-4057-b5e8-57d52ebcfefe";
        configuration["RAILWAY_SERVICE_ID"] = Guid.Empty.ToString();
        Assert.Throws<InvalidOperationException>(() => HoloOasisDevelopmentConductor.CreateIfEnabled(configuration));
        configuration["RAILWAY_SERVICE_ID"] = "5123199a-43c4-4439-9921-e10e2aec7841";

        var hosted = Assert.IsType<HoloOasisDevelopmentConductor>(
            HoloOasisDevelopmentConductor.CreateIfEnabled(configuration));
        var managed = Environment.GetEnvironmentVariable("OASIS_CQL_HOLOOASIS_MANAGED_NATIVE") == "1";
        (string Dna, string Agent) originalCell = default;
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

            if (managed)
                originalCell = await VerifyManagedReaderAsync(hosted, configuration, ids);
        }
        finally
        {
            await hosted.StopAsync();
        }

        using (IoFile.Open("/data/COSMIC/HoloOasisCql/web4.lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
        }

        if (managed)
        {
            await hosted.StartAsync(default);
            try
            {
                Assert.Equal(originalCell, await VerifyManagedReaderAsync(hosted, configuration, ids));
            }
            finally
            {
                await hosted.StopAsync();
            }
        }
    }

    private static async Task<(string Dna, string Agent)> VerifyManagedReaderAsync(HoloOasisDevelopmentConductor hosted,
        IConfiguration configuration, Guid[] ids)
    {
        var generated = await hosted.ProvisionReaderAsync(configuration, default);
        var settings = new ConfigurationBuilder().AddConfiguration(configuration)
            .AddInMemoryCollection(generated).Build();
        var services = new ServiceCollection();
        CqlPublicReadCatalogue.Enroll(services, settings);
        CqlHoloOasisReadSourceRegistration.AddHoloOasisReadSource(services, settings);
        var provider = services.BuildServiceProvider();
        var reader = Assert.IsType<HoloOasisReadSource>(
            Assert.Single(provider.GetServices<CqlReadSourceProfile>()).Storage);
        try
        {
            var activation = await reader.ActivateQuerySourceAsync(default);
            Assert.True(activation.Result && !activation.IsError, activation.ErrorCode);
            const ProviderType authority = ProviderType.HoloOASIS;
            var access = Assert.Single(provider.GetServices<CqlPublicReadCatalogue>())
                .Authorize(Guid.NewGuid(), authority);
            Assert.False(access.IsError, access.ErrorCode);
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
                .WithReadAccess(access.Result).Where(row => ids.Contains(row.Id));
            Assert.False(query.IsError, query.ErrorCode);
            var plan = CosmicQueryPlanner.Plan(query.Result, authority, [new(authority, reader.QueryCapabilities)]);
            Assert.False(plan.IsError, plan.ErrorCode);
            var result = await reader.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(result.IsError, result.ErrorCode);
            Assert.Equal(ids.Order(), result.Result.Select(holon => holon.Id).Order());
            return (generated["CQL_HOLOOASIS_QUERY:DnaHash"], generated["CQL_HOLOOASIS_QUERY:AgentKey"]);
        }
        finally
        {
            provider.Dispose();
            Assert.False(reader.IsProviderActivated);
        }
    }
}
