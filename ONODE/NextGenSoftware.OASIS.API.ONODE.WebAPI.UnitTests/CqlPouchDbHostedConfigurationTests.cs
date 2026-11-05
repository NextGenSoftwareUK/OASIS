using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlPouchDbHostedConfigurationTests
{
    private const string Sandbox = "{\"OASIS\":{\"Sandbox\":{\"Enabled\":true}}}";
    private static readonly string Digest = new('a', 64);
    private static IConfiguration Settings(string? enabled, string? digest, string? dna) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["OASIS_SANDBOX_POUCHDB"] = enabled,
            ["CQL_POUCHDB_QUERY_SOURCE_DIGEST"] = digest, ["OASIS_DNA_JSON"] = dna }).Build();

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public void DisabledRuntimeDoesNotEnrollSource(string? enabled)
    {
        var services = new ServiceCollection(); services.AddPouchDbReadSource(Settings(enabled, null, null));
        Assert.Empty(services);
    }

    [Theory]
    [InlineData("TRUE", "valid", Sandbox)]
    [InlineData("", "valid", Sandbox)]
    [InlineData("1", "valid", Sandbox)]
    [InlineData("false", "valid", Sandbox)]
    [InlineData(null, "valid", Sandbox)]
    [InlineData("true", "valid", null)]
    [InlineData("true", "valid", "{malformed")]
    [InlineData("true", "valid", "[]")]
    [InlineData("true", "valid", "{\"OASIS\":{\"Sandbox\":{\"Enabled\":false}}}")]
    [InlineData("true", "valid", "{\"OASIS\":{\"Sandbox\":{\"Enabled\":\"true\"}}}")]
    [InlineData("true", null, Sandbox)]
    [InlineData("true", "invalid", Sandbox)]
    [InlineData("true", "newline", Sandbox)]
    public void InvalidConfigurationFailsBeforeServicesAreCreated(string? enabled, string? digest, string? dna)
    {
        var services = new ServiceCollection();
        var configuration = Settings(enabled, digest == "valid" ? Digest : digest == "newline" ? Digest + "\n" : digest, dna);
        Assert.Throws<InvalidOperationException>(() => services.AddPouchDbReadSource(configuration));
        Assert.Empty(services);
    }

    [Fact]
    public void ValidConfigurationCreatesOneInactiveReadOnlyProfileWithoutNetwork()
    {
        var services = new ServiceCollection(); services.AddPouchDbReadSource(Settings("true", Digest, Sandbox));
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.PouchDBOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
        Assert.Same(profile, container.GetRequiredService<CqlReadSourceProfile>());
    }

    [Theory]
    [InlineData("wrong-source", "9.0.0")]
    [InlineData("missing-source", "9.0.0")]
    [InlineData("duplicate-source", "9.0.0")]
    [InlineData("valid", "8.0.0")]
    [InlineData("valid", null)]
    public async Task TransportRejectsUnownedResponsesWithoutRetry(string source, string? version)
    {
        var transport = new ControlledTransport(source, version);
        using var client = new HttpClient(new CqlPouchDbSourceHttpHandler(Digest, transport));
        await Assert.ThrowsAsync<InvalidDataException>(() => client.GetAsync(CqlPouchDbReadSourceRegistration.Origin + "/oasis_holons"));
        Assert.Equal(1, transport.Requests);
    }

    [Fact]
    public async Task TransportPreservesOwnedNativeErrorsAndForwardsNoVisitorCredentials()
    {
        var transport = new ControlledTransport("valid", "9.0.0");
        using var client = new HttpClient(new CqlPouchDbSourceHttpHandler(Digest, transport));
        using var response = await client.GetAsync(CqlPouchDbReadSourceRegistration.Origin + "/oasis_holons");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("{\"error\":\"not_found\",\"reason\":\"missing\"}", await response.Content.ReadAsStringAsync());
        Assert.Equal(1, transport.Requests);
    }

    private sealed class ControlledTransport(string source, string? version) : HttpMessageHandler
    {
        internal int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Assert.Null(request.Headers.Authorization); Assert.False(request.Headers.Contains("Cookie"));
            var response = new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{\"error\":\"not_found\",\"reason\":\"missing\"}") };
            if (source != "missing-source") response.Headers.Add("X-OASIS-PouchDB-Source", source == "duplicate-source" ? new[] { Digest, Digest } : new[] { source == "valid" ? Digest : "other" });
            if (version != null) response.Headers.Add("X-OASIS-PouchDB-Version", version);
            return Task.FromResult(response);
        }
    }
}
