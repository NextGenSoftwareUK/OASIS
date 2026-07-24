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
    /// External memory adapter for Graphiti (temporal knowledge-graph memory by Zep team).
    /// Set GRAPHITI_BASE_URL and optionally GRAPHITI_API_KEY environment variables.
    /// </summary>
    public class GraphitiAdapter : IExternalMemoryProvider
    {
        private static readonly HttpClient _http = new HttpClient();
        private readonly string _baseUrl;
        private readonly string _apiKey;

        public string Name => "Graphiti";

        public GraphitiAdapter(string baseUrl = null, string apiKey = null)
        {
            _baseUrl = (baseUrl ?? Environment.GetEnvironmentVariable("GRAPHITI_BASE_URL") ?? "http://localhost:8000").TrimEnd('/');
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("GRAPHITI_API_KEY");
        }

        public async Task<List<MemoryEntry>> SearchAsync(Guid avatarId, string query, int topK = 5)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/search");
            Authorize(req);
            req.Content = new StringContent(JsonSerializer.Serialize(new
            {
                query,
                user_id = avatarId.ToString(),
                num_results = topK
            }), Encoding.UTF8, "application/json");
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
                        Content = item.TryGetProperty("fact", out var f) ? f.GetString()
                               : item.TryGetProperty("content", out var c) ? c.GetString() : null,
                        Score = item.TryGetProperty("score", out var sc) ? sc.GetDouble() : 0.75
                    });
            return entries;
        }

        public async Task AddAsync(Guid avatarId, string content, Dictionary<string, string> metadata = null)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/episodes");
            Authorize(req);
            req.Content = new StringContent(JsonSerializer.Serialize(new
            {
                name = "oasis_memory",
                episode_body = content,
                source = "text",
                source_description = "OASIS WEB6",
                group_id = avatarId.ToString()
            }), Encoding.UTF8, "application/json");
            await _http.SendAsync(req);
        }

        public async Task DeleteAsync(Guid avatarId, string memoryId)
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/episodes/{memoryId}");
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
