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
    /// External memory adapter for Mem0 (https://mem0.ai). Manages per-user/per-session/per-agent memories
    /// via the Mem0 REST API. Set MEM0_API_KEY environment variable.
    /// </summary>
    public class Mem0Adapter : IExternalMemoryProvider
    {
        private static readonly HttpClient _http = new HttpClient();
        private readonly string _apiKey;
        private const string BaseUrl = "https://api.mem0.ai/v1";

        public string Name => "Mem0";

        public Mem0Adapter(string apiKey = null)
        {
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("MEM0_API_KEY");
        }

        public async Task<List<MemoryEntry>> SearchAsync(Guid avatarId, string query, int topK = 5)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/memories/search/");
            Authorize(req);
            req.Content = new StringContent(JsonSerializer.Serialize(new { query, user_id = avatarId.ToString(), limit = topK }), Encoding.UTF8, "application/json");
            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return new List<MemoryEntry>();
            string body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var entries = new List<MemoryEntry>();
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                entries.Add(new MemoryEntry
                {
                    Id = item.TryGetProperty("id", out var id) ? id.GetString() : null,
                    Content = item.TryGetProperty("memory", out var mem) ? mem.GetString() : null,
                    Score = item.TryGetProperty("score", out var sc) ? sc.GetDouble() : 0
                });
            }
            return entries;
        }

        public async Task AddAsync(Guid avatarId, string content, Dictionary<string, string> metadata = null)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/memories/");
            Authorize(req);
            req.Content = new StringContent(JsonSerializer.Serialize(new
            {
                messages = new[] { new { role = "user", content } },
                user_id = avatarId.ToString(),
                metadata
            }), Encoding.UTF8, "application/json");
            await _http.SendAsync(req);
        }

        public async Task DeleteAsync(Guid avatarId, string memoryId)
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/memories/{memoryId}/");
            Authorize(req);
            await _http.SendAsync(req);
        }

        private void Authorize(HttpRequestMessage req)
        {
            if (!string.IsNullOrEmpty(_apiKey))
                req.Headers.Authorization = new AuthenticationHeaderValue("Token", _apiKey);
        }
    }
}
