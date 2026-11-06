using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlUpstashHostedConfigurationTests
{
    private const string Origin = "https://teaching.upstash.io";
    private const string Id = "aaaaaaaa-1111-4111-8111-111111111111";
    private const string Valid = "[\"MGET\",\"holon:" + Id + "\"]";

    [Fact]
    public void ExplicitEnrollmentRequiresNativeHttpsAndAnExistingProbe()
    {
        var services = new ServiceCollection();
        services.AddUpstashReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var settings = new Dictionary<string, string?>
        {
            ["CQL_UPSTASH_QUERY_ORIGIN"] = "https://teaching.upstash.io.foreign.example",
            ["CQL_UPSTASH_QUERY_READER_TOKEN"] = "controlled-reader",
            ["CQL_UPSTASH_QUERY_PROBE_ID"] = Id
        };
        IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Assert.Throws<InvalidOperationException>(() => services.AddUpstashReadSource(Configuration()));
        settings["CQL_UPSTASH_QUERY_ORIGIN"] = Origin;
        settings["CQL_UPSTASH_QUERY_PROBE_ID"] = Guid.Empty.ToString("D");
        Assert.Throws<InvalidOperationException>(() => services.AddUpstashReadSource(Configuration()));
        Assert.Empty(services);

        settings["CQL_UPSTASH_QUERY_PROBE_ID"] = Id;
        services.AddUpstashReadSource(Configuration());
        using var container = services.BuildServiceProvider();
        var source = Assert.Single(container.GetServices<CqlReadSourceProfile>()).Storage;
        Assert.Equal(ProviderType.UpstashOASIS, source.ProviderType.Value);
        Assert.False(source.IsProviderActivated);
    }

    [Theory]
    [InlineData("GET", Origin + "/", Valid)]
    [InlineData("POST", "https://foreign.upstash.io/", Valid)]
    [InlineData("POST", Origin + "/pipeline", Valid)]
    [InlineData("POST", Origin + "/?_token=foreign", Valid)]
    [InlineData("POST", Origin + "/", "[\"SET\",\"holon:" + Id + "\",\"foreign\"]")]
    [InlineData("POST", Origin + "/", "[\"MGET\",\"avatar:" + Id + "\"]")]
    [InlineData("POST", Origin + "/", "[\"MGET\",\"holon:AAAAAAAA-1111-4111-8111-111111111111\"]")]
    [InlineData("POST", Origin + "/", "[\"MGET\"]")]
    public async Task NonTeachingCommandsNeverReachTheTransport(string method, string uri, string body)
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlUpstashReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));
        using var request = new HttpRequestMessage(new HttpMethod(method), uri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
        Assert.Equal(0, transport.Requests);
    }

    [Fact]
    public async Task TransportAdmitsOneThroughOneHundredCanonicalKeys()
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlUpstashReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));
        static StringContent Body(int count) => new(JsonSerializer.Serialize(new[] { "MGET" }
            .Concat(Enumerable.Range(1, count).Select(index => $"holon:aaaaaaaa-1111-4111-8111-{index:D12}"))),
            Encoding.UTF8, "application/json");
        using var one = Body(1);
        using var max = Body(100);
        using var tooMany = Body(101);
        using var first = await client.PostAsync(Origin, one);
        using var second = await client.PostAsync(Origin, max);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.PostAsync(Origin, tooMany));
        Assert.Equal(2, transport.Requests);
    }

    private sealed class ObservedTransport : HttpMessageHandler
    {
        internal int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
