using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.UrbitOASIS;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    private static readonly Guid UrbitConnection = Guid.Parse("eaa5679e-9250-4abb-bb5e-d415dcf1f117");
    private static readonly string[] UrbitCids = { "0v1", "0v2", "0v3" };

    private static ServiceProvider UrbitPolicyServices(Guid owner)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["CQL_URBIT_QUERY_ORIGIN"] = "https://ship.example.test", ["CQL_URBIT_QUERY_SHIP"] = "~zod",
            ["CQL_URBIT_QUERY_SESSION_TOKEN"] = "0v1", ["CQL_URBIT_QUERY_OWNER"] = owner.ToString("D"),
            ["CQL_URBIT_QUERY_CONNECTION_ID"] = UrbitConnection.ToString("D"),
            ["CQL_URBIT_QUERY_PAGE_IDS"] = JsonSerializer.Serialize(UrbitCids)
        }).Build();
        var services = new ServiceCollection();
        services.AddUrbitReadSource(config);
        return services.BuildServiceProvider();
    }

    private sealed class UrbitWire : HttpMessageHandler
    {
        internal int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Null(request.Content);
            var path = request.RequestUri!.AbsolutePath;
            var cid = UrbitCids.Single(id => path == "/~/scry/contacts/v1/book/id/" + id + ".json");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("[{}, {\"nickname\":{\"type\":\"text\",\"value\":\"Owned " + cid + "\"}}]", Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class UrbitEndpointSource(UrbitContactBookQuerySource source) : ICqlV1Providers
    {
        internal int Managers;
        public IOASISStorageProvider Resolve(ProviderType? requested)
        {
            if (requested != ProviderType.UrbitOASIS || !source.IsProviderActivated)
                throw new InvalidOperationException("Explicit activated private source required.");
            return source;
        }
        public HolonManager CreateManager(IOASISStorageProvider provider)
        {
            Assert.Same(source, provider);
            Managers++;
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
            var registry = new ProviderManager(null, dna) {
                IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false
            };
            return new HolonManager(provider, dna, registry);
        }
    }

    [Fact]
    public async Task UrbitAdmittedOwnerQueriesUseRealAdapterManagerAndLeafProjection()
    {
        var wire = new UrbitWire();
        using var http = new HttpClient(wire);
        using var reader = new UrbitContactsV1Client(new Uri("https://ship.example.test"), http);
        using var adapter = new UrbitContactBookQuerySource(reader, Actor, UrbitConnection, UrbitCids);
        Assert.False((await adapter.ActivateQuerySourceAsync()).IsError);
        using var services = UrbitPolicyServices(Actor);
        var source = new UrbitEndpointSource(adapter);
        var ids = UrbitCids.Select(cid => UrbitContactBookQuerySource.LogicalId(Actor, UrbitConnection, cid)).ToArray();
        foreach (var selected in new[] { ids, new[] { ids[0], ids[2] } })
        {
            var admitted = Admission(ProviderType.UrbitOASIS);
            admitted.Http.RequestServices = services;
            var request = new CqlV1Request { Provider = "UrbitOASIS", OrderBy = "None", Take = 10,
                IncludeMetadata = true, Filters = new[] { new CqlV1Filter {
                    Field = "Id", Operator = CqlV1Operator.In, Value = JsonSerializer.SerializeToElement(selected)
                } } };
            var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http, request, source));
            var file = Assert.IsType<FileContentResult>(result);
            using var body = JsonDocument.Parse(file.FileContents);
            var payload = body.RootElement.GetProperty("result");
            Assert.Equal("UrbitOASIS", payload.GetProperty("provider").GetString());
            Assert.Equal(selected.Length, payload.GetProperty("count").GetInt32());
            var rows = payload.GetProperty("rows").EnumerateArray().ToArray();
            Assert.Equal(selected.Order(), rows.Select(row => row.GetProperty("id").GetGuid()).Order());
            foreach (var row in rows)
            {
                Assert.False(row.GetProperty("isPublic").GetBoolean());
                Assert.Equal(Actor, row.GetProperty("createdByAvatarId").GetGuid());
                var cid = row.GetProperty("metadata").GetProperty("UrbitBookPageId").GetString();
                Assert.Equal("Owned " + cid, row.GetProperty("metadata").GetProperty("OverlayNickname").GetString());
            }
            var receipt = Assert.Single(admitted.Receipts);
            Assert.Equal("UrbitOASIS", receipt.Provider);
            Assert.Equal(1, receipt.Units);
        }
        Assert.Equal(6, wire.Calls); // One explicit probe, then three and two point reads.
        Assert.Equal(2, source.Managers);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UrbitAdmittedRejectionDoesNotReadNativePages(bool foreignActor)
    {
        var owner = foreignActor ? Guid.NewGuid() : Actor;
        var wire = new UrbitWire();
        using var http = new HttpClient(wire);
        using var reader = new UrbitContactsV1Client(new Uri("https://ship.example.test"), http);
        using var adapter = new UrbitContactBookQuerySource(reader, owner, UrbitConnection, UrbitCids);
        Assert.False((await adapter.ActivateQuerySourceAsync()).IsError);
        using var services = UrbitPolicyServices(owner);
        var source = new UrbitEndpointSource(adapter);
        var admitted = Admission(ProviderType.UrbitOASIS);
        admitted.Http.RequestServices = services;
        var request = new CqlV1Request { Provider = "UrbitOASIS", OrderBy = foreignActor ? "None" : "Name" };
        var result = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http, request, source));
        Assert.Equal(foreignActor ? 403 : 400, Assert.IsType<ObjectResult>(result).StatusCode);
        if (foreignActor) Assert.Equal("CQL_ACCESS_FORBIDDEN", Code(result));
        Assert.Equal(1, wire.Calls);
        Assert.Equal(0, source.Managers);
        Assert.Empty(admitted.Receipts);
    }
}
