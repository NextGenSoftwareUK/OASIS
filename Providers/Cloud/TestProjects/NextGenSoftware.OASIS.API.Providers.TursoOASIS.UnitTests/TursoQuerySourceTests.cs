using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.TursoOASIS.UnitTests;

[TestClass]
public class TursoQuerySourceTests
{
    [TestMethod]
    public async Task QueryUsesBoundExactKeysAndChecksPublicAuthorityBeforePaging()
    {
        const ProviderType provider = ProviderType.TursoOASIS;
        var ids = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        using var transport = new Pipeline(ids);
        using var source = new TursoQuerySource(new Uri("https://teaching.turso.io/"), "reader-token", ids[0], transport);
        Assert.IsTrue((await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid() })).IsError);
        Assert.AreEqual(0, transport.Requests);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.Message);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        var targets = new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) };
        var result = await source.ExecuteCosmicQueryAsync(CosmicQueryPlanner.Plan(query, provider, targets).Result);
        Assert.IsFalse(result.IsError, result.Exception?.Message);
        CollectionAssert.AreEquivalent(ids[..2], result.Result.Select(row => row.Id).ToArray());
        transport.PrivateSecond = true;
        var denied = await source.ExecuteCosmicQueryAsync(CosmicQueryPlanner.Plan(query.Take(1).Result, provider, targets).Result);
        Assert.IsTrue(denied.IsError);
        Assert.IsNull(denied.Result);
    }

    private sealed class Pipeline(Guid[] ids) : HttpMessageHandler
    {
        public bool PrivateSecond { get; set; }
        public int Requests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/v2/pipeline", request.RequestUri?.AbsolutePath);
            Assert.AreEqual("Bearer reader-token", request.Headers.Authorization?.ToString());
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var requests = body.RootElement.GetProperty("requests");
            Assert.AreEqual(2, requests.GetArrayLength());
            Assert.AreEqual("execute", requests[0].GetProperty("type").GetString());
            Assert.AreEqual("close", requests[1].GetProperty("type").GetString());
            var statement = requests[0].GetProperty("stmt");
            Assert.AreEqual("SELECT id, is_deleted, data_json FROM oasis_holons WHERE id=? LIMIT 2", statement.GetProperty("sql").GetString());
            var argument = statement.GetProperty("args").EnumerateArray().Single();
            Assert.AreEqual("text", argument.GetProperty("type").GetString());
            var key = argument.GetProperty("value").GetString()!;
            var id = Guid.ParseExact(key, "D");
            Assert.IsTrue(ids.Contains(id));
            var cols = new[] { new { name = "id" }, new { name = "is_deleted" }, new { name = "data_json" } };
            var rows = id == ids[3] ? Array.Empty<object[]>() : new[] { new object[]
            {
                new { type = "text", value = key },
                new { type = "integer", value = id == ids[2] ? "1" : "0" },
                new { type = "text", value = JsonSerializer.Serialize(new { id, isPublic = id != ids[1] || !PrivateSecond }) }
            } };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new
            {
                results = new object[]
                {
                    new { type = "ok", response = new { type = "execute", result = new { cols, rows } } },
                    new { type = "ok", response = new { type = "close" } }
                }
            }) };
        }
    }
}
