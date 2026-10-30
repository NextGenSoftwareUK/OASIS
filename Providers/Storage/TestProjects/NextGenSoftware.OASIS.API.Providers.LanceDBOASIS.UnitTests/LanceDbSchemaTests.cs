using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.LanceDBOASIS.UnitTests;

[TestClass]
public class LanceDbSchemaTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public string Mode = "success";
        public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith("/describe/"), "No create/overwrite request allowed.");
            CollectionAssert.Contains(new[] { "/v1/table/oasis_avatars/describe/", "/v1/table/oasis_avatar_details/describe/", "/v1/table/oasis_holons/describe/" }, request.RequestUri.AbsolutePath);
            Assert.AreEqual("{\"version\":null}", await request.Content!.ReadAsStringAsync(token));
            var fields = new JsonArray();
            foreach (var name in new[] { "id", "doc", "username", "email", "parent_holon_id" })
                fields.Add(JsonSerializer.SerializeToNode(new { name, type = new { type = "string" }, nullable = false }));
            fields.Add(JsonSerializer.SerializeToNode(new
            {
                name = "vector", nullable = false,
                type = new { type = "fixed_size_list", length = 1, fields = new[] { new { name = "item", nullable = true, type = new { type = "float" } } } }
            }));
            var root = new JsonObject { ["version"] = JsonValue.Create(ulong.MaxValue), ["schema"] = new JsonObject { ["fields"] = fields } };
            if (Mode == "version") root["version"] = 0;
            if (Mode == "count") fields.RemoveAt(5);
            if (Mode == "name") fields[0]!["name"] = "other";
            if (Mode == "nullable") fields[0]!["nullable"] = true;
            if (Mode == "type") fields[1]!["type"]!["type"] = "binary";
            if (Mode == "primitive") fields[1]!["type"]!["length"] = 1;
            if (Mode == "vector") fields[5]!["type"]!["type"] = "list";
            if (Mode == "length") fields[5]!["type"]!["length"] = 2;
            if (Mode == "child") fields[5]!["type"]!["fields"]![0]!["type"]!["type"] = "double";
            if (Mode == "childname") fields[5]!["type"]!["fields"]![0]!["name"] = "other";
            if (Mode == "childnullable") fields[5]!["type"]!["fields"]![0]!["nullable"] = false;
            var json = Mode == "oversized" ? JsonSerializer.Serialize(new { padding = new string('x', 262145) }) : root.ToJsonString();
            return new HttpResponseMessage(Mode == "http" ? HttpStatusCode.NotFound : HttpStatusCode.OK)
            { Content = new StringContent(json, Encoding.UTF8, Mode == "media" ? "text/plain" : "application/json") };
        }
    }

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("version")]
    [DataRow("count")]
    [DataRow("name")]
    [DataRow("nullable")]
    [DataRow("type")]
    [DataRow("primitive")]
    [DataRow("vector")]
    [DataRow("length")]
    [DataRow("child")]
    [DataRow("childname")]
    [DataRow("childnullable")]
    [DataRow("oversized")]
    [DataRow("http")]
    [DataRow("media")]
    public async Task ActivationRequiresNativeVersionAndExactArrowTableSchema(string mode)
    {
        var transport = new Transport { Mode = mode };
        using var http = new HttpClient(transport);
        var provider = new LanceDBOASIS(http, "http://native.invalid", "test-key", "test-project") { IsProviderActivated = true };
        var result = await provider.ActivateProviderAsync();
        Assert.AreEqual(mode == "success", !result.IsError, result.Message);
        Assert.AreEqual(mode == "success", provider.IsProviderActivated);
        Assert.AreEqual(mode == "success" ? 3 : 1, transport.Calls);
        if (mode != "success") Assert.IsNotNull(result.Exception);
    }

    [TestMethod]
    public async Task FailedReactivationClearsPreviouslyActiveState()
    {
        var transport = new Transport();
        using var http = new HttpClient(transport);
        var provider = new LanceDBOASIS(http, "http://native.invalid", "test-key", "test-project");
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        transport.Mode = "http";
        Assert.IsTrue((await provider.ActivateProviderAsync()).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual(4, transport.Calls);
    }
}
