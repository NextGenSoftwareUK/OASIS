using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS.UnitTests
{
    internal sealed class FakeOvms : HttpMessageHandler
    {
        public readonly List<(string Method, string Path, string Body)> Calls = new();
        public bool Ready = true;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            var path = request.RequestUri.AbsolutePath;
            Calls.Add((request.Method.Method, path, body));

            return path switch
            {
                "/v2/health/ready" => Ready ? Json("") : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
                "/v2/models/resnet/ready" => Json(""),
                "/v2/models/missing/ready" => new HttpResponseMessage(HttpStatusCode.NotFound),
                "/v2/models/resnet/versions/2/infer" => Infer(body),
                "/v3/chat/completions" => Json("{\"model\":\"llama\",\"object\":\"chat.completion\",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"Hello Neo\"},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":5,\"completion_tokens\":2,\"total_tokens\":7}}"),
                "/v3/embeddings" => Json("{\"object\":\"list\",\"data\":[{\"index\":1,\"embedding\":[0.5,0.6]},{\"index\":0,\"embedding\":[0.1,0.2]}]}"),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{\"error\":\"Model with requested name is not found\"}") }
            };
        }

        private static HttpResponseMessage Infer(string body)
        {
            using var doc = JsonDocument.Parse(body);
            var input = doc.RootElement.GetProperty("inputs")[0];
            var sum = input.GetProperty("data").EnumerateArray().Sum(v => v.GetSingle());
            return Json($"{{\"model_name\":\"resnet\",\"model_version\":\"2\",\"id\":\"x\",\"outputs\":[{{\"name\":\"sum\",\"shape\":[1],\"datatype\":\"FP32\",\"data\":[{sum}]}}]}}");
        }

        private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    [TestClass]
    public class IntelOpenVINOOASISTests
    {
        private FakeOvms _ovms;
        private IntelOpenVINOOASIS _provider;

        [TestInitialize]
        public void Init()
        {
            _ovms = new FakeOvms();
            _provider = new IntelOpenVINOOASIS("http://ovms:8000", _ovms);
        }

        [TestMethod]
        public async Task Activation_requires_a_ready_server()
        {
            Assert.IsFalse((await _provider.ActivateProviderAsync()).IsError);
            _ovms.Ready = false;
            Assert.IsTrue((await new IntelOpenVINOOASIS("http://ovms:8000", _ovms).ActivateProviderAsync()).IsError);
        }

        [TestMethod]
        public async Task Model_readiness_reflects_the_server()
        {
            Assert.IsTrue((await _provider.IsModelReadyAsync("resnet")).Result);
            Assert.IsFalse((await _provider.IsModelReadyAsync("missing")).Result);
        }

        [TestMethod]
        public async Task Inference_posts_kserve_v2_tensors_and_parses_outputs()
        {
            var result = await _provider.InferAsync("resnet", new[] { OpenVinoTensor.Fp32("x", new long[] { 3 }, new[] { 1f, 2f, 3f }) }, version: "2");
            Assert.IsFalse(result.IsError, result.Message);
            Assert.AreEqual(6f, result.Result.Outputs.Single().Data[0].GetSingle());

            var sent = JsonDocument.Parse(_ovms.Calls.Last().Body).RootElement.GetProperty("inputs")[0];
            Assert.AreEqual("FP32", sent.GetProperty("datatype").GetString());
            Assert.AreEqual(3, sent.GetProperty("shape")[0].GetInt64());
        }

        [TestMethod]
        public async Task Inference_errors_surface_the_server_message()
        {
            var result = await _provider.InferAsync("nope", new[] { OpenVinoTensor.Fp32("x", new long[] { 1 }, new[] { 1f }) });
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "not found");
        }

        [TestMethod]
        public async Task Chat_and_embeddings_use_the_openai_compatible_endpoints()
        {
            var chat = await _provider.ChatCompletionAsync("llama", new[] { new OpenVinoChatMessage("user", "hi") }, maxTokens: 10);
            Assert.AreEqual("Hello Neo", chat.Result.Content);
            Assert.AreEqual(2, chat.Result.CompletionTokens);
            StringAssert.Contains(_ovms.Calls.Last().Body, "\"max_tokens\":10");

            var embeddings = await _provider.EmbeddingsAsync("bge", new[] { "a", "b" });
            CollectionAssert.AreEqual(new[] { 0.1f, 0.2f }, embeddings.Result[0], "embeddings are returned in input order");
        }
    }
}
