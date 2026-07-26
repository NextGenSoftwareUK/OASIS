using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Web6.Core.Memory
{
    /// <summary>
    /// External memory adapter for LangMem (LangGraph-native memory provider).
    /// Set LANGMEM_API_KEY and optionally LANGMEM_BASE_URL environment variables.
    /// </summary>
    public class LangMemAdapter : IExternalMemoryProvider
    {
        private static readonly HttpClient _http = new HttpClient();
        private readonly string _baseUrl;
        private readonly string _apiKey;

        public string Name => "LangMem";

        public LangMemAdapter(string baseUrl = null, string apiKey = null)
        {
            _baseUrl = (baseUrl ?? Environment.GetEnvironmentVariable("LANGMEM_BASE_URL") ?? "https://api.langchain.com/langmem").TrimEnd('/');
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("LANGMEM_API_KEY");
        }

        public async Task<List<MemoryEntry>> SearchAsync(Guid avatarId, string query, int topK = 5)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/memories/search");
            Authorize(req);
            req.Content = new StringContent(JsonSerializer.Serialize(new { query, user_id = avatarId.ToString(), limit = topK }), Encoding.UTF8, "application/json");
            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return new List<MemoryEntry>();
            string body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var entries = new List<MemoryEntry>();
            if (doc.RootElement.TryGetProperty("memories", out var mems))
                foreach (var item in mems.EnumerateArray())
                    entries.Add(new MemoryEntry
                    {
                        Id = item.TryGetProperty("id", out var id) ? id.GetString() : null,
                        Content = item.TryGetProperty("content", out var c) ? c.GetString() : null,
                        Score = item.TryGetProperty("relevance_score", out var sc) ? sc.GetDouble() : 0.7
                    });
            return entries;
        }

        public async Task AddAsync(Guid avatarId, string content, Dictionary<string, string> metadata = null)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/memories");
            Authorize(req);
            req.Content = new StringContent(JsonSerializer.Serialize(new { content, user_id = avatarId.ToString(), metadata }), Encoding.UTF8, "application/json");
            await _http.SendAsync(req);
        }

        public async Task DeleteAsync(Guid avatarId, string memoryId)
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/memories/{memoryId}");
            Authorize(req);
            await _http.SendAsync(req);
        }

        private void Authorize(HttpRequestMessage req)
        {
            if (!string.IsNullOrEmpty(_apiKey))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }
}
