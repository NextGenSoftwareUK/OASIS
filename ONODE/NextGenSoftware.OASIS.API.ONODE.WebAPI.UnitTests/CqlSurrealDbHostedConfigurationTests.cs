using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using SurrealProvider = NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.SurrealDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlSurrealDbHostedConfigurationTests
{
    private const string Origin = "https://surreal.example.test";
    private const string Probe = "11111111-1111-4111-8111-111111111111";

    private static Dictionary<string, string?> Settings() => new()
    {
        ["CQL_SURREALDB_QUERY_ORIGIN"] = Origin,
        ["CQL_SURREALDB_QUERY_USERNAME"] = "reader",
        ["CQL_SURREALDB_QUERY_PASSWORD"] = "restricted",
        ["CQL_SURREALDB_QUERY_NAMESPACE"] = "teaching",
        ["CQL_SURREALDB_QUERY_DATABASE"] = "public_demo",
        ["CQL_SURREALDB_QUERY_PROBE_ID"] = Probe
    };

    [Fact]
    public void ExplicitSettingsEnrollOneInactiveReadProfile()
    {
        var services = new ServiceCollection();
        services.AddSurrealDbReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddSurrealDbReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.SurrealDBOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("ORIGIN", "http://surreal.example.test")]
    [InlineData("ORIGIN", "https://localhost")]
    [InlineData("DATABASE", "bad name")]
    [InlineData("NAMESPACE", null)]
    [InlineData("PROBE_ID", null)]
    [InlineData("PASSWORD", null)]
    public void RejectsInvalidScopeAndCredentialsBeforeRegistration(string field, string? value)
    {
        var input = Settings();
        input["CQL_SURREALDB_QUERY_" + field] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddSurrealDbReadSource(
            new ConfigurationBuilder().AddInMemoryCollection(input).Build()));
        Assert.Empty(services);
    }

    [Fact]
    public async Task ActualSdkUsesInjectedTransportWithDatabaseAuthenticationScope()
    {
        using var transport = new ObservedTransport();
        var provider = new SurrealProvider(Origin, "reader", "restricted", "teaching", "public_demo", Guid.Parse(Probe), transport);
        var result = await provider.ActivateQuerySourceAsync(default);
        Assert.True(result.IsError);
        Assert.NotEmpty(transport.Requests);
        Assert.All(transport.Requests, request =>
        {
            Assert.Equal("teaching", request.Namespace);
            Assert.Equal("public_demo", request.Database);
            Assert.Equal("surreal.example.test", request.Host);
        });
    }

    private sealed class ObservedTransport : HttpMessageHandler
    {
        internal List<(string Namespace, string Database, string Host)> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Headers.GetValues("surreal-auth-ns").Single(),
                request.Headers.GetValues("surreal-auth-db").Single(), request.RequestUri!.Host));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }
    }
}
