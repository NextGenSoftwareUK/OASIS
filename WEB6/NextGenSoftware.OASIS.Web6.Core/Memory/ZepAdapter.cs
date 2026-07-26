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
    /// External memory adapter for Zep (https://getzep.com). Graph-based long-term memory strong for
    /// dialog history and entity extraction. Set ZEP_API_KEY environment variable.
    /// </summary>
    public class ZepAdapter : IExternalMemoryProvider
    {
        private static readonly HttpClient _http = new HttpClient();
        private readonly string _baseUrl;
        private readonly string _apiKey;

        public string Name => "Zep";

        public ZepAdapter(string baseUrl = null, string apiKey = null)
        {
            _baseUrl = (baseUrl ?? Environment.GetEnvironmentVariable("ZEP_BASE_URL") ?? "https://api.getzep.com").TrimEnd('/');
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("ZEP_API_KEY");
        }

        public async Task<List<MemoryEntry>> SearchAsync(Guid avatarId, string query, int topK = 5)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/v2/users/{avatarId}/memory/search");
            Authorize(req);
            req.Content = new StringContent(JsonSerializer.Serialize(new { text = query, limit = topK }), Encoding.UTF8, "application/json");
            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return new List<MemoryEntry>();
            string body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var entries = new List<MemoryEntry>();
            if (doc.RootElement.TryGetProperty("results", out var results))
                foreach (var item in results.EnumerateArray())
                    entries.Add(new MemoryEntry
                    {
                        Id = item.TryGetProperty("uuid", out var id) ? id.GetString() : null,
                        Content = item.TryGetProperty("content", out var c) ? c.GetString() : null,
                        Score = item.TryGetProperty("score", out var sc) ? sc.GetDouble() : 0
                    });
            return entries;
        }

        public async Task AddAsync(Guid avatarId, string content, Dictionary<string, string> metadata = null)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/v2/users/{avatarId}/memory");
            Authorize(req);
            req.Content = new StringContent(JsonSerializer.Serialize(new
            {
                messages = new[] { new { role = "user", content } }
            }), Encoding.UTF8, "application/json");
            await _http.SendAsync(req);
        }

        public async Task DeleteAsync(Guid avatarId, string memoryId)
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/api/v2/users/{avatarId}/memory/{memoryId}");
            Authorize(req);
            await _http.SendAsync(req);
        }

        private void Authorize(HttpRequestMessage req)
        {
            if (!string.IsNullOrEmpty(_apiKey))
                req.Headers.Authorization = new AuthenticationHeaderValue("Api-Key", _apiKey);
        }
    }
}
