using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS.UnitTests;

[TestClass]
public class OpenVinoMetadataTests
{
    [DataTestMethod]
    [DataRow("oversized")]
    [DataRow("utf8")]
    [DataRow("duplicate")]
    [DataRow("deep")]
    [DataRow("error")]
    [DataRow("media")]
    [DataRow("array")]
    [DataRow("http")]
    public async Task InvalidMetadataFailsAtTheOneSdkBoundary(string fault)
    {
        using var handler = new MetadataReplies { Fault = fault };
        using var client = new IntelOpenVINOOASIS("http://127.0.0.1:28000/", handler);
        Assert.IsTrue((await client.GetServerMetadataAsync()).IsError);
        Assert.IsTrue((await client.GetModelMetadataAsync("cosmic_dummy", "1")).IsError);
        Assert.AreEqual(2, handler.Calls, "One request per operation; no retry or alternate endpoint.");
    }

    [TestMethod]
    public async Task NativeAnnotationsSurviveMetadataCloneWithoutGuessingTensorTypes()
    {
        using var handler = new MetadataReplies();
        using var client = new IntelOpenVINOOASIS("http://127.0.0.1:28000/", handler);
        var result = await client.GetModelMetadataAsync("cosmic_dummy", "1");
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual("FP32", result.Result.GetProperty("inputs")[0].GetProperty("datatype").GetString());
        Assert.AreEqual("FP16", result.Result.GetProperty("rt_info").GetProperty("model_info").GetProperty("precision").GetString());
        Assert.AreEqual(3, result.Result.GetProperty("versions").GetArrayLength());
    }

    [TestMethod]
    public async Task ReadinessIsBoundedAndOnlyHttp200MeansReady()
    {
        using var oversized = new MetadataReplies { Fault = "ready-oversized" };
        using var first = new IntelOpenVINOOASIS("http://127.0.0.1:28000/", oversized);
        Assert.IsTrue((await first.IsModelReadyAsync("cosmic_dummy", "1")).IsError);
        using var empty = new MetadataReplies { Fault = "ready-204" };
        using var second = new IntelOpenVINOOASIS("http://127.0.0.1:28000/", empty);
        var result = await second.IsModelReadyAsync("cosmic_dummy", "1");
        Assert.IsFalse(result.IsError); Assert.IsFalse(result.Result);
    }

    [TestMethod]
    public async Task CancelledMetadataDoesNotReachTheHandler()
    {
        using var handler = new MetadataReplies();
        using var client = new IntelOpenVINOOASIS("http://127.0.0.1:28000/", handler);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.IsTrue((await client.GetServerMetadataAsync(cancelled.Token)).IsError);
        Assert.AreEqual(0, handler.Calls);
    }

    [TestMethod, TestCategory("OpenVinoClientNative")]
    public async Task OpenVinoClientNativeMetadataAndInferenceUseRealOwnedModelVersions()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_OPENVINO_CLIENT_NATIVE") != "owned-loopback-28000")
        { Assert.Inconclusive("Explicitly owned OVMS cloud fixture required."); return; }
        var path = Environment.GetEnvironmentVariable("COSMIC_OPENVINO_PIN_FILE");
        Assert.IsTrue(Path.IsPathFullyQualified(path));
        using var pinsDocument = JsonDocument.Parse(File.ReadAllBytes(path));
        var pins = pinsDocument.RootElement; var model = pins.GetProperty("modelName").GetString();
        using var client = new IntelOpenVINOOASIS("http://127.0.0.1:28000/");
        var server = await client.GetServerMetadataAsync(); Assert.IsFalse(server.IsError, server.Message);
        Assert.AreEqual("OpenVINO Model Server", server.Result.GetProperty("name").GetString());
        Assert.AreEqual(pins.GetProperty("serverVersion").GetString(), server.Result.GetProperty("version").GetString());
        foreach (var version in new[] { "1", "2", "3" })
        {
            var ready = await client.IsModelReadyAsync(model, version); Assert.IsFalse(ready.IsError, ready.Message); Assert.IsTrue(ready.Result);
            var metadata = await client.GetModelMetadataAsync(model, version); Assert.IsFalse(metadata.IsError, metadata.Message);
            Assert.AreEqual(model, metadata.Result.GetProperty("name").GetString());
            CollectionAssert.AreEquivalent(new[] { "1", "2", "3" }, metadata.Result.GetProperty("versions").EnumerateArray().Select(x => x.GetString()).ToArray());
            Assert.AreEqual("b", metadata.Result.GetProperty("inputs")[0].GetProperty("name").GetString());
            Assert.AreEqual("FP32", metadata.Result.GetProperty("inputs")[0].GetProperty("datatype").GetString());
            var result = await client.InferAsync(model, new[] { OpenVinoTensor.Fp32("b", new long[] { 1, 10 }, Enumerable.Range(0, 10).Select(x => (float)x)) }, version);
            Assert.IsFalse(result.IsError, result.Message); Assert.AreEqual(model, result.Result.ModelName); Assert.AreEqual(version, result.Result.ModelVersion);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Result.Id));
            var output = result.Result.Outputs.Single(); Assert.AreEqual("a", output.Name); Assert.AreEqual("FP32", output.Datatype);
            CollectionAssert.AreEqual(new long[] { 1, 10 }, output.Shape);
            CollectionAssert.AreEqual(Enumerable.Range(1, 10).Select(x => (float)x).ToArray(), output.Data.EnumerateArray().Select(x => x.GetSingle()).ToArray());
        }
    }

    private sealed class MetadataReplies : HttpMessageHandler
    {
        public string Fault { get; init; }
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls); ct.ThrowIfCancellationRequested(); Assert.AreEqual(HttpMethod.Get, request.Method);
            var isReady = request.RequestUri.AbsolutePath.EndsWith("/ready", StringComparison.Ordinal);
            var text = "{\"name\":\"cosmic_dummy\",\"versions\":[\"1\",\"2\",\"3\"],\"inputs\":[{\"name\":\"b\",\"datatype\":\"FP32\",\"shape\":[1,10]}],\"rt_info\":{\"model_info\":{\"precision\":\"FP16\"}}}";
            if (Fault == "duplicate") text = "{\"name\":\"first\",\"name\":\"second\"}";
            if (Fault == "deep") text = new string('[', 40) + "0" + new string(']', 40);
            if (Fault == "error") text = "{\"error\":\"native failure\"}";
            if (Fault == "array") text = "[]";
            var bytes = Fault == "oversized" ? new byte[65537] : Fault == "ready-oversized" ? new byte[1025]
                : Fault == "utf8" ? new byte[] { 255 } : isReady ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(text);
            var response = new HttpResponseMessage(Fault == "http" ? HttpStatusCode.ServiceUnavailable : Fault == "ready-204" ? HttpStatusCode.NoContent : HttpStatusCode.OK)
                { Content = new ByteArrayContent(bytes) };
            response.Content.Headers.ContentType = new(Fault == "media" ? "text/html" : "application/json");
            return Task.FromResult(response);
        }
    }
}
