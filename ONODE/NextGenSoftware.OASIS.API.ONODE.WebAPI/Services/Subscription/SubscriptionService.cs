using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using SubscriptionAuthorizationDecision = NextGenSoftware.OASIS.API.Core.Services.Subscriptions.SubscriptionAuthorizationResult;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    /// <summary>WEB4 owns subscription records, billing history and usage in one durable MongoDB authority.</summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionUsageRepository _usageRepository;
        private readonly ISubscriptionBillingRepository _billing;
        public SubscriptionService(ILogger<SubscriptionService> logger, ISubscriptionUsageRepository usageRepository, ISubscriptionBillingRepository billing)
        { _usageRepository = usageRepository; _billing = billing; }

        public Task<SubscriptionRecord> GetSubscriptionAsync(string userId) => _billing.GetSubscriptionAsync(userId);
        public Task<SubscriptionRecord> GetSubscriptionByStripeCustomerIdAsync(string stripeCustomerId) => _billing.FindSubscriptionAsync(stripeCustomerId, true);
        public Task<SubscriptionRecord> GetSubscriptionByStripeSubscriptionIdAsync(string stripeSubscriptionId) => _billing.FindSubscriptionAsync(stripeSubscriptionId, false);
        public Task UpsertSubscriptionAsync(SubscriptionRecord record) => _billing.SaveSubscriptionAsync(record);
        public Task SetPayAsYouGoAsync(string userId, bool enabled) => _billing.SetPayAsYouGoAsync(userId, enabled);
        public Task<List<OrderRecord>> GetOrdersAsync(string userId) => _billing.GetOrdersAsync(userId);
        public Task AddOrderAsync(OrderRecord order) => _billing.AddOrderAsync(order);
        private Task<SubscriptionRecord> LoadSubscriptionAsync(Guid avatarId) => _billing.GetSubscriptionAsync(avatarId.ToString("D"));
        // ── Usage ───────────────────────────────────────────────────────────

        public async Task<UsageRecord> GetUsageAsync(string userId, int year, int month)
        {
            if (!Guid.TryParseExact(userId, "D", out _)) throw new ArgumentException("Invalid avatar id.", nameof(userId));
            var aggregate = await _usageRepository.GetAggregateForPeriodAsync(userId, new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None);
            return new UsageRecord { UserId = userId, Year = year, Month = month, RequestCount = aggregate.MonthlyRequests, LastUpdated = aggregate.UpdatedAtUtc };
        }
        public async Task<SubscriptionAuthorizationDecision> AuthorizeUsageAsync(
            string userId, int karma, UsageAuthorizationRequest request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(userId, out var avatarId))
                return Denied(401, "INVALID_AVATAR", "The authenticated avatar ID is invalid.");
            if (request == null || !Guid.TryParseExact(request.OperationId, "D", out _) ||
                string.IsNullOrWhiteSpace(request.ConsumingService) || string.IsNullOrWhiteSpace(request.Endpoint) ||
                request.MeterCategory is not ("ai.tokens" or "api.request") || request.RequestedUnits < 0 || (request.MeterCategory == "ai.tokens" && request.RequestedUnits == 0) || request.EstimatedCostUsd < 0 ||
                request.RequestFingerprint == null || !System.Text.RegularExpressions.Regex.IsMatch(request.RequestFingerprint, "^[a-f0-9]{64}$"))
                return Denied(400, "INVALID_USAGE_AUTHORIZATION", "OperationId must be a UUID; service, endpoint, and meter category are required; reservation values cannot be negative.");
            string normalizedService = request.ConsumingService.Trim().ToUpperInvariant();
            if (normalizedService is not ("WEB5" or "WEB6" or "WEB7" or "WEB8" or "WEB9" or "WEB10"))
                return Denied(400, "INVALID_CONSUMING_SERVICE", "ConsumingService must be WEB5, WEB6, WEB7, WEB8, WEB9, or WEB10.");

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
                RequestFingerprint = request.RequestFingerprint,
                ExpiresAtUtc = now.AddMinutes(15),
                AuthorizedAtUtc = now
            };

            try
            {
                var (operation, aggregate) = await _usageRepository.AuthorizeAsync(usageEvent, karma, cancellationToken);
                return BuildDecision(operation.PlanId, operation.Karma, operation, aggregate, operation.AuthorizationPolicy);
            }
            catch (SubscriptionUsageLimitException ex)
            {
                var denied = Denied(ex.StatusCode, ex.Code, ex.Message);
                denied.OperationId = request.OperationId;
                return denied;
            }
            catch (SubscriptionUsageConflictException ex)
            {
                return Denied(409, "OPERATION_ID_CONFLICT", ex.Message);
            }
        }

        public async Task<UsageSettlementResult> SettleUsageAsync(
            string userId, UsageSettlementRequest request, CancellationToken cancellationToken)
        {
            UsageLedgerValidation.ValidateSettlement(userId, request);

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
                DailyCalls=aggregate.DailyCalls, DailyTokens=aggregate.DailyTokens, ReservedTokens=aggregate.ReservedUnits,
                MonthlySpendUsd=aggregate.SettledCostUsd + aggregate.ReservedCostUsd,
                DailyCallLimit=policy.DailyCallLimit, DailyTokenLimit=policy.DailyTokenLimit, MonthlyBudgetUsd=policy.MonthlyBudgetUsd,
                DailyCallsRemaining=Remaining(policy.DailyCallLimit, aggregate.DailyCalls),
                DailyTokensRemaining=Remaining(policy.DailyTokenLimit, checked(aggregate.DailyTokens + aggregate.ReservedUnits)),
                MonthlyBudgetRemainingUsd=policy.MonthlyBudgetUsd <= 0 ? -1 : Math.Max(0, policy.MonthlyBudgetUsd - aggregate.SettledCostUsd - aggregate.ReservedCostUsd),
                UpdatedAtUtc=aggregate.UpdatedAtUtc
            };
        }

        public Task<IReadOnlyList<SubscriptionUsageEvent>> GetUsageEventsAsync(string userId, int limit, CancellationToken cancellationToken) =>
            _usageRepository.GetEventsAsync(userId, limit, cancellationToken);

        public static SubscriptionUsagePolicy GetUsagePolicy(string planId, int karma) => new SubscriptionUsagePolicyProvider().GetPolicy(planId, karma);
        public static decimal KarmaMultiplier(int karma) => SubscriptionUsagePolicyProvider.KarmaMultiplier(karma);

        private static SubscriptionAuthorizationDecision BuildDecision(string planId, int karma, SubscriptionUsageEvent operation, SubscriptionUsageAggregate aggregate, SubscriptionUsagePolicy policy) => new()
        {
            Allowed=true, StatusCode=200, Code="AUTHORIZED", Message="Usage authorized.", PlanId=planId,
            OperationId=operation.OperationId, UserId=operation.UserId, AlreadyAuthorized=operation.AlreadyAuthorized,
            Status=operation.Status, ExpiresAtUtc=operation.ExpiresAtUtc, Karma=karma, CurrentUsage=aggregate.MonthlyRequests, Limit=policy.MonthlyRequestLimit,
            Remaining=Remaining(policy.MonthlyRequestLimit, aggregate.MonthlyRequests), DailyCallLimit=policy.DailyCallLimit,
            DailyCallsRemaining=checked((int)Remaining(policy.DailyCallLimit, aggregate.DailyCalls)), DailyTokenLimit=policy.DailyTokenLimit,
            DailyTokensRemaining=Remaining(policy.DailyTokenLimit, checked(aggregate.DailyTokens + aggregate.ReservedUnits)), MonthlyBudgetUsd=policy.MonthlyBudgetUsd,
            MonthlyBudgetRemainingUsd=policy.MonthlyBudgetUsd <= 0 ? -1 : Math.Max(0, policy.MonthlyBudgetUsd - aggregate.ReservedCostUsd - aggregate.SettledCostUsd)
        };

        private static long Remaining(long limit, long used) => limit <= 0 ? -1 : Math.Max(0, limit - used);

        private static SubscriptionAuthorizationDecision Denied(int status, string code, string message, string planId = null) =>
            new() { Allowed = false, StatusCode = status, Code = code, Message = message, PlanId = planId, Limit = 0, Remaining = 0 };

    }
}
