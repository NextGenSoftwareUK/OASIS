using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using BootLoader = NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader;
using CanonicalHoloOasis = NextGenSoftware.OASIS.API.Providers.HoloOASIS.HoloOASIS;
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

        configuration["CQL_HOLOOASIS_LOCAL_DEMO:UseBootLoader"] = "true";
        Assert.Throws<InvalidOperationException>(() => HoloOasisDevelopmentConductor.CreateIfEnabled(configuration));
        configuration["OASIS_DNA_JSON"] = """
            {"OASIS":{"StorageProviders":{"HoloOASIS":{
              "LocalNodeURI":"ws://127.0.0.1:65464",
              "UseLocalNode":true,
              "UseHoloNetwork":false,
              "HoloNETORMUseReflection":true
            }}}}
            """;
        Assert.NotNull(HoloOasisDevelopmentConductor.CreateIfEnabled(configuration));
        configuration["CQL_HOLOOASIS_LOCAL_DEMO:UseBootLoader"] = "false";

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
            await VerifyManagedBootLoaderAsync(configuration, ids);
        }
    }

    private static async Task VerifyManagedBootLoaderAsync(IConfiguration configuration, Guid[] ids)
    {
        configuration["CQL_HOLOOASIS_LOCAL_DEMO:UseBootLoader"] = "true";
        var hosted = Assert.IsType<HoloOasisDevelopmentConductor>(
            HoloOasisDevelopmentConductor.CreateIfEnabled(configuration));
        await hosted.StartAsync(default);
        CanonicalHoloOasis? provider = null;
        try
        {
            Assert.Empty(await hosted.ProvisionAsync(configuration, default));
            var dna = CqlHoloOasisConductorTests.CreateFixtureDna(new Uri("ws://127.0.0.1:65464"));
            var boot = await BootLoader.BootOASISForAdmittedStorageAsync(dna);
            Assert.True(boot.Result && !boot.IsError, boot.Message);
            provider = Assert.IsType<CanonicalHoloOasis>(
                ProviderManager.Instance.GetStorageProvider(ProviderType.HoloOASIS));
            var services = new ServiceCollection();
            CqlPublicReadCatalogue.Enroll(services, configuration);
            using var container = services.BuildServiceProvider();
            var catalogues = container.GetServices<CqlPublicReadCatalogue>().ToArray();
            var registry = new CqlHostedSourceRegistry([], NullLogger<CqlHostedSourceRegistry>.Instance, catalogues);
            await registry.ActivateReadSourceAsync(ProviderType.HoloOASIS, default);
            Assert.Same(provider, registry.Find(ProviderType.HoloOASIS));
            var catalogue = Assert.Single(catalogues);
            await VerifyCatalogueAsync(provider, catalogue, ids);
            await VerifyCatalogueAsync(provider, catalogue, ids[..2]);
        }
        finally
        {
            try
            {
                if (provider?.IsProviderActivated == true)
                {
                    var closed = await provider.DeActivateProviderAsync();
                    Assert.True(closed.Result && !closed.IsError, closed.Message);
                }
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
        var generated = await hosted.ProvisionAsync(configuration, default);
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
            await VerifyCatalogueAsync(reader, Assert.Single(provider.GetServices<CqlPublicReadCatalogue>()), ids);
            return (generated["CQL_HOLOOASIS_QUERY:DnaHash"], generated["CQL_HOLOOASIS_QUERY:AgentKey"]);
        }
        finally
        {
            provider.Dispose();
            Assert.False(reader.IsProviderActivated);
        }
    }

    private static async Task VerifyCatalogueAsync(ICosmicQueryStorageProvider provider, CqlPublicReadCatalogue catalogue, Guid[] ids)
    {
        const ProviderType authority = ProviderType.HoloOASIS;
        var access = catalogue.Authorize(Guid.NewGuid(), authority);
        Assert.False(access.IsError, access.ErrorCode);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(access.Result).Where(row => ids.Contains(row.Id));
        Assert.False(query.IsError, query.ErrorCode);
        var plan = CosmicQueryPlanner.Plan(query.Result, authority, [new(authority, provider.QueryCapabilities)]);
        Assert.False(plan.IsError, plan.ErrorCode);
        var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.False(result.IsError, result.ErrorCode);
        Assert.Equal(ids.Order(), result.Result.Select(holon => holon.Id).Order());
    }
}
