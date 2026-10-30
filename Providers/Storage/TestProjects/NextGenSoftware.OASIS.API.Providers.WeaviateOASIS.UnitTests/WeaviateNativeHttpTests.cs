using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.WeaviateOASIS.UnitTests;

[TestClass]
public class WeaviateNativeHttpTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> OnSend = (_, _) => throw new InvalidOperationException();
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Calls++; return OnSend(request, token); }
    }

    private static HttpResponseMessage Json(object value, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("failed")]
    [DataRow("foreign")]
    [DataRow("class")]
    [DataRow("empty")]
    [DataRow("errors")]
    [DataRow("http")]
    [DataRow("budget")]
    public async Task OneNativeUpsertRequiresMatchingPerObjectSuccessWithoutExistenceProbe(string mode)
    {
        var transport = new Transport { OnSend = async (request, token) => {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/v1/batch/objects", request.RequestUri!.AbsolutePath);
            StringAssert.Contains(request.RequestUri.Query, "consistency_level=ALL");
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var objects = body.RootElement.GetProperty("objects");
            Assert.AreEqual(1, objects.GetArrayLength());
            var id = objects[0].GetProperty("id").GetGuid();
            Assert.AreEqual("OasisHolon", objects[0].GetProperty("class").GetString());
            using var payload = JsonDocument.Parse(objects[0].GetProperty("properties").GetProperty("dataJson").GetString()!);
            Assert.AreEqual(id, payload.RootElement.GetProperty("id").GetGuid());
            if (mode == "budget") return Json(new { padding = new string('x', 4 * 1024 * 1024 + 1) });
            var rows = new[] { new { @class = mode == "class" ? "Foreign" : "OasisHolon",
                id = mode == "foreign" ? Guid.NewGuid() : id,
                result = new { status = mode == "failed" ? "FAILED" : "SUCCESS",
                    errors = mode == "errors" ? new { error = new[] { new { message = "Injected rejection" } } } : null } } };
            return Json(mode == "empty" ? rows.Take(0).ToArray() : rows,
                mode == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
        }};
        using var http = new HttpClient(transport);
        var provider = new WeaviateOASIS(http, "https://fixture.invalid");
        var row = new Holon();
        var result = await provider.SaveHolonAsync(row, saveChildren: false);
        Assert.AreEqual(1, transport.Calls, "No check-then-write probe or alternate write path.");
        if (mode == "success")
        { Assert.IsFalse(result.IsError, result.Message); Assert.IsTrue(result.IsSaved); Assert.AreNotEqual(Guid.Empty, row.Id); }
        else
        { Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved); Assert.AreEqual(Guid.Empty, row.Id); }
    }

    [DataTestMethod]
    [DataRow("existing")]
    [DataRow("missing")]
    [DataRow("http")]
    [DataRow("schema")]
    [DataRow("badcreate")]
    [DataRow("vectorizer")]
    public async Task ActivationRequiresConfirmedAbsenceOrMatchingNativeSchema(string mode)
    {
        var creates = 0;
        var transport = new Transport { OnSend = async (request, token) => {
            string name;
            object[] properties;
            if (request.Method == HttpMethod.Post)
            {
                creates++;
                Assert.AreEqual("/v1/schema", request.RequestUri!.AbsolutePath);
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
                if (mode == "badcreate") return Json(new { @class = "Wrong", vectorizer = "none", properties = Array.Empty<object>() });
                return Json(body.RootElement.Clone());
            }
            Assert.AreEqual(HttpMethod.Get, request.Method);
            name = request.RequestUri!.Segments.Last();
            if (mode is "missing" or "badcreate") return Json(new { error = "Absent" }, HttpStatusCode.NotFound);
            properties = name == "OasisHolon"
                ? new object[] { new { name = "parentHolonId", dataType = new[] { "text" } }, new { name = "holonType", dataType = new[] { "int" } }, new { name = "isDeleted", dataType = new[] { "boolean" } }, new { name = "dataJson", dataType = new[] { "text" } } }
                : name == "OasisAvatar"
                ? new object[] { new { name = "username", dataType = new[] { "text" } }, new { name = "email", dataType = new[] { "text" } }, new { name = "isDeleted", dataType = new[] { "boolean" } }, new { name = "dataJson", dataType = new[] { "text" } } }
                : new object[] { new { name = "username", dataType = new[] { "text" } }, new { name = "email", dataType = new[] { "text" } }, new { name = "dataJson", dataType = new[] { "text" } } };
            return Json(new { @class = name, vectorizer = mode == "vectorizer" ? "external" : "none",
                properties = mode == "schema" ? Array.Empty<object>() : properties },
                mode == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
        }};
        using var http = new HttpClient(transport);
        var provider = new WeaviateOASIS(http, "https://fixture.invalid") { IsProviderActivated = true };
        var result = await provider.ActivateProviderAsync();
        Assert.AreEqual(mode is "existing" or "missing", !result.IsError);
        Assert.AreEqual(!result.IsError, provider.IsProviderActivated);
        Assert.AreEqual(mode == "missing" ? 3 : mode == "badcreate" ? 1 : 0, creates);
    }

    [TestMethod]
    public async Task CqlRequiresExplicitGrpcConfigurationAndNeverCallsGraphQl()
    {
        var transport = new Transport();
        using var http = new HttpClient(transport);
        var provider = new WeaviateOASIS(http, "https://fixture.invalid") { IsProviderActivated = true };
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.WeaviateOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.WeaviateOASIS, new[] { Guid.NewGuid() }).Result).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.WeaviateOASIS,
            new[] { new CosmicQueryTarget(ProviderType.WeaviateOASIS, provider.QueryCapabilities) }).Result;
        var result = await provider.ExecuteCosmicQueryAsync(plan);
        Assert.AreEqual("CQL_PROVIDER_CONFIGURATION_REQUIRED", result.ErrorCode);
        Assert.IsNull(result.Result);
        Assert.AreEqual(0, transport.Calls);
        provider.IsVersionControlEnabled = true;
        result = await provider.ExecuteCosmicQueryAsync(plan);
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode);
    }
}
