using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS.UnitTests;

[TestClass]
public class CouchbaseQuerySourceTests
{
    [TestMethod]
    public async Task NativeReadOnlyStatementUsesExactKeysAndRequiresPublicRecords()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var transport = new QueryTransport(ids);
        using var client = new HttpClient(transport);
        var source = new CouchbaseQuerySource(new Uri("https://couchbase.example.test:18093"), "teaching",
            "controlled-reader", "controlled-secret", client, ids[0]);
        transport.PrivateId = ids[0];
        Assert.IsTrue((await source.ActivateQuerySourceAsync(default)).IsError);
        Assert.IsFalse(source.IsProviderActivated);
        transport.PrivateId = null;
        var active = await source.ActivateQuerySourceAsync(default);
        Assert.IsFalse(active.IsError, active.Exception?.ToString() ?? active.Message);

        const ProviderType authority = ProviderType.CouchbaseOASIS;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), authority, ids).Result).Result;
        var plan = CosmicQueryPlanner.Plan(query, authority, new[] { new CosmicQueryTarget(authority, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var selected = await source.ExecuteCosmicQueryAsync(plan.Result);
        Assert.IsFalse(selected.IsError, selected.Exception?.ToString() ?? selected.Message);
        CollectionAssert.AreEqual(ids, selected.Result.Select(row => row.Id).ToArray());

        transport.PrivateId = ids[1];
        var rejected = await source.ExecuteCosmicQueryAsync(plan.Result);
        Assert.IsTrue(rejected.IsError);
        Assert.IsNull(rejected.Result);
        Assert.AreEqual(4, transport.Requests);
    }

    private sealed class QueryTransport(Guid[] ids) : HttpMessageHandler
    {
        internal Guid? PrivateId { get; set; }
        internal int Requests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("https://couchbase.example.test:18093/query/service", request.RequestUri?.AbsoluteUri);
            Assert.AreEqual("Basic", request.Headers.Authorization?.Scheme);
            Assert.AreEqual("controlled-reader:controlled-secret",
                System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization!.Parameter!)));
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var body = json.RootElement;
            Assert.AreEqual(4, body.EnumerateObject().Count());
            Assert.IsTrue(body.GetProperty("readonly").GetBoolean());
            Assert.AreEqual("15s", body.GetProperty("timeout").GetString());
            StringAssert.Contains(body.GetProperty("statement").GetString(), "FROM `teaching`.`_default`.`holons` AS h USE KEYS $keys");
            var requested = body.GetProperty("$keys").EnumerateArray().Select(value => value.GetGuid()).ToArray();
            CollectionAssert.AreEqual(Requests <= 2 ? new[] { ids[0] } : ids, requested);
            var rows = requested.Select(id => new
            {
                Key = id, Id = id, IsDeleted = false,
                DataJson = JsonSerializer.Serialize(new Holon { Id = id, IsPublic = id != PrivateId },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            }).ToArray();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { status = "success", results = rows }))
            };
        }
    }
}
