using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.ChromaOASIS.UnitTests;

[TestClass]
public class ChromaSchemaTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public string Mode = "empty";
        public int Calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Assert.AreEqual(HttpMethod.Get, request.Method, "Activation never creates or replaces collections.");
            var name = request.RequestUri!.Segments.Last();
            var json = Mode == "oversized" ? JsonSerializer.Serialize(new { padding = new string('x', 256 * 1024 + 1) }) :
                JsonSerializer.Serialize(new
                {
                    id = Mode == "uuid" ? "foreign/path" : "3bbf531e-0f7d-4e8a-9306-c901ae2c7880",
                    name = Mode == "name" ? "other" : name,
                    tenant = Mode == "tenant" ? "other" : "default_tenant",
                    database = Mode == "database" ? "other" : "default_database",
                    dimension = Mode == "empty" ? (int?)null : Mode == "dimension" ? 2 : 1,
                    metadata = new { cosmic_storage_schema = Mode == "schema" ? "other" : "chroma-doc-v1", cosmic_embedding_dimension = Mode == "metadata" ? 2 : 1 }
                });
            return Task.FromResult(new HttpResponseMessage(Mode == "http" ? HttpStatusCode.NotFound : HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }

    [DataTestMethod]
    [DataRow("empty")]
    [DataRow("populated")]
    [DataRow("uuid")]
    [DataRow("name")]
    [DataRow("tenant")]
    [DataRow("database")]
    [DataRow("dimension")]
    [DataRow("schema")]
    [DataRow("metadata")]
    [DataRow("oversized")]
    [DataRow("http")]
    public async Task ExplicitCollectionAuthorityAndDocumentSchemaAreValidated(string mode)
    {
        var transport = new Transport { Mode = mode };
        using var http = new HttpClient(transport);
        var provider = new ChromaOASIS(http) { IsProviderActivated = true };
        var result = await provider.ActivateProviderAsync();
        var success = mode is "empty" or "populated";
        Assert.AreEqual(success, !result.IsError, result.Message);
        Assert.AreEqual(success, provider.IsProviderActivated);
        Assert.AreEqual(success ? 3 : 1, transport.Calls);
        if (!success) Assert.IsNotNull(result.Exception);
    }

    [TestMethod]
    public async Task FailedReactivationDoesNotReuseCachedCollectionOrActiveState()
    {
        var transport = new Transport();
        using var http = new HttpClient(transport);
        var provider = new ChromaOASIS(http);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        transport.Mode = "http";
        Assert.IsTrue((await provider.ActivateProviderAsync()).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual(4, transport.Calls);
    }
}
