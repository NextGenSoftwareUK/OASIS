using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS
{
    /// <summary>A KServe v2 tensor (request input or response output).</summary>
    public sealed class OpenVinoTensor
    {
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("shape")] public long[] Shape { get; set; }
        [JsonPropertyName("datatype")] public string Datatype { get; set; }
        [JsonPropertyName("data")] public JsonElement Data { get; set; }

        public static OpenVinoTensor Fp32(string name, long[] shape, IEnumerable<float> values)
            => new() { Name = name, Shape = shape, Datatype = "FP32", Data = JsonSerializer.SerializeToElement(values.ToArray()) };
    }

    public sealed class OpenVinoInferResult
    {
        [JsonPropertyName("model_name")] public string ModelName { get; set; }
        [JsonPropertyName("model_version")] public string ModelVersion { get; set; }
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("outputs")] public List<OpenVinoTensor> Outputs { get; set; } = new();
    }

    public sealed record OpenVinoChatMessage(string Role, string Content);

    public sealed class OpenVinoChatResult
    {
        public string Model { get; init; }
        public string Content { get; init; }
        public string FinishReason { get; init; }
        public int PromptTokens { get; init; }
        public int CompletionTokens { get; init; }
    }

    /// <summary>
    /// Intel OpenVINO Model Server (OVMS) provider. OVMS is an inference server, not a data store, so this is an
    /// <see cref="OASISProvider"/> exposing OVMS's KServe v2 API (health, model metadata, tensor inference) and its
    /// OpenAI-compatible endpoints (chat completions, embeddings).
    /// </summary>
    public class IntelOpenVINOOASIS : OASISProvider
    {
        private static readonly JsonSerializerOptions Json = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        private readonly HttpClient _http;

        public IntelOpenVINOOASIS(string baseUrl = "http://localhost:8000") : this(baseUrl, null) { }

        internal IntelOpenVINOOASIS(string baseUrl, HttpMessageHandler handler)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentException("The OVMS REST URL is required.", nameof(baseUrl));
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            ProviderName = "IntelOpenVINOOASIS";
            ProviderDescription = "Intel OpenVINO Model Server provider: KServe v2 inference and OpenAI-compatible chat/embeddings.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.IntelOpenVINOOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.AI);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                using var response = await _http.GetAsync("v2/health/ready");
                if (!response.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"IntelOpenVINOOASIS: OVMS is not ready ({(int)response.StatusCode} {response.ReasonPhrase}).");
                    return result;
                }
                IsProviderActivated = true;
                result.Result = true;
                result.Message = "IntelOpenVINOOASIS activated: OVMS is ready.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"IntelOpenVINOOASIS: cannot reach OVMS: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "IntelOpenVINOOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        private static string ModelPath(string model, string version)
        {
            if (string.IsNullOrWhiteSpace(model)) throw new ArgumentException("A model name is required.", nameof(model));
            return $"v2/models/{Uri.EscapeDataString(model)}" + (string.IsNullOrWhiteSpace(version) ? string.Empty : $"/versions/{Uri.EscapeDataString(version)}");
        }

        private async Task<OASISResult<T>> SendAsync<T>(HttpMethod method, string path, object body, Func<string, T> parse, CancellationToken ct)
        {
            var result = new OASISResult<T>();
            try
            {
                using var request = new HttpRequestMessage(method, path);
                if (body != null) request.Content = JsonContent.Create(body, body.GetType(), options: Json);
                using var response = await _http.SendAsync(request, ct);
                var text = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"IntelOpenVINOOASIS: {method} {path} failed: {(int)response.StatusCode} {response.ReasonPhrase} {text}");
                    return result;
                }
                result.Result = parse(text);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"IntelOpenVINOOASIS: {method} {path} failed: {ex.Message}", ex); }
            return result;
        }

        /// <summary>Server metadata (name, version, extensions) from GET /v2.</summary>
        public Task<OASISResult<JsonElement>> GetServerMetadataAsync(CancellationToken ct = default)
            => SendAsync(HttpMethod.Get, "v2", null, t => JsonDocument.Parse(t).RootElement.Clone(), ct);

        /// <summary>True when the model (and optional version) is loaded and ready.</summary>
        public async Task<OASISResult<bool>> IsModelReadyAsync(string model, string version = null, CancellationToken ct = default)
        {
            var result = new OASISResult<bool>();
            try
            {
                using var response = await _http.GetAsync($"{ModelPath(model, version)}/ready", ct);
                result.Result = response.IsSuccessStatusCode;
                result.Message = result.Result ? $"Model '{model}' is ready." : $"Model '{model}' is not ready ({(int)response.StatusCode}).";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"IntelOpenVINOOASIS: model readiness check failed: {ex.Message}", ex); }
            return result;
        }

        /// <summary>Model metadata (inputs/outputs with names, shapes and datatypes).</summary>
        public Task<OASISResult<JsonElement>> GetModelMetadataAsync(string model, string version = null, CancellationToken ct = default)
            => SendAsync(HttpMethod.Get, ModelPath(model, version), null, t => JsonDocument.Parse(t).RootElement.Clone(), ct);

        /// <summary>Runs KServe v2 inference: POST /v2/models/{model}[/versions/{v}]/infer.</summary>
        public Task<OASISResult<OpenVinoInferResult>> InferAsync(string model, IEnumerable<OpenVinoTensor> inputs, string version = null, IEnumerable<string> requestedOutputs = null, CancellationToken ct = default)
        {
            var inputList = inputs?.ToList();
            if (inputList == null || inputList.Count == 0)
                return Task.FromResult(new OASISResult<OpenVinoInferResult> { IsError = true, Message = "IntelOpenVINOOASIS: at least one input tensor is required." });
            var body = new
            {
                id = Guid.NewGuid().ToString("N"),
                inputs = inputList,
                outputs = requestedOutputs?.Select(n => new { name = n }).ToList()
            };
            return SendAsync(HttpMethod.Post, $"{ModelPath(model, version)}/infer", body, t => JsonSerializer.Deserialize<OpenVinoInferResult>(t), ct);
        }

        /// <summary>OpenAI-compatible chat completion served by OVMS: POST /v3/chat/completions.</summary>
        public Task<OASISResult<OpenVinoChatResult>> ChatCompletionAsync(string model, IEnumerable<OpenVinoChatMessage> messages, int? maxTokens = null, double? temperature = null, CancellationToken ct = default)
        {
            var body = new
            {
                model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                max_tokens = maxTokens,
                temperature,
                stream = false
            };
            return SendAsync(HttpMethod.Post, "v3/chat/completions", body, t =>
            {
                using var doc = JsonDocument.Parse(t);
                var root = doc.RootElement;
                var choice = root.GetProperty("choices")[0];
                root.TryGetProperty("usage", out var usage);
                return new OpenVinoChatResult
                {
                    Model = root.TryGetProperty("model", out var m) ? m.GetString() : model,
                    Content = choice.GetProperty("message").GetProperty("content").GetString(),
                    FinishReason = choice.TryGetProperty("finish_reason", out var f) ? f.GetString() : null,
                    PromptTokens = usage.ValueKind == JsonValueKind.Object ? usage.GetProperty("prompt_tokens").GetInt32() : 0,
                    CompletionTokens = usage.ValueKind == JsonValueKind.Object ? usage.GetProperty("completion_tokens").GetInt32() : 0
                };
            }, ct);
        }

        /// <summary>OpenAI-compatible embeddings served by OVMS: POST /v3/embeddings.</summary>
        public Task<OASISResult<IReadOnlyList<float[]>>> EmbeddingsAsync(string model, IEnumerable<string> inputs, CancellationToken ct = default)
            => SendAsync(HttpMethod.Post, "v3/embeddings", new { model, input = inputs.ToList() }, t =>
            {
                using var doc = JsonDocument.Parse(t);
                return (IReadOnlyList<float[]>)doc.RootElement.GetProperty("data").EnumerateArray()
                    .OrderBy(d => d.GetProperty("index").GetInt32())
                    .Select(d => d.GetProperty("embedding").EnumerateArray().Select(v => v.GetSingle()).ToArray())
                    .ToList();
            }, ct);
    }
}
