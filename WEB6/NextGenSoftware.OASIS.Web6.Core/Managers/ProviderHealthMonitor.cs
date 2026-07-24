using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    public class ProviderHealthStatus
    {
        public string Provider { get; set; }
        public bool Healthy { get; set; }
        public long? LatencyMs { get; set; }
        public string Error { get; set; }
        public DateTime LastCheckedUtc { get; set; }
    }

    /// <summary>
    /// Pings each configured AI provider periodically and caches health/latency results.
    /// Backs GET /v1/providers/status.
    /// </summary>
    public class ProviderHealthMonitor
    {
        private static readonly ConcurrentDictionary<string, ProviderHealthStatus> _cache = new();
        private static DateTime _lastFullCheck = DateTime.MinValue;
        private static readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);

        private readonly AIProviderManager _providerManager;

        public ProviderHealthMonitor(Guid avatarId, OASISDNA OASISDNA = null)
        {
            _providerManager = new AIProviderManager(avatarId, OASISDNA);
        }

        public async Task<List<ProviderHealthStatus>> GetStatusAsync(bool forceRefresh = false)
        {
            if (forceRefresh || (DateTime.UtcNow - _lastFullCheck) > _checkInterval)
                await RefreshAllAsync();

            return new List<ProviderHealthStatus>(_cache.Values);
        }

        private async Task RefreshAllAsync()
        {
            _lastFullCheck = DateTime.UtcNow;

            var providers = new[]
            {
                AIProviderType.OpenAI, AIProviderType.Anthropic, AIProviderType.Gemini,
                AIProviderType.Groq, AIProviderType.Mistral, AIProviderType.Cohere,
                AIProviderType.XAI, AIProviderType.DeepSeek, AIProviderType.HuggingFace,
                AIProviderType.Cerebras, AIProviderType.TogetherAI, AIProviderType.Perplexity
            };

            var tasks = new List<Task>();
            foreach (var p in providers)
                tasks.Add(PingAsync(p));

            await Task.WhenAll(tasks);
        }

        private async Task PingAsync(AIProviderType provider)
        {
            string name = provider.ToString();
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                var req = new CompletionRequest
                {
                    Provider = name.ToLowerInvariant(),
                    MaxTokens = 5,
                    Messages = new List<ChatMessage> { new ChatMessage { Role = "user", Content = "Reply with: OK" } }
                };
                var result = await _providerManager.CompleteAsync(req);
                sw.Stop();

                _cache[name] = new ProviderHealthStatus
                {
                    Provider = name,
                    Healthy = !result.IsError,
                    LatencyMs = sw.ElapsedMilliseconds,
                    Error = result.IsError ? result.Message : null,
                    LastCheckedUtc = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                _cache[name] = new ProviderHealthStatus
                {
                    Provider = name,
                    Healthy = false,
                    LatencyMs = null,
                    Error = ex.Message,
                    LastCheckedUtc = DateTime.UtcNow
                };
            }
        }
    }
}
