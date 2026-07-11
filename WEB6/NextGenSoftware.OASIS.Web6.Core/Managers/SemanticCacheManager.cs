using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// In-memory semantic response cache.
    /// On each completion request: embed the last user message, cosine-compare against cached embeddings,
    /// return cached response on hit, store on miss. LRU eviction at MaxEntries.
    /// </summary>
    public class SemanticCacheManager : OASISManager
    {
        private sealed class CacheEntry
        {
            public float[]           Embedding  { get; set; }
            public CompletionResponse Response  { get; set; }
            public DateTime          ExpiresAt  { get; set; }
            public DateTime          LastUsed   { get; set; } = DateTime.UtcNow;
        }

        private static readonly List<CacheEntry>       _cache = new List<CacheEntry>();
        private static readonly SemaphoreSlim           _lock  = new SemaphoreSlim(1, 1);

        private readonly EmbeddingManager _embedder;

        public SemanticCacheManager(Guid avatarId, OASISDNA dna = null) : base(avatarId, dna)
        {
            _embedder = new EmbeddingManager(avatarId, dna);
        }

        /// <summary>Looks up the cache for a semantically equivalent prior response. Returns null on miss or if cache is disabled.</summary>
        public async Task<CompletionResponse> GetAsync(CompletionRequest request)
        {
            int ttl = request.CacheTtlSeconds ?? OASISDNA?.OASIS?.Web6?.Cache?.DefaultTtlSeconds ?? 3600;
            if (ttl == 0) return null;

            string queryText = ExtractUserMessage(request);
            if (string.IsNullOrWhiteSpace(queryText)) return null;

            float[] queryEmbedding = await EmbedAsync(queryText);
            if (queryEmbedding == null) return null;

            double threshold = request.CacheSimilarityThreshold ?? OASISDNA?.OASIS?.Web6?.Cache?.SimilarityThreshold ?? 0.95;

            await _lock.WaitAsync();
            try
            {
                DateTime now = DateTime.UtcNow;
                CacheEntry best = null;
                double bestScore = 0;

                foreach (CacheEntry entry in _cache)
                {
                    if (entry.ExpiresAt < now) continue;
                    double score = CosineSimilarity(queryEmbedding, entry.Embedding);
                    if (score >= threshold && score > bestScore)
                    {
                        bestScore = score;
                        best = entry;
                    }
                }

                if (best != null)
                {
                    best.LastUsed = now;
                    CompletionResponse cached = Clone(best.Response);
                    cached.FromCache = true;
                    return cached;
                }
            }
            finally { _lock.Release(); }

            return null;
        }

        /// <summary>Stores a completed response in the cache with its embedding and TTL.</summary>
        public async Task SetAsync(CompletionRequest request, CompletionResponse response)
        {
            int ttl = request.CacheTtlSeconds ?? OASISDNA?.OASIS?.Web6?.Cache?.DefaultTtlSeconds ?? 3600;
            if (ttl == 0) return;

            string queryText = ExtractUserMessage(request);
            if (string.IsNullOrWhiteSpace(queryText)) return;

            float[] embedding = await EmbedAsync(queryText);
            if (embedding == null) return;

            int maxEntries = OASISDNA?.OASIS?.Web6?.Cache?.MaxEntries ?? 1000;

            await _lock.WaitAsync();
            try
            {
                // LRU eviction
                while (_cache.Count >= maxEntries)
                {
                    CacheEntry lru = _cache.OrderBy(e => e.LastUsed).First();
                    _cache.Remove(lru);
                }

                _cache.Add(new CacheEntry
                {
                    Embedding = embedding,
                    Response  = Clone(response),
                    ExpiresAt = DateTime.UtcNow.AddSeconds(ttl),
                });
            }
            finally { _lock.Release(); }
        }

        private async Task<float[]> EmbedAsync(string text)
        {
            try
            {
                var result = await _embedder.EmbedAsync(new EmbeddingRequest { Texts = new List<string> { text } });
                return result.IsError || result.Result?.Embeddings?.Count == 0 ? null : result.Result.Embeddings[0];
            }
            catch { return null; }
        }

        private static string ExtractUserMessage(CompletionRequest req)
        {
            for (int i = req.Messages.Count - 1; i >= 0; i--)
                if (string.Equals(req.Messages[i].Role, "user", StringComparison.OrdinalIgnoreCase))
                    return req.Messages[i].Content;
            return null;
        }

        private static double CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length) return 0;
            double dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++) { dot += a[i] * b[i]; magA += a[i] * a[i]; magB += b[i] * b[i]; }
            double denom = Math.Sqrt(magA) * Math.Sqrt(magB);
            return denom == 0 ? 0 : dot / denom;
        }

        private static CompletionResponse Clone(CompletionResponse r) => new CompletionResponse
        {
            Id               = r.Id,
            Provider         = r.Provider,
            Model            = r.Model,
            Content          = r.Content,
            PromptTokens     = r.PromptTokens,
            CompletionTokens = r.CompletionTokens,
            LatencyMs        = r.LatencyMs,
            FailedOver       = r.FailedOver,
            EstimatedCostUSD = r.EstimatedCostUSD,
        };
    }
}
