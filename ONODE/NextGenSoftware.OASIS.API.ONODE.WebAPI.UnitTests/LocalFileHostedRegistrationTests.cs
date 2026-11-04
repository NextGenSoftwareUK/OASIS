using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class LocalFileHostedRegistrationTests
{
    private static readonly Guid[] Ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
    private static string SourceJson() => JsonSerializer.Serialize(new { schema = 1,
        directory = Path.Combine(AppContext.BaseDirectory, "no-native-source-created"), sourceOwner = Guid.NewGuid(),
        records = Ids.Select(id => new { id, sha256 = new string('1', 64) }).ToArray() });

    [Fact]
    public void MissingConfigurationDoesNotRegisterOrCreateAReadSource()
    {
        var services = new ServiceCollection();
        LocalFileCqlRegistration.AddConfigured(services, new ConfigurationBuilder().Build(), sandboxEnabled: false);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(CqlReadSourceProfile));
    }

    [Fact]
    public void ExplicitConfigurationRegistersQueryProfileWithoutActivationOrImplicitReadGrant()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            [LocalFileCqlRegistration.ConfigurationKey] = SourceJson() }).Build();
        LocalFileCqlRegistration.AddConfigured(services, configuration, sandboxEnabled: false);
        using var provider = services.BuildServiceProvider();
        var registration = Assert.Single(provider.GetServices<CqlReadSourceProfile>());
        var source = Assert.IsType<LocalFilePublicCqlProfile>(registration.Storage);
        Assert.False(source.IsProviderActivated);
        Assert.False(Directory.Exists(source.SourceDirectory));
        Assert.Empty(provider.GetServices<CqlPublicReadCatalogue>());
        Assert.Equal(ProviderType.LocalFileOASIS, CqlV1Compiler.RequestedProvider("LocalFileOASIS", true));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.RequestedProvider("LocalFileOASIS"));
    }

    [Fact]
    public void HttpRowsCompileUnsortedGuidSelectionAgainstActualFileCapabilities()
    {
        using var source = LocalFilePublicCqlProfile.FromJson(SourceJson());
        var actor = Guid.NewGuid();
        var request = JsonSerializer.Deserialize<CqlV1Request>(JsonSerializer.Serialize(new {
            orderBy = "None", take = 3, filters = new[] {
                new { field = "Id", scalar = "Guid", @operator = "In", value = Ids } }
        }), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var access = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.LocalFileOASIS, Ids).Result;
        var query = CqlV1Compiler.Compile(request, actor, ProviderType.LocalFileOASIS, access);
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.LocalFileOASIS,
            new[] { new CosmicQueryTarget(ProviderType.LocalFileOASIS, source.QueryCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        Assert.Equal(3, LocalFileCqlCompiler.Compile(query, ProviderType.LocalFileOASIS).Result.Count);
        Assert.False(source.IsProviderActivated);
    }
}
