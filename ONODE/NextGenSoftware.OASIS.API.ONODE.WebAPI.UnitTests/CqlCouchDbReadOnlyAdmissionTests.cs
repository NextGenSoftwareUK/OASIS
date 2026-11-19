using System.Net;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.CouchDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlCouchDbReadOnlyAdmissionTests
{
    private static readonly Uri Origin = new("https://couch.example.invalid");
    private static readonly Guid Probe = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static string Path => $"/oasis_holons/{Probe:D}?conflicts=true";

    [Fact]
    public void AdmissionRequiresAnExplicitQueryOnlyTariff()
    {
        var providers = new[] { ProviderType.CouchDBOASIS };
        var bindings = Array.Empty<NativeStorageAdmissionBinding>();
        var tariffs = new[] { new StorageQueryTariff(ProviderType.CouchDBOASIS, 0, "demo-read-v1") };
        var factory = new PricedStorageAdmissionFactory(null, bindings,
            Array.Empty<ProviderType>(), tariffs, null, providers);
        Assert.Equal(providers, factory.QueryProviders);
        Assert.Contains(ProviderType.CouchDBOASIS, factory.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null, bindings,
            Array.Empty<ProviderType>(), tariffs, null, Array.Empty<ProviderType>()));
    }

    [Theory]
    [InlineData("public", true)]
    [InlineData("private", false)]
    [InlineData("missing", false)]
    [InlineData("conflict", false)]
    [InlineData("wrong-id", false)]
    public async Task ActivationReadsOnlyTheExactPublicProbe(string shape, bool expected)
    {
        var transport = new Reply(shape);
        using var client = new HttpClient(new CqlCouchDbReadSourceRegistration.ExactReadHandler(Origin, transport));
        var source = new CouchDbQuerySource(Origin.AbsoluteUri, "teaching-reader", "synthetic", client, Probe);

        var result = await source.ActivateQuerySourceAsync(default);

        Assert.Equal(expected, source.IsProviderActivated);
        Assert.Equal(!expected, result.IsError);
        var request = Assert.Single(transport.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(new Uri(Origin, Path), request.Uri);
        Assert.Equal("Basic", request.Scheme);
    }

    [Fact]
    public async Task TransportRejectsWritesAndCrossOriginReadsBeforeDispatch()
    {
        var transport = new Reply("public");
        using var client = new HttpClient(new CqlCouchDbReadSourceRegistration.ExactReadHandler(Origin, transport));
        using var write = new HttpRequestMessage(HttpMethod.Put, new Uri(Origin, Path));
        using var crossOrigin = new HttpRequestMessage(HttpMethod.Get, new Uri("https://other.example.invalid" + Path));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(write));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(crossOrigin));
        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task QueryDispatchesOnlyCatalogueKeys()
    {
        const ProviderType authority = ProviderType.CouchDBOASIS;
        var other = Guid.Parse("22222222-2222-4222-8222-222222222222");
        var transport = new Reply("public");
        using var client = new HttpClient(new CqlCouchDbReadSourceRegistration.ExactReadHandler(Origin, transport));
        var source = new CouchDbQuerySource(Origin.AbsoluteUri, "teaching-reader", "synthetic", client, Probe);
        Assert.False((await source.ActivateQuerySourceAsync(default)).IsError);
        transport.Requests.Clear();

        var requested = new[] { Probe, other };
        var access = CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), authority, new[] { Probe });
        Assert.False(access.IsError, access.Message);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(access.Result).Result.Where(row => requested.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, authority, new[] { new CosmicQueryTarget(authority, source.QueryCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        var result = await source.ExecuteCosmicQueryAsync(plan.Result);

        Assert.False(result.IsError, result.Message);
        Assert.Equal(Probe, Assert.Single(result.Result).Id);
        Assert.Equal(new Uri(Origin, Path), Assert.Single(transport.Requests).Uri);
    }

    private sealed class Reply(string shape) : HttpMessageHandler
    {
        internal readonly List<(HttpMethod Method, Uri? Uri, string? Scheme)> Requests = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.RequestUri, request.Headers.Authorization?.Scheme));
            if (shape == "missing") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            var id = shape == "wrong-id" ? Guid.Empty : Probe;
            var document = JsonSerializer.Serialize(new
            {
                _id = id.ToString("D"),
                _rev = "1-controlled",
                _conflicts = shape == "conflict" ? new[] { "1-other" } : Array.Empty<string>(),
                id,
                isPublic = shape != "private"
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(document, Encoding.UTF8, "application/json")
            });
        }
    }
}
