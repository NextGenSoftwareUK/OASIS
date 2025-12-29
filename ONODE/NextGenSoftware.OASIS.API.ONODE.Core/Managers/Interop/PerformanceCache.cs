using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Interop;
using NextGenSoftware.OASIS.Common;
using System.Collections;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop
{
    /// <summary>
    /// Performance cache for library invocations
    /// Caches results and tracks performance metrics
    /// Supports LRU (Least Recently Used) and time-based expiration strategies
    /// </summary>
    public class PerformanceCache
    {
        private readonly Dictionary<string, CacheEntry> _cache;
        private readonly Dictionary<string, PerformanceMetrics> _metrics;
        private readonly LinkedList<string> _lruOrder; // For LRU eviction
        private readonly object _lockObject = new object();
        private readonly TimeSpan _defaultCacheExpiry;
        private readonly int _maxCacheSize;
        private readonly CacheStrategy _strategy;

        public enum CacheStrategy
        {
            TimeBased,      // Expire entries after time
            LRU,            // Least Recently Used eviction
            Hybrid          // Both time-based and LRU
        }

        public PerformanceCache(TimeSpan? defaultCacheExpiry = null, int maxCacheSize = 1000, CacheStrategy strategy = CacheStrategy.Hybrid)
        {
            _cache = new Dictionary<string, CacheEntry>();
            _metrics = new Dictionary<string, PerformanceMetrics>();
            _lruOrder = new LinkedList<string>();
            _defaultCacheExpiry = defaultCacheExpiry ?? TimeSpan.FromMinutes(5);
            _maxCacheSize = maxCacheSize;
            _strategy = strategy;
        }

        /// <summary>
        /// Gets cached result if available and not expired
        /// Updates LRU order if using LRU strategy
        /// </summary>
        public OASISResult<T> GetCached<T>(string cacheKey)
        {
            var result = new OASISResult<T>();

            try
            {
                lock (_lockObject)
                {
                    if (_cache.TryGetValue(cacheKey, out var entry))
                    {
                        // Check expiration
                        if (_strategy == CacheStrategy.TimeBased || _strategy == CacheStrategy.Hybrid)
                        {
                            if (entry.ExpiresAt <= DateTime.UtcNow)
                            {
                                RemoveCacheEntry(cacheKey);
                                return result;
                            }
                        }

                        // Update LRU order
                        if (_strategy == CacheStrategy.LRU || _strategy == CacheStrategy.Hybrid)
                        {
                            UpdateLRUOrder(cacheKey);
                        }

                        if (entry.Value is T typedValue)
                        {
                            result.Result = typedValue;
                            result.Message = "Result retrieved from cache.";
                            return result;
                        }
                        else if (entry.Value != null)
                        {
                            try
                            {
                                result.Result = (T)Convert.ChangeType(entry.Value, typeof(T));
                                result.Message = "Result retrieved from cache (converted).";
                                return result;
                            }
                            catch
                            {
                                // Conversion failed, remove invalid entry
                                RemoveCacheEntry(cacheKey);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting cached result: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Caches a result with optional expiry
        /// Uses LRU eviction if cache is full
        /// </summary>
        public void CacheResult<T>(string cacheKey, T value, TimeSpan? expiry = null)
        {
            try
            {
                lock (_lockObject)
                {
                    // Evict if cache is full (LRU strategy)
                    if ((_strategy == CacheStrategy.LRU || _strategy == CacheStrategy.Hybrid) && 
                        _cache.Count >= _maxCacheSize && 
                        !_cache.ContainsKey(cacheKey))
                    {
                        EvictLRUEntry();
                    }

                    var expiresAt = DateTime.UtcNow.Add(expiry ?? _defaultCacheExpiry);
                    _cache[cacheKey] = new CacheEntry
                    {
                        Value = value,
                        ExpiresAt = expiresAt,
                        CachedAt = DateTime.UtcNow
                    };

                    // Update LRU order
                    if (_strategy == CacheStrategy.LRU || _strategy == CacheStrategy.Hybrid)
                    {
                        UpdateLRUOrder(cacheKey);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - caching is non-critical
                System.Diagnostics.Debug.WriteLine($"Error caching result: {ex.Message}");
            }
        }

        /// <summary>
        /// Records performance metrics for a function invocation
        /// </summary>
        public void RecordInvocation(string functionKey, TimeSpan duration, bool success)
        {
            try
            {
                lock (_lockObject)
                {
                    if (!_metrics.TryGetValue(functionKey, out var metrics))
                    {
                        metrics = new PerformanceMetrics
                        {
                            FunctionKey = functionKey
                        };
                        _metrics[functionKey] = metrics;
                    }

                    metrics.TotalInvocations++;
                    if (success)
                        metrics.SuccessfulInvocations++;
                    else
                        metrics.FailedInvocations++;

                    metrics.TotalDuration = metrics.TotalDuration.Add(duration);
                    metrics.AverageDuration = TimeSpan.FromMilliseconds(
                        metrics.TotalDuration.TotalMilliseconds / metrics.TotalInvocations);

                    if (duration > metrics.MaxDuration)
                        metrics.MaxDuration = duration;
                    if (duration < metrics.MinDuration || metrics.MinDuration == TimeSpan.Zero)
                        metrics.MinDuration = duration;

                    metrics.LastInvocation = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error recording metrics: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets performance metrics for a function
        /// </summary>
        public PerformanceMetrics GetMetrics(string functionKey)
        {
            lock (_lockObject)
            {
                return _metrics.TryGetValue(functionKey, out var metrics) ? metrics : null;
            }
        }

        /// <summary>
        /// Gets all performance metrics
        /// </summary>
        public IEnumerable<PerformanceMetrics> GetAllMetrics()
        {
            lock (_lockObject)
            {
                return _metrics.Values.ToList();
            }
        }

        /// <summary>
        /// Clears cache entries
        /// </summary>
        public void ClearCache()
        {
            lock (_lockObject)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// Clears expired cache entries
        /// </summary>
        public void ClearExpiredCache()
        {
            lock (_lockObject)
            {
                var now = DateTime.UtcNow;
                var expiredKeys = _cache.Where(kvp => kvp.Value.ExpiresAt <= now)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    RemoveCacheEntry(key);
                }
            }
        }

        /// <summary>
        /// Updates LRU order - moves key to end (most recently used)
        /// </summary>
        private void UpdateLRUOrder(string cacheKey)
        {
            // Remove from current position if exists
            var node = _lruOrder.Find(cacheKey);
            if (node != null)
            {
                _lruOrder.Remove(node);
            }

            // Add to end (most recently used)
            _lruOrder.AddLast(cacheKey);
        }

        /// <summary>
        /// Evicts least recently used entry
        /// </summary>
        private void EvictLRUEntry()
        {
            if (_lruOrder.Count > 0)
            {
                var lruKey = _lruOrder.First.Value;
                RemoveCacheEntry(lruKey);
            }
        }

        /// <summary>
        /// Removes cache entry and updates LRU order
        /// </summary>
        private void RemoveCacheEntry(string cacheKey)
        {
            _cache.Remove(cacheKey);
            var node = _lruOrder.Find(cacheKey);
            if (node != null)
            {
                _lruOrder.Remove(node);
            }
        }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        public CacheStatistics GetCacheStatistics()
        {
            lock (_lockObject)
            {
                return new CacheStatistics
                {
                    TotalEntries = _cache.Count,
                    MaxSize = _maxCacheSize,
                    Strategy = _strategy,
                    ExpiredEntries = _cache.Count(kvp => kvp.Value.ExpiresAt <= DateTime.UtcNow),
                    MemoryUsage = _cache.Count * 100 // Rough estimate in bytes
                };
            }
        }

        /// <summary>
        /// Clears performance metrics
        /// </summary>
        public void ClearMetrics()
        {
            lock (_lockObject)
            {
                _metrics.Clear();
            }
        }

        /// <summary>
        /// Generates a cache key from function name and parameters
        /// </summary>
        public static string GenerateCacheKey(string libraryId, string functionName, params object[] parameters)
        {
            var paramStr = parameters != null && parameters.Length > 0
                ? string.Join("|", parameters.Select(p => p?.ToString() ?? "null"))
                : "no_params";

            return $"{libraryId}:{functionName}:{paramStr}";
        }

        private class CacheEntry
        {
            public object Value { get; set; }
            public DateTime CachedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }

    /// <summary>
    /// Performance metrics for a function
    /// </summary>
    public class PerformanceMetrics
    {
        public string FunctionKey { get; set; }
        public long TotalInvocations { get; set; }
        public long SuccessfulInvocations { get; set; }
        public long FailedInvocations { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public DateTime LastInvocation { get; set; }

        public double SuccessRate => TotalInvocations > 0
            ? (double)SuccessfulInvocations / TotalInvocations * 100
            : 0;

        public override string ToString()
        {
            return $"Function: {FunctionKey}, " +
                   $"Invocations: {TotalInvocations}, " +
                   $"Success Rate: {SuccessRate:F2}%, " +
                   $"Avg Duration: {AverageDuration.TotalMilliseconds:F2}ms, " +
                   $"Min: {MinDuration.TotalMilliseconds:F2}ms, " +
                   $"Max: {MaxDuration.TotalMilliseconds:F2}ms";
        }
    }

    /// <summary>
    /// Cache statistics
    /// </summary>
    public class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int MaxSize { get; set; }
        public PerformanceCache.CacheStrategy Strategy { get; set; }
        public int ExpiredEntries { get; set; }
        public long MemoryUsage { get; set; }

        public double UtilizationPercent => MaxSize > 0 ? (double)TotalEntries / MaxSize * 100 : 0;

        public override string ToString()
        {
            return $"Cache: {TotalEntries}/{MaxSize} entries ({UtilizationPercent:F1}% used), " +
                   $"Strategy: {Strategy}, Expired: {ExpiredEntries}, Memory: ~{MemoryUsage} bytes";
        }
    }
}


