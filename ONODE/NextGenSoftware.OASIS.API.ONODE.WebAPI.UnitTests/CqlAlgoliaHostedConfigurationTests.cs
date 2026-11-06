using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlAlgoliaHostedConfigurationTests
{
    private const string Application = "Teaching123";
    private const string Id = "01234567-89ab-cdef-0123-456789abcdef";
    private const string Route = "https://teaching123-dsn.algolia.net/1/indexes/*/objects";
    private const string Record = "{\"indexName\":\"oasis_holons\",\"objectID\":\"" + Id + "\"}";

    [Fact]
    public void ExplicitSettingsRegisterAnAlgoliaQueryProfile()
    {
        var services = new ServiceCollection();
        services.AddAlgoliaReadSource(Settings(Application, "controlled-test-key", Id));
        using var provider = services.BuildServiceProvider();

        Assert.Equal(ProviderType.AlgoliaOASIS, provider.GetRequiredService<CqlReadSourceProfile>().Storage.ProviderType.Value);
    }

    [Theory]
    [InlineData("invalid/app", "controlled-test-key", Id)]
    [InlineData(Application, "key with spaces", Id)]
    [InlineData(Application, "controlled-test-key", "01234567-89ab-cdef-0123-456789ABCDEF")]
    [InlineData(Application, null, Id)]
    public void InvalidProtectedSettingsRejectRegistration(string application, string key, string probe)
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddAlgoliaReadSource(Settings(application, key, probe)));
        Assert.Empty(services);
    }

    [Fact]
    public async Task ExactBatchReachesTransport()
    {
        var transport = new RecordingTransport();
        using var client = new HttpClient(new CqlAlgoliaReadSourceRegistration.ExactReadHandler(Application, transport));
        using var request = CreateRequest(Route, Application, Record);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, transport.Requests);
    }

    [Fact]
    public async Task FullNativeBatchFitsTheTransportBudget()
    {
        var transport = new RecordingTransport();
        using var client = new HttpClient(new CqlAlgoliaReadSourceRegistration.ExactReadHandler(Application, transport));
        var records = Enumerable.Range(1, 100).Select(index =>
            JsonSerializer.Serialize(new { indexName = "oasis_holons", objectID = $"00000000-0000-0000-0000-{index:D12}" }));
        using var request = CreateRequest(Route, Application, string.Join(",", records));
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, transport.Requests);
    }

    [Theory]
    [InlineData("host")]
    [InlineData("header")]
    [InlineData("route")]
    [InlineData("index")]
    [InlineData("duplicate")]
    [InlineData("fields")]
    [InlineData("uuid")]
    public async Task OutOfContractBatchDoesNotReachTransport(string mutation)
    {
        var transport = new RecordingTransport();
        using var client = new HttpClient(new CqlAlgoliaReadSourceRegistration.ExactReadHandler(Application, transport));
        var uri = mutation switch
        {
            "host" => Route.Replace("teaching123", "different"),
            "route" => Route.Replace("objects", "browse"),
            _ => Route
        };
        var records = mutation switch
        {
            "index" => Record.Replace("oasis_holons", "private_holons"),
            "duplicate" => Record + "," + Record,
            "fields" => Record[..^1] + ",\"attributesToRetrieve\":[]}",
            "uuid" => Record.Replace(Id, Id.ToUpperInvariant()),
            _ => Record
        };
        using var request = CreateRequest(uri, mutation == "header" ? "Different" : Application, records);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
        Assert.Equal(0, transport.Requests);
    }

    private static HttpRequestMessage CreateRequest(string uri, string application, string records)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent("{\"requests\":[" + records + "]}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Algolia-Application-Id", application);
        return request;
    }

    private static IConfiguration Settings(string application, string key, string probe)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["CQL_ALGOLIA_QUERY_APP_ID"] = application,
            ["CQL_ALGOLIA_QUERY_READER_KEY"] = key,
            ["CQL_ALGOLIA_QUERY_PROBE_ID"] = probe
        }).Build();

    private sealed class RecordingTransport : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
