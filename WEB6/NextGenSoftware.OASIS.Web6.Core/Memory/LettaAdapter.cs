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
    /// External memory adapter for Letta (formerly MemGPT). Stateful agent memory with self-editing context
    /// windows. Set LETTA_BASE_URL and LETTA_API_KEY environment variables.
    /// </summary>
    public class LettaAdapter : IExternalMemoryProvider
    {
        private static readonly HttpClient _http = new HttpClient();
        private readonly string _baseUrl;
        private readonly string _apiKey;

        public string Name => "Letta";

        public LettaAdapter(string baseUrl = null, string apiKey = null)
        {
            _baseUrl = (baseUrl ?? Environment.GetEnvironmentVariable("LETTA_BASE_URL") ?? "http://localhost:8283").TrimEnd('/');
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("LETTA_API_KEY");
        }

        public async Task<List<MemoryEntry>> SearchAsync(Guid avatarId, string query, int topK = 5)
        {
            // Letta uses an agent per user — agent_id can be derived from avatarId or stored in DNA
            string agentId = avatarId.ToString();
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/agents/{agentId}/memory/messages/search");
            Authorize(req);
            req.Content = new StringContent(JsonSerializer.Serialize(new { query, limit = topK }), Encoding.UTF8, "application/json");
            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return new List<MemoryEntry>();
            string body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var entries = new List<MemoryEntry>();
            foreach (var item in doc.RootElement.EnumerateArray())
                entries.Add(new MemoryEntry
                {
                    Id = item.TryGetProperty("id", out var id) ? id.GetString() : null,
                    Content = item.TryGetProperty("text", out var t) ? t.GetString() : null,
                    Score = 0.8
                });
            return entries;
        }

        public async Task AddAsync(Guid avatarId, string content, Dictionary<string, string> metadata = null)
        {
            string agentId = avatarId.ToString();
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/agents/{agentId}/messages");
            Authorize(req);
            req.Content = new StringContent(JsonSerializer.Serialize(new
            {
                messages = new[] { new { role = "user", content } }
            }), Encoding.UTF8, "application/json");
            await _http.SendAsync(req);
        }

        public async Task DeleteAsync(Guid avatarId, string memoryId)
        {
            using var req = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/v1/messages/{memoryId}");
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
