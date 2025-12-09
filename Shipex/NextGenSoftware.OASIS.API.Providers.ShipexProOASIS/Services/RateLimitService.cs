using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

/// <summary>
/// Service for rate limiting merchant requests
/// </summary>
public class RateLimitService
{
    private readonly ILogger<RateLimitService> _logger;
    private readonly ConcurrentDictionary<Guid, RateLimitTracker> _rateLimitCache = new();
    
    // Rate limit configurations per tier
    private readonly Dictionary<string, RateLimitConfig> _tierConfigs = new()
    {
        { "Basic", new RateLimitConfig { Tier = "Basic", RequestsPerHour = 100, RequestsPerDay = 1000 } },
        { "Premium", new RateLimitConfig { Tier = "Premium", RequestsPerHour = 1000, RequestsPerDay = 10000 } },
        { "Enterprise", new RateLimitConfig { Tier = "Enterprise", RequestsPerHour = 10000, RequestsPerDay = 100000 } }
    };

    public RateLimitService(ILogger<RateLimitService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Check if merchant has exceeded rate limit
    /// </summary>
    public RateLimitStatus CheckRateLimit(Guid merchantId, string tier)
    {
        var config = _tierConfigs.GetValueOrDefault(tier, _tierConfigs["Basic"]);
        var tracker = _rateLimitCache.GetOrAdd(merchantId, _ => new RateLimitTracker());

        // Clean up old entries
        CleanupOldEntries(tracker);

        // Check hourly limit
        var now = DateTime.UtcNow;
        var hourWindow = now.AddHours(-1);
        var requestsInHour = tracker.Requests.Count(r => r > hourWindow);

        if (requestsInHour >= config.RequestsPerHour)
        {
            var resetAt = tracker.Requests.Where(r => r > hourWindow).Min().AddHours(1);
            return new RateLimitStatus
            {
                Limit = config.RequestsPerHour,
                Remaining = 0,
                ResetAt = resetAt
            };
        }

        // Record this request
        tracker.Requests.Add(now);

        return new RateLimitStatus
        {
            Limit = config.RequestsPerHour,
            Remaining = config.RequestsPerHour - requestsInHour - 1,
            ResetAt = now.AddHours(1)
        };
    }

    private void CleanupOldEntries(RateLimitTracker tracker)
    {
        var cutoff = DateTime.UtcNow.AddDays(-1);
        tracker.Requests.RemoveAll(r => r < cutoff);
    }
}

/// <summary>
/// Tracks rate limit requests for a merchant
/// </summary>
internal class RateLimitTracker
{
    public List<DateTime> Requests { get; } = new();
}




