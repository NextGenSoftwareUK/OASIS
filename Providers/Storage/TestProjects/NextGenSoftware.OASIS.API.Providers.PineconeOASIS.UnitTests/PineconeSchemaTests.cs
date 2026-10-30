using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PineconeOASIS.UnitTests;

[TestClass]
public class PineconeSchemaTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public string Mode = "cosine";
        public int Calls;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/describe_index_stats", request.RequestUri!.AbsolutePath);
            Assert.AreEqual("2026-07", request.Headers.GetValues("X-Pinecone-API-Version").Single());
            Assert.AreEqual("{}", await request.Content!.ReadAsStringAsync(token));
            if (Mode == "disconnect") throw new HttpRequestException("Offline");
            var json = Mode == "oversized" ? JsonSerializer.Serialize(new { padding = new string('x', 262145) }) :
                JsonSerializer.Serialize(new
                {
                    dimension = Mode == "dimension" ? 2 : 1,
                    vectorType = Mode == "sparse" ? "sparse" : "dense",
                    metric = Mode == "metric" ? "unknown" : Mode == "dotproduct" ? "dotproduct" : Mode == "euclidean" ? "euclidean" : "cosine",
                    totalVectorCount = Mode == "count" ? -1L : long.MaxValue,
                    indexFullness = Mode == "fullness" ? 1.1 : 0,
                    namespaces = Mode == "shape" ? (object)Array.Empty<string>() : new Dictionary<string, object>
                    { ["holons"] = new { vectorCount = Mode == "namespace" ? -1L : long.MaxValue } }
                });
            return new HttpResponseMessage(Mode == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent(json) };
        }
    }

    [DataTestMethod]
    [DataRow("cosine")]
    [DataRow("dotproduct")]
    [DataRow("euclidean")]
    [DataRow("dimension")]
    [DataRow("sparse")]
    [DataRow("metric")]
    [DataRow("count")]
    [DataRow("fullness")]
    [DataRow("shape")]
    [DataRow("namespace")]
    [DataRow("oversized")]
    [DataRow("http")]
    [DataRow("disconnect")]
    public async Task ActivationValidatesNativeDenseIndexAndBoundsWithoutCreatingStorage(string mode)
    {
        var transport = new Transport { Mode = mode };
        using var http = new HttpClient(transport);
        var provider = new PineconeOASIS(http, "http://native.invalid", "test-key") { IsProviderActivated = true };
        var result = await provider.ActivateProviderAsync();
        var success = mode is "cosine" or "dotproduct" or "euclidean";
        Assert.AreEqual(success, !result.IsError, result.Message);
        Assert.AreEqual(success, provider.IsProviderActivated);
        Assert.AreEqual(1, transport.Calls, "No create or fallback request is permitted.");
        if (!success) Assert.IsNotNull(result.Exception);
    }

    [TestMethod]
    public async Task FailedReactivationClearsPriorActivation()
    {
        var transport = new Transport();
        using var http = new HttpClient(transport);
        var provider = new PineconeOASIS(http, "http://native.invalid", "test-key");
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        transport.Mode = "http";
        Assert.IsTrue((await provider.ActivateProviderAsync()).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual(2, transport.Calls);
    }
}
