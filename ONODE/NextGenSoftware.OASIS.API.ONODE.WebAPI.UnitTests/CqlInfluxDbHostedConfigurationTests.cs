using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS;
using InfluxProvider = NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.InfluxDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlInfluxDbHostedConfigurationTests
{
    private const string Origin = "https://influx.example.test";
    private const string Organization = "teaching-org";
    private const string Bucket = "teaching-bucket";
    private const string Route = Origin + "/api/v2/query?org=" + Organization;
    private const string Id = "aaaaaaaa-1111-4111-8111-111111111111";
    private static readonly Guid Probe = Guid.Parse(Id);

    [Fact]
    public void EnrollmentIsExplicitAndOriginIsCredentialFree()
    {
        var services = new ServiceCollection();
        services.AddInfluxDbReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var settings = new Dictionary<string, string?>
        {
            ["CQL_INFLUXDB_QUERY_ORIGIN"] = "https://user@influx.example.test",
            ["CQL_INFLUXDB_QUERY_ORGANIZATION"] = Organization,
            ["CQL_INFLUXDB_QUERY_BUCKET"] = Bucket,
            ["CQL_INFLUXDB_QUERY_READER_TOKEN"] = "controlled-reader",
            ["CQL_INFLUXDB_QUERY_PROBE_ID"] = Id
        };
        IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Assert.Throws<InvalidOperationException>(() => services.AddInfluxDbReadSource(Configuration()));
        Assert.Empty(services);
        settings["CQL_INFLUXDB_QUERY_ORIGIN"] = Origin;
        services.AddInfluxDbReadSource(Configuration());
        using var container = services.BuildServiceProvider();
        var source = Assert.Single(container.GetServices<CqlReadSourceProfile>()).Storage;
        Assert.Equal(ProviderType.InfluxDBOASIS, source.ProviderType.Value);
        Assert.False(source.IsProviderActivated);
    }

    [Fact]
    public async Task OfficialSdkUsesTheGuardOnEveryActivation()
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlInfluxDbReadSourceRegistration.ExactReadHandler(
            new Uri(Origin), Organization, Bucket, transport));
        var provider = new InfluxProvider(Origin, "controlled-reader", Organization, Bucket, client, Probe);
        var first = await provider.ActivateQuerySourceAsync(default);
        Assert.True(first.IsError);
        Assert.True(transport.Requests == 1, first.Exception?.ToString());
        Assert.Equal("Token controlled-reader", transport.Authorization);
        Assert.False((await provider.DeActivateProviderAsync()).IsError);
        Assert.True((await provider.ActivateQuerySourceAsync(default)).IsError);
        Assert.Equal(2, transport.Requests);
        Assert.False((await provider.DeActivateProviderAsync()).IsError);
    }

    [Theory]
    [InlineData("other-org")]
    [InlineData("other-bucket")]
    [InlineData("extra-flux")]
    [InlineData("extern")]
    [InlineData("write-route")]
    public async Task ForeignNativeQueriesNeverReachTheTransport(string change)
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlInfluxDbReadSourceRegistration.ExactReadHandler(
            new Uri(Origin), Organization, Bucket, transport));
        var query = InfluxDBCqlCompiler.BuildNativeFlux(change == "other-bucket" ? "other-bucket" : Bucket, new[] { Probe });
        if (change == "extra-flux") query += "\n  |> to(bucket: \"foreign\")";
        var address = change switch
        {
            "other-org" => Origin + "/api/v2/query?org=foreign",
            "write-route" => Origin + "/api/v2/write?org=" + Organization,
            _ => Route
        };
        var body = change == "extern" ? JsonSerializer.Serialize(new { query, @extern = new { } }) :
            JsonSerializer.Serialize(new { query });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.PostAsync(address, content));
        Assert.Equal(0, transport.Requests);
    }

    [Fact]
    public async Task OversizedNativeResponseIsRejectedBeforeItReachesTheSdk()
    {
        var transport = new ObservedTransport(8 * 1024 * 1024 + 1);
        using var client = new HttpClient(new CqlInfluxDbReadSourceRegistration.ExactReadHandler(
            new Uri(Origin), Organization, Bucket, transport));
        using var body = new StringContent(JsonSerializer.Serialize(new
        {
            query = InfluxDBCqlCompiler.BuildNativeFlux(Bucket, new[] { Probe })
        }), Encoding.UTF8, "application/json");
        await Assert.ThrowsAsync<HttpRequestException>(() => client.PostAsync(Route, body));
        Assert.Equal(1, transport.Requests);
    }

    private sealed class ObservedTransport(int responseLength = 0) : HttpMessageHandler
    {
        internal int Requests { get; private set; }
        internal string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Authorization = request.Headers.Authorization?.ToString();
            var content = new StringContent("");
            if (responseLength != 0) content.Headers.ContentLength = responseLength;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }
}
