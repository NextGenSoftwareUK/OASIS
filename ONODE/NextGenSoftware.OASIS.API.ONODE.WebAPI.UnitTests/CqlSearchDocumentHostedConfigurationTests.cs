using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlSearchDocumentHostedConfigurationTests
{
    private const string Origin = "https://search.example.test";
    private const string Id = "11111111-1111-4111-8111-111111111111";
    private const string ExactQuery = "?realtime=true&_source=true";

    private static Dictionary<string, string?> ValidSettings() => new()
    {
        ["CQL_ELASTICSEARCH_QUERY_ORIGIN"] = Origin,
        ["CQL_ELASTICSEARCH_QUERY_PROBE_ID"] = Id,
        ["CQL_ELASTICSEARCH_QUERY_API_KEY"] = "aWQ6cmVzdHJpY3RlZA==",
        ["CQL_OPENSEARCH_QUERY_ORIGIN"] = Origin,
        ["CQL_OPENSEARCH_QUERY_PROBE_ID"] = Id,
        ["CQL_OPENSEARCH_QUERY_USERNAME"] = "reader",
        ["CQL_OPENSEARCH_QUERY_PASSWORD"] = "restricted"
    };

    [Fact]
    public void ExplicitSettingsCreateIndependentInactiveReadProfiles()
    {
        var services = new ServiceCollection();
        services.AddSearchDocumentReadSources(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddSearchDocumentReadSources(new ConfigurationBuilder().AddInMemoryCollection(ValidSettings()).Build());
        using var container = services.BuildServiceProvider();
        var profiles = container.GetServices<CqlReadSourceProfile>().ToArray();
        Assert.Equal(new[] { ProviderType.ElasticsearchOASIS, ProviderType.OpenSearchOASIS },
            profiles.Select(profile => profile.Storage.ProviderType.Value));
        Assert.All(profiles, profile => Assert.False(profile.Storage.IsProviderActivated));
    }

    [Theory]
    [InlineData("CQL_ELASTICSEARCH_QUERY_API_KEY", "malformed!")]
    [InlineData("CQL_ELASTICSEARCH_QUERY_PROBE_ID", null)]
    [InlineData("CQL_OPENSEARCH_QUERY_ORIGIN", "https://localhost")]
    [InlineData("CQL_OPENSEARCH_QUERY_USERNAME", "invalid:reader")]
    [InlineData("CQL_OPENSEARCH_QUERY_PASSWORD", null)]
    public void InvalidSettingsAreRefusedBeforeEitherProfileIsRegistered(string setting, string? value)
    {
        var input = ValidSettings();
        input[setting] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddSearchDocumentReadSources(
            new ConfigurationBuilder().AddInMemoryCollection(input).Build()));
        Assert.Empty(services);
    }

    [Theory]
    [InlineData("oasis-holons", "oasis_holons")]
    [InlineData("oasis_holons", "oasis-holons")]
    public async Task EachTransportOnlyForwardsCanonicalDocumentReadsFromItsOwnIndex(string index, string foreignIndex)
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlSearchDocumentReadSources.ExactReadHandler(new Uri(Origin), index, transport));
        var exact = "/" + index + "/_doc/" + Id + ExactQuery;
        using var allowed = await client.GetAsync(Origin + exact);
        Assert.Equal(HttpStatusCode.NotFound, allowed.StatusCode);
        foreach (var uri in new[] { Origin + "/" + foreignIndex + "/_doc/" + Id + ExactQuery,
                     Origin + "/" + index + "/_search", Origin + exact + "&routing=other",
                     "https://different.example.test" + exact })
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync(uri));
        using var write = new HttpRequestMessage(HttpMethod.Put, Origin + exact);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(write));
        Assert.Equal(new[] { exact }, transport.Requests);
    }

    private sealed class ObservedTransport : HttpMessageHandler
    {
        internal List<string> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!.PathAndQuery);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
