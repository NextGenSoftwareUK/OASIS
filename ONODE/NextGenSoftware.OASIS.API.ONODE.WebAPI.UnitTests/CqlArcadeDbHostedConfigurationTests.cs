using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using ArcadeProvider = NextGenSoftware.OASIS.API.Providers.ArcadeDBOASIS.ArcadeDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlArcadeDbHostedConfigurationTests
{
    private const string Origin = "https://arcade.example.test";
    private const string Id = "11111111-1111-4111-8111-111111111111";

    [Fact]
    public void ExplicitSettingsEnrollAnInactiveReadProfile()
    {
        var services = new ServiceCollection();
        services.AddArcadeDbReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["CQL_ARCADEDB_QUERY_ORIGIN"] = Origin,
            ["CQL_ARCADEDB_QUERY_USERNAME"] = "reader",
            ["CQL_ARCADEDB_QUERY_PASSWORD"] = "restricted",
            ["CQL_ARCADEDB_QUERY_PROBE_ID"] = Id
        }).Build();
        services.AddArcadeDbReadSource(configuration);
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.ArcadeDBOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("sql", "DELETE FROM Holon", Id, ArcadeProvider.CqlExactRecordRoute)]
    [InlineData("sql", ArcadeProvider.CqlExactRecordQuery, "foreign", ArcadeProvider.CqlExactRecordRoute)]
    [InlineData("gremlin", ArcadeProvider.CqlExactRecordQuery, Id, ArcadeProvider.CqlExactRecordRoute)]
    [InlineData("sql", ArcadeProvider.CqlExactRecordQuery, Id, "/api/v1/command/oasis")]
    public async Task RefusesUnexpectedStatementsRoutesAndParameters(string language, string command, string id, string route)
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlArcadeDbReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.PostAsJsonAsync(Origin + route,
            new { language, command, @params = new { id } }));
        Assert.Equal(0, transport.Requests);
    }

    [Fact]
    public async Task ActualProviderActivationUsesTheAdmittedNativeRequest()
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlArcadeDbReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));
        var provider = new ArcadeProvider(Origin, "reader", "restricted", Guid.Parse(Id), client);
        var result = await provider.ActivateQuerySourceAsync(default);
        Assert.True(result.IsError);
        Assert.Equal(1, transport.Requests);
    }

    private sealed class ObservedTransport : HttpMessageHandler
    {
        internal int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
