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
        private readonly ISubscriptionUsageRepository _usageRepository;

        // Protects read-modify-write usage increments from concurrent requests
        private readonly SemaphoreSlim _usageLock = new(1, 1);
        private readonly SemaphoreSlim _orderLock = new(1, 1);

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public SubscriptionService(ILogger<SubscriptionService> logger, ISubscriptionUsageRepository usageRepository)
        {
            _logger = logger;
            _usageRepository = usageRepository;
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
                var result = await HolonManager.Instance.SaveSettingsAsync(avatarId, "subscription", settings);
                if (result.IsError)
                    _logger.LogError("SaveSettingsAsync failed for user {UserId}: {Message}", record.UserId, result.Message);
                else
                    _logger.LogInformation("Subscription saved for user {UserId} plan {PlanId}", record.UserId, record.PlanId);
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

        public async Task<SubscriptionAuthorizationDecision> AuthorizeAndIncrementRequestAsync(string userId, string consumingService)
        {
            if (!Guid.TryParse(userId, out var avatarId))
                return Denied(401, "INVALID_AVATAR", "The authenticated avatar ID is invalid.");

            await _usageLock.WaitAsync();
            try
            {
                var subscription = await LoadSubscriptionAsync(avatarId);
                if (subscription == null)
                    return Denied(402, "SUBSCRIPTION_REQUIRED", "A valid OASIS subscription is required.");
                if (subscription.Status is not ("active" or "trialing" or "free") ||
                    subscription.CurrentPeriodEnd.HasValue && DateTime.UtcNow > subscription.CurrentPeriodEnd.Value)
                    return Denied(402, "INACTIVE_SUBSCRIPTION", "The OASIS subscription is inactive or expired.", subscription.PlanId);

                int limit = subscription.PlanId?.ToLowerInvariant() switch
                {
                    "free" => 1_000,
                    "bronze" => 10_000,
                    "silver" => 100_000,
                    "gold" => 1_000_000,
                    "enterprise" => -1,
                    _ => 1_000
                };
                var now = DateTime.UtcNow;
                var key = UsageKey(now.Year, now.Month);
                var allResult = await HolonManager.Instance.GetAllSettingsAsync(avatarId, "subscription-usage");
                if (allResult.IsError)
                    throw new InvalidOperationException(allResult.Message);
                var all = allResult.Result ?? new Dictionary<string, object>();
                var usage = all.TryGetValue(key, out var raw)
                    ? DeserializeUsage(userId, now.Year, now.Month, raw)
                    : new UsageRecord { UserId = userId, Year = now.Year, Month = now.Month };

                if (limit >= 0 && usage.RequestCount >= limit && !subscription.PayAsYouGoEnabled)
                    return new SubscriptionAuthorizationDecision { Allowed = false, StatusCode = 429, Code = "PLAN_LIMIT_EXCEEDED", Message = $"You have used {usage.RequestCount:N0} of your {limit:N0} monthly requests.", PlanId = subscription.PlanId, CurrentUsage = usage.RequestCount, Limit = limit, Remaining = 0 };

                usage.RequestCount++;
                if (limit >= 0 && usage.RequestCount > limit) usage.OverageCount++;
                usage.LastUpdated = now;
                all[key] = JsonSerializer.Serialize(usage, _json);
                var save = await HolonManager.Instance.SaveSettingsAsync(avatarId, "subscription-usage", all);
                if (save.IsError) throw new InvalidOperationException(save.Message);

                return new SubscriptionAuthorizationDecision { Allowed = true, StatusCode = 200, Code = "AUTHORIZED", Message = $"{consumingService} request authorized.", PlanId = subscription.PlanId, CurrentUsage = usage.RequestCount, Limit = limit, Remaining = limit < 0 ? -1 : Math.Max(0, limit - usage.RequestCount) };
            }
            finally { _usageLock.Release(); }
        }

        public async Task<SubscriptionAuthorizationDecision> AuthorizeUsageAsync(
            string userId, int karma, UsageAuthorizationRequest request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(userId, out var avatarId))
                return Denied(401, "INVALID_AVATAR", "The authenticated avatar ID is invalid.");
            if (request == null || !Guid.TryParse(request.OperationId, out _) ||
                string.IsNullOrWhiteSpace(request.ConsumingService) || string.IsNullOrWhiteSpace(request.Endpoint) ||
                string.IsNullOrWhiteSpace(request.MeterCategory) || request.RequestedUnits < 0 || request.EstimatedCostUsd < 0)
                return Denied(400, "INVALID_USAGE_AUTHORIZATION", "OperationId must be a UUID; service, endpoint, and meter category are required; reservation values cannot be negative.");
            string normalizedService = request.ConsumingService.Trim().ToUpperInvariant();
            if (normalizedService is not ("WEB5" or "WEB6" or "WEB7" or "WEB8" or "WEB9" or "WEB10"))
                return Denied(400, "INVALID_CONSUMING_SERVICE", "ConsumingService must be WEB5, WEB6, WEB7, WEB8, WEB9, or WEB10.");

            var subscription = await LoadSubscriptionAsync(avatarId);
            if (subscription == null)
                return Denied(402, "SUBSCRIPTION_REQUIRED", "A valid OASIS subscription is required.");
            if (subscription.Status is not ("active" or "trialing" or "free") ||
                subscription.CurrentPeriodEnd.HasValue && DateTime.UtcNow > subscription.CurrentPeriodEnd.Value)
                return Denied(402, "INACTIVE_SUBSCRIPTION", "The OASIS subscription is inactive or expired.", subscription.PlanId);

            var policy = GetUsagePolicy(subscription.PlanId, karma);
            var now = DateTime.UtcNow;
            var usageEvent = new SubscriptionUsageEvent
            {
                OperationId = request.OperationId,
                UserId = userId,
                ConsumingService = normalizedService,
                Endpoint = request.Endpoint?.Trim(),
                MeterCategory = request.MeterCategory?.Trim(),
                Status = "authorized",
                RequestedUnits = request.RequestedUnits,
                ReservedCostUsd = request.EstimatedCostUsd,
                AuthorizedAtUtc = now
            };

            try
            {
                var (_, aggregate) = await _usageRepository.AuthorizeAsync(usageEvent, policy, cancellationToken);
                return BuildDecision(subscription.PlanId, karma, request.OperationId, aggregate, policy);
            }
            catch (SubscriptionUsageLimitException ex)
            {
                var denied = Denied(429, ex.Code, ex.Message, subscription.PlanId);
                denied.OperationId = request.OperationId;
                return denied;
            }
            catch (SubscriptionUsageConflictException ex)
            {
                return Denied(409, "OPERATION_ID_CONFLICT", ex.Message, subscription.PlanId);
            }
        }

        public async Task<UsageSettlementResult> SettleUsageAsync(
            string userId, UsageSettlementRequest request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(userId, out _) || request == null || !Guid.TryParse(request.OperationId, out _))
                throw new ArgumentException("A valid authenticated avatar and operation UUID are required.");
            if (request.PromptTokens < 0 || request.CompletionTokens < 0 || request.Units < 0 ||
                request.EstimatedCostUsd < 0 || request.ActualCostUsd.GetValueOrDefault() < 0)
                throw new ArgumentOutOfRangeException(nameof(request), "Usage settlement values cannot be negative.");
            request.Outcome = request.Outcome?.Trim().ToLowerInvariant();
            if (request.Outcome is not ("succeeded" or "failed" or "cancelled"))
                throw new ArgumentException("Outcome must be succeeded, failed, or cancelled.", nameof(request));

            var (_, _, alreadySettled) = await _usageRepository.SettleAsync(userId, request, cancellationToken);
            return new UsageSettlementResult
            {
                Settled = true,
                AlreadySettled = alreadySettled,
                OperationId = request.OperationId,
                Message = alreadySettled ? "Usage was already settled." : "Usage settled."
            };
        }

        public async Task<SubscriptionUsageSummary> GetUsageSummaryAsync(string userId, int karma, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(userId, out var avatarId)) throw new ArgumentException("Invalid avatar id.", nameof(userId));
            var subscription = await LoadSubscriptionAsync(avatarId) ?? throw new InvalidOperationException("A valid OASIS subscription is required.");
            var policy = GetUsagePolicy(subscription.PlanId, karma);
            var aggregate = await _usageRepository.GetAggregateAsync(userId, cancellationToken);
            return new SubscriptionUsageSummary
            {
                UserId=userId, PlanId=subscription.PlanId, Karma=karma, MonthlyRequests=aggregate.MonthlyRequests,
                DailyCalls=aggregate.DailyCalls, DailyTokens=aggregate.DailyTokens,
                MonthlySpendUsd=aggregate.SettledCostUsd + aggregate.ReservedCostUsd,
                DailyCallLimit=policy.DailyCallLimit, DailyTokenLimit=policy.DailyTokenLimit, MonthlyBudgetUsd=policy.MonthlyBudgetUsd,
                DailyCallsRemaining=Remaining(policy.DailyCallLimit, aggregate.DailyCalls),
                DailyTokensRemaining=Remaining(policy.DailyTokenLimit, aggregate.DailyTokens),
                MonthlyBudgetRemainingUsd=policy.MonthlyBudgetUsd <= 0 ? -1 : Math.Max(0, policy.MonthlyBudgetUsd - aggregate.SettledCostUsd - aggregate.ReservedCostUsd),
                UpdatedAtUtc=aggregate.UpdatedAtUtc
            };
        }

        public Task<IReadOnlyList<SubscriptionUsageEvent>> GetUsageEventsAsync(string userId, int limit, CancellationToken cancellationToken) =>
            _usageRepository.GetEventsAsync(userId, limit, cancellationToken);

        public static SubscriptionUsagePolicy GetUsagePolicy(string planId, int karma)
        {
            var (monthly, daily, tokens, budget) = (planId ?? "free").ToLowerInvariant() switch
            {
                "bronze" => (10_000, 100, 250_000L, 10m),
                "silver" => (100_000, 500, 1_000_000L, 50m),
                "gold" => (1_000_000, 2_000, 5_000_000L, 250m),
                "enterprise" => (-1, 0, 0L, 0m),
                _ => (1_000, 20, 50_000L, 1m)
            };
            if (daily > 0) daily = checked((int)(daily * KarmaMultiplier(karma)));
            return new SubscriptionUsagePolicy { MonthlyRequestLimit=monthly, DailyCallLimit=daily, DailyTokenLimit=tokens, MonthlyBudgetUsd=budget };
        }

        public static decimal KarmaMultiplier(int karma) => karma switch
        {
            >= 100_000 => 10m, >= 20_000 => 5m, >= 5_000 => 3m, >= 1_000 => 2m, >= 500 => 1.5m, _ => 1m
        };

        private static SubscriptionAuthorizationDecision BuildDecision(string planId, int karma, string operationId, SubscriptionUsageAggregate aggregate, SubscriptionUsagePolicy policy) => new()
        {
            Allowed=true, StatusCode=200, Code="AUTHORIZED", Message="Usage authorized.", PlanId=planId,
            OperationId=operationId, Karma=karma, CurrentUsage=aggregate.MonthlyRequests, Limit=policy.MonthlyRequestLimit,
            Remaining=Remaining(policy.MonthlyRequestLimit, aggregate.MonthlyRequests), DailyCallLimit=policy.DailyCallLimit,
            DailyCallsRemaining=Remaining(policy.DailyCallLimit, aggregate.DailyCalls), DailyTokenLimit=policy.DailyTokenLimit,
            DailyTokensRemaining=Remaining(policy.DailyTokenLimit, aggregate.DailyTokens), MonthlyBudgetUsd=policy.MonthlyBudgetUsd,
            MonthlyBudgetRemainingUsd=policy.MonthlyBudgetUsd <= 0 ? -1 : Math.Max(0, policy.MonthlyBudgetUsd - aggregate.ReservedCostUsd - aggregate.SettledCostUsd)
        };

        private static long Remaining(long limit, long used) => limit <= 0 ? -1 : Math.Max(0, limit - used);

        private static SubscriptionAuthorizationDecision Denied(int status, string code, string message, string planId = null) =>
            new() { Allowed = false, StatusCode = status, Code = code, Message = message, PlanId = planId, Limit = 0, Remaining = 0 };

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
