using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Services.Subscription
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ILogger<SubscriptionService> _logger;
        private readonly SemaphoreSlim _usageLock = new(1, 1);

        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        public SubscriptionService(ILogger<SubscriptionService> logger)
        {
            _logger = logger;
        }

        public async Task<SubscriptionRecord> GetSubscriptionAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var avatarId)) return null;
            return await LoadSubscriptionAsync(avatarId, userId);
        }

        public async Task<UsageRecord> GetUsageAsync(string userId, int year, int month)
        {
            if (!Guid.TryParse(userId, out var avatarId))
                return new UsageRecord { UserId = userId, Year = year, Month = month };
            try
            {
                var key = $"{year:D4}-{month:D2}";
                var result = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "subscription-usage");
                if (result.IsError || result.Result == null || !result.Result.TryGetValue(key, out var raw))
                    return new UsageRecord { UserId = userId, Year = year, Month = month };
                return DeserializeUsage(userId, year, month, raw);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading usage for user {UserId}", userId);
                return new UsageRecord { UserId = userId, Year = year, Month = month };
            }
        }

        public async Task IncrementUsageAsync(string userId) => await BumpCounter(userId, overage: false);
        public async Task IncrementOverageAsync(string userId) => await BumpCounter(userId, overage: true);

        private async Task BumpCounter(string userId, bool overage)
        {
            if (!Guid.TryParse(userId, out var avatarId)) return;
            var now = DateTime.UtcNow;
            var key = $"{now.Year:D4}-{now.Month:D2}";

            await _usageLock.WaitAsync();
            try
            {
                var allResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "subscription-usage");
                var all = allResult.Result ?? new Dictionary<string, object>();

                var rec = all.TryGetValue(key, out var raw)
                    ? DeserializeUsage(userId, now.Year, now.Month, raw)
                    : new UsageRecord { UserId = userId, Year = now.Year, Month = now.Month };

                rec.RequestCount++;
                if (overage) rec.OverageCount++;
                rec.LastUpdated = now;
                all[key] = JsonSerializer.Serialize(rec, _json);

                await HolonManager.Instance.SaveSettingsAsync(avatarId, "subscription-usage", all);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bumping usage counter for user {UserId}", userId);
            }
            finally { _usageLock.Release(); }
        }

        private async Task<SubscriptionRecord> LoadSubscriptionAsync(Guid avatarId, string userId)
        {
            try
            {
                var result = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "subscription");
                if (result.IsError || result.Result == null || result.Result.Count == 0) return null;
                var d = result.Result;
                return new SubscriptionRecord
                {
                    UserId            = userId,
                    PlanId            = d.GetValueOrDefault("planId")?.ToString() ?? "free",
                    Status            = d.GetValueOrDefault("status")?.ToString() ?? "active",
                    PayAsYouGoEnabled = d.TryGetValue("payAsYouGoEnabled", out var payg) && payg is bool b && b,
                    CurrentPeriodEnd  = d.TryGetValue("currentPeriodEnd", out var e) && e != null ? ParseDate(e) : null,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading subscription for avatar {AvatarId}", avatarId);
                return null;
            }
        }

        private UsageRecord DeserializeUsage(string userId, int year, int month, object raw)
        {
            try
            {
                var rec = JsonSerializer.Deserialize<UsageRecord>(raw?.ToString() ?? "{}", _json);
                if (rec != null) return rec;
            }
            catch { }
            return new UsageRecord { UserId = userId, Year = year, Month = month };
        }

        private static DateTime? ParseDate(object val)
        {
            if (val == null) return null;
            if (val is DateTime dt) return dt;
            if (DateTime.TryParse(val.ToString(), out var parsed)) return parsed;
            return null;
        }
    }
}
