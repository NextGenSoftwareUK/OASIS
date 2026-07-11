using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// Generates float embeddings via OpenAI, Cohere, or HuggingFace.
    /// </summary>
    public class EmbeddingManager : OASISManager
    {
        private static readonly HttpClient _http = new HttpClient();

        public EmbeddingManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }
        public EmbeddingManager(IOASISStorageProvider provider, Guid avatarId, OASISDNA OASISDNA = null) : base(provider, avatarId, OASISDNA) { }

        public async Task<OASISResult<EmbeddingResponse>> EmbedAsync(EmbeddingRequest request)
        {
            OASISResult<EmbeddingResponse> result = new OASISResult<EmbeddingResponse>();

            if (request?.Texts == null || request.Texts.Count == 0)
            {
                OASISErrorHandling.HandleError(ref result, "EmbeddingRequest.Texts is required.");
                return result;
            }

            string provider = ResolveProvider(request.Provider);

            try
            {
                result.Result = provider switch
                {
                    "cohere" => await EmbedCohereAsync(request),
                    "huggingface" => await EmbedHuggingFaceAsync(request),
                    _ => await EmbedOpenAIAsync(request)
                };
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating embeddings via {provider}. Reason: {ex.Message}", ex);
            }

            return result;
        }

        private string ResolveProvider(string requested)
        {
            if (string.IsNullOrEmpty(requested) || requested == "auto")
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))) return "openai";
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COHERE_API_KEY"))) return "cohere";
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HUGGINGFACE_API_KEY"))) return "huggingface";
                return "openai";
            }
            return requested.ToLowerInvariant();
        }

        private async Task<EmbeddingResponse> EmbedOpenAIAsync(EmbeddingRequest request)
        {
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? OASISDNA?.OASIS?.Web6?.ApiKeys?.OpenAI;
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("No API key configured for OpenAI (OPENAI_API_KEY).");

            string model = request.Model == "auto" ? "text-embedding-3-small" : request.Model;
            var payload = new { model, input = request.Texts };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req);
            string body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"OpenAI embeddings: {body}");

            using var doc = JsonDocument.Parse(body);
            var embeddings = doc.RootElement.GetProperty("data")
                .EnumerateArray()
                .Select(e => e.GetProperty("embedding").EnumerateArray().Select(f => f.GetSingle()).ToArray())
                .ToList();
            int tokens = doc.RootElement.TryGetProperty("usage", out var u) && u.TryGetProperty("total_tokens", out var t) ? t.GetInt32() : 0;

            return new EmbeddingResponse { Provider = "openai", Model = model, Embeddings = embeddings, TotalTokens = tokens };
        }

        private async Task<EmbeddingResponse> EmbedCohereAsync(EmbeddingRequest request)
        {
            string apiKey = Environment.GetEnvironmentVariable("COHERE_API_KEY")
                ?? OASISDNA?.OASIS?.Web6?.ApiKeys?.Cohere;
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("No API key configured for Cohere (COHERE_API_KEY).");

            string model = request.Model == "auto" ? "embed-english-v3.0" : request.Model;
            var payload = new { model, texts = request.Texts, input_type = "search_document" };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.cohere.com/v2/embed");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req);
            string body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"Cohere embeddings: {body}");

            using var doc = JsonDocument.Parse(body);
            var embeddings = doc.RootElement.GetProperty("embeddings").GetProperty("float")
                .EnumerateArray()
                .Select(e => e.EnumerateArray().Select(f => f.GetSingle()).ToArray())
                .ToList();

            return new EmbeddingResponse { Provider = "cohere", Model = model, Embeddings = embeddings };
        }

        private async Task<EmbeddingResponse> EmbedHuggingFaceAsync(EmbeddingRequest request)
        {
            string apiKey = Environment.GetEnvironmentVariable("HUGGINGFACE_API_KEY")
                ?? OASISDNA?.OASIS?.Web6?.ApiKeys?.HuggingFace;
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("No API key configured for HuggingFace (HUGGINGFACE_API_KEY).");

            string model = request.Model == "auto" ? "sentence-transformers/all-MiniLM-L6-v2" : request.Model;
            var payload = new { model, inputs = request.Texts };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://router.huggingface.co/v1/embeddings");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req);
            string body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new HttpRequestException($"HuggingFace embeddings: {body}");

            using var doc = JsonDocument.Parse(body);
            var embeddings = doc.RootElement.GetProperty("data")
                .EnumerateArray()
                .Select(e => e.GetProperty("embedding").EnumerateArray().Select(f => f.GetSingle()).ToArray())
                .ToList();

            return new EmbeddingResponse { Provider = "huggingface", Model = model, Embeddings = embeddings };
        }
    }
}
