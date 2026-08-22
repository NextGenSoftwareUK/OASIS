using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    /// <summary>
    /// HolonManager-backed subscription service.
    /// Subscription records are persisted per-avatar under the "subscription" settings category.
    /// Usage is persisted under "subscription-usage". Orders under "subscription-orders".
    /// Reverse lookups by Stripe IDs use LoadHolonsByMetaDataAsync.
    /// </summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ILogger<SubscriptionService> _logger;

        // Protects read-modify-write usage increments from concurrent requests
        private readonly SemaphoreSlim _usageLock = new(1, 1);
        private readonly SemaphoreSlim _orderLock = new(1, 1);

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public SubscriptionService(ILogger<SubscriptionService> logger)
        {
            _logger = logger;
        }

        // ── Subscription ────────────────────────────────────────────────────

        public async Task<SubscriptionRecord> GetSubscriptionAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var avatarId)) return null;
            return await LoadSubscriptionAsync(avatarId);
        }

        public async Task<SubscriptionRecord> GetSubscriptionByStripeCustomerIdAsync(string stripeCustomerId)
        {
            try
            {
                var result = await HolonManager.Instance.LoadHolonsByMetaDataAsync(
                    "stripeCustomerId", stripeCustomerId);

                if (result.IsError || result.Result == null) return null;

                var holon = result.Result.FirstOrDefault();
                if (holon == null) return null;

                // The avatarId is stored in the holon's CreatedByAvatarId
                return await LoadSubscriptionAsync(holon.CreatedByAvatarId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up subscription by Stripe customer ID {Id}", stripeCustomerId);
                return null;
            }
        }

        public async Task<SubscriptionRecord> GetSubscriptionByStripeSubscriptionIdAsync(string stripeSubscriptionId)
        {
            try
            {
                var result = await HolonManager.Instance.LoadHolonsByMetaDataAsync(
                    "stripeSubscriptionId", stripeSubscriptionId);

                if (result.IsError || result.Result == null) return null;

                var holon = result.Result.FirstOrDefault();
                if (holon == null) return null;

                return await LoadSubscriptionAsync(holon.CreatedByAvatarId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up subscription by Stripe subscription ID {Id}", stripeSubscriptionId);
                return null;
            }
        }

        public async Task UpsertSubscriptionAsync(SubscriptionRecord record)
        {
            if (!Guid.TryParse(record.UserId, out var avatarId)) return;
            record.UpdatedAt = DateTime.UtcNow;

            var settings = RecordToDict(record);
            try
            {
                await HolonManager.Instance.SaveSettingsAsync(avatarId, "subscription", settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting subscription for user {UserId}", record.UserId);
            }
        }

        public async Task SetPayAsYouGoAsync(string userId, bool enabled)
        {
            if (!Guid.TryParse(userId, out var avatarId)) return;
            try
            {
                var record = await LoadSubscriptionAsync(avatarId) ?? new SubscriptionRecord
                {
                    UserId = userId,
                    PlanId = "free",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                };
                record.PayAsYouGoEnabled = enabled;
                record.UpdatedAt = DateTime.UtcNow;
                await HolonManager.Instance.SaveSettingsAsync(avatarId, "subscription", RecordToDict(record));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting pay-as-you-go for user {UserId}", userId);
            }
        }

        // ── Usage ───────────────────────────────────────────────────────────

        public async Task<UsageRecord> GetUsageAsync(string userId, int year, int month)
        {
            if (!Guid.TryParse(userId, out var avatarId))
                return new UsageRecord { UserId = userId, Year = year, Month = month };

            try
            {
                var key = UsageKey(year, month);
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

        public async Task IncrementUsageAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var avatarId)) return;
            var now = DateTime.UtcNow;

            await _usageLock.WaitAsync();
            try
            {
                var key = UsageKey(now.Year, now.Month);
                var allResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "subscription-usage");
                var all = allResult.Result ?? new Dictionary<string, object>();

                var rec = all.TryGetValue(key, out var raw)
                    ? DeserializeUsage(userId, now.Year, now.Month, raw)
                    : new UsageRecord { UserId = userId, Year = now.Year, Month = now.Month };

                rec.RequestCount++;
                rec.LastUpdated = now;
                all[key] = JsonSerializer.Serialize(rec, _json);

                await HolonManager.Instance.SaveSettingsAsync(avatarId, "subscription-usage", all);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing usage for user {UserId}", userId);
            }
            finally { _usageLock.Release(); }
        }

        public async Task IncrementOverageAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var avatarId)) return;
            var now = DateTime.UtcNow;

            await _usageLock.WaitAsync();
            try
            {
                var key = UsageKey(now.Year, now.Month);
                var allResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "subscription-usage");
                var all = allResult.Result ?? new Dictionary<string, object>();

                var rec = all.TryGetValue(key, out var raw)
                    ? DeserializeUsage(userId, now.Year, now.Month, raw)
                    : new UsageRecord { UserId = userId, Year = now.Year, Month = now.Month };

                rec.RequestCount++;
                rec.OverageCount++;
                rec.LastUpdated = now;
                all[key] = JsonSerializer.Serialize(rec, _json);

                await HolonManager.Instance.SaveSettingsAsync(avatarId, "subscription-usage", all);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing overage for user {UserId}", userId);
            }
            finally { _usageLock.Release(); }
        }

        // ── Orders ──────────────────────────────────────────────────────────

        public async Task<List<OrderRecord>> GetOrdersAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var avatarId)) return new();
            try
            {
                var result = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "subscription-orders");
                if (result.IsError || result.Result == null || !result.Result.TryGetValue("orders", out var raw))
                    return new();

                var list = JsonSerializer.Deserialize<List<OrderRecord>>(raw?.ToString() ?? "[]", _json) ?? new();
                return list.OrderByDescending(o => o.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders for user {UserId}", userId);
                return new();
            }
        }

        public async Task AddOrderAsync(OrderRecord order)
        {
            if (!Guid.TryParse(order.UserId, out var avatarId)) return;

            await _orderLock.WaitAsync();
            try
            {
                var result = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "subscription-orders");
                var all = result.Result ?? new Dictionary<string, object>();

                var list = all.TryGetValue("orders", out var raw)
                    ? JsonSerializer.Deserialize<List<OrderRecord>>(raw?.ToString() ?? "[]", _json) ?? new()
                    : new List<OrderRecord>();

                if (!string.IsNullOrEmpty(order.StripeInvoiceId) && list.Any(o => o.StripeInvoiceId == order.StripeInvoiceId))
                    return;

                list.Add(order);
                all["orders"] = JsonSerializer.Serialize(list, _json);
                await HolonManager.Instance.SaveSettingsAsync(avatarId, "subscription-orders", all);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding order for user {UserId}", order.UserId);
            }
            finally { _orderLock.Release(); }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private async Task<SubscriptionRecord> LoadSubscriptionAsync(Guid avatarId)
        {
            try
            {
                var result = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "subscription");
                if (result.IsError || result.Result == null || result.Result.Count == 0) return null;

                var d = result.Result;
                return new SubscriptionRecord
                {
                    UserId               = avatarId.ToString(),
                    StripeCustomerId     = d.GetValueOrDefault("stripeCustomerId")?.ToString(),
                    StripeSubscriptionId = d.GetValueOrDefault("stripeSubscriptionId")?.ToString(),
                    PlanId               = d.GetValueOrDefault("planId")?.ToString() ?? "free",
                    Status               = d.GetValueOrDefault("status")?.ToString() ?? "active",
                    PayAsYouGoEnabled    = d.TryGetValue("payAsYouGoEnabled", out var payg) && payg is bool b && b,
                    CurrentPeriodStart   = d.TryGetValue("currentPeriodStart", out var s) && s != null ? ParseDate(s) : null,
                    CurrentPeriodEnd     = d.TryGetValue("currentPeriodEnd", out var e) && e != null ? ParseDate(e) : null,
                    CreatedAt            = ParseDate(d.GetValueOrDefault("createdAt")) ?? DateTime.UtcNow,
                    UpdatedAt            = ParseDate(d.GetValueOrDefault("updatedAt")) ?? DateTime.UtcNow,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading subscription for avatar {AvatarId}", avatarId);
                return null;
            }
        }

        private static Dictionary<string, object> RecordToDict(SubscriptionRecord r) => new()
        {
            ["stripeCustomerId"]     = r.StripeCustomerId ?? "",
            ["stripeSubscriptionId"] = r.StripeSubscriptionId ?? "",
            ["planId"]               = r.PlanId ?? "free",
            ["status"]               = r.Status ?? "active",
            ["payAsYouGoEnabled"]    = r.PayAsYouGoEnabled,
            ["currentPeriodStart"]   = r.CurrentPeriodStart?.ToString("O") ?? "",
            ["currentPeriodEnd"]     = r.CurrentPeriodEnd?.ToString("O") ?? "",
            ["createdAt"]            = r.CreatedAt.ToString("O"),
            ["updatedAt"]            = r.UpdatedAt.ToString("O"),
        };

        private static string UsageKey(int year, int month) => $"{year:D4}-{month:D2}";

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
