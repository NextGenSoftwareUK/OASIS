using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlSolrHostedConfigurationTests
{
    private const string Origin = "https://solr.example.test";
    private const string Collection = "/solr/oasis_holons";
    private const string Id = "11111111-1111-4111-8111-111111111111";
    private const string Schema = Collection + "/schema/uniquekey?wt=json";
    private const string Document = Collection + "/get?id=" + Id + "&wt=json&omitHeader=false";

    private static IConfiguration Settings(string? origin, string? username, string? password)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["CQL_SOLR_QUERY_ORIGIN"] = origin,
            ["CQL_SOLR_QUERY_USERNAME"] = username,
            ["CQL_SOLR_QUERY_PASSWORD"] = password
        }).Build();

    [Fact]
    public void EnrollsOnlyAnExplicitInactiveReadProfile()
    {
        var services = new ServiceCollection();
        services.AddSolrReadSource(Settings(null, null, null));
        Assert.Empty(services);
        services.AddSolrReadSource(Settings(Origin, "viewer", "restricted"));
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.SolrOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData(null, "viewer", "restricted")]
    [InlineData(Origin, null, "restricted")]
    [InlineData(Origin, "viewer", null)]
    [InlineData(Origin, "viewer:other", "restricted")]
    [InlineData("http://solr.example.test", "viewer", "restricted")]
    [InlineData("https://localhost", "viewer", "restricted")]
    [InlineData("https://user@solr.example.test", "viewer", "restricted")]
    [InlineData(Origin + Collection, "viewer", "restricted")]
    public void RefusesInvalidConfigurationBeforeRegistration(string? origin, string? username, string? password)
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddSolrReadSource(Settings(origin, username, password)));
        Assert.Empty(services);
    }

    [Theory]
    [InlineData("POST", Origin + Document)]
    [InlineData("GET", Origin + Collection + "/select?q=*:*")]
    [InlineData("GET", Origin + Collection + "/schema/fields?wt=json")]
    [InlineData("GET", Origin + Collection + "/get?ids=" + Id + "&wt=json&omitHeader=false")]
    [InlineData("GET", Origin + Collection + "/get?id=" + Id + "&id=" + Id + "&wt=json&omitHeader=false")]
    [InlineData("GET", Origin + Document + "&fl=id")]
    [InlineData("GET", "https://different.example.test" + Document)]
    public async Task RefusesUnadmittedRequestsBeforeTransport(string method, string uri)
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlSolrReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));
        using var request = new HttpRequestMessage(new HttpMethod(method), uri);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task ForwardsOnlyTheUniqueKeyCheckAndCanonicalDocumentRead()
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlSolrReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));
        using var schema = await client.GetAsync(Origin + Schema);
        using var document = await client.GetAsync(Origin + Document);
        Assert.Equal(new[] { Schema, Document }, transport.Requests);
        Assert.Equal(HttpStatusCode.NotFound, document.StatusCode);
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
