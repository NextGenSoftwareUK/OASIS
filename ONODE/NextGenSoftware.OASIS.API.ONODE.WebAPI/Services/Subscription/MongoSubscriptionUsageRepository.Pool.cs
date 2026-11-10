using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed partial class MongoSubscriptionUsageRepository
    {
        /// <summary>
        /// For a pooled app, also admit the request against the app's shared pool (app plan limits, karma 0).
        /// A refusal uses the normal code with an APP_ prefix so a client can tell "you are out" from "the app is out".
        /// No-op for requests without a pooled app, so every other flow is unchanged.
        /// </summary>
        private async Task EnsurePoolAdmissionAsync(IClientSessionHandle session, SubscriptionUsageEvent usageEvent, int karma, CancellationToken ct)
        {
            if (usageEvent.PooledAppId == null) return;
            var pool = await LoadAggregateAsync(session, PoolSubject(usageEvent.PooledAppId), usageEvent.AuthorizedAtUtc, ct);
            var policy = _policyProvider.GetPolicy(usageEvent.PooledAppPlanId, 0);
            try
            {
                SubscriptionUsagePolicyEvaluator.EnsureAuthorized(policy, pool,
                    usageEvent.MeterCategory == "ai.tokens" ? usageEvent.RequestedUnits : 0, usageEvent.ReservedCostUsd);
            }
            catch (SubscriptionUsageLimitException ex)
            {
                throw new SubscriptionUsageLimitException("APP_" + ex.Code, "The app's shared quota is used up: " + ex.Message, ex.StatusCode);
            }
        }

        /// <summary>Administrator view of an app pool: this month's and today's usage against the plan.</summary>
        public async Task<AppPoolUsage> GetAppPoolUsageAsync(string appId, CancellationToken cancellationToken = default)
        {
            AppAccountRules.ValidateAppId(appId);
            var account = await AppAccounts.Find(x => x.AppId == appId).FirstOrDefaultAsync(cancellationToken)
                ?? throw new System.Collections.Generic.KeyNotFoundException("The app account was not found.");
            using var session = await _client.StartSessionAsync(cancellationToken: cancellationToken);
            var now = _time.GetUtcNow().UtcDateTime;
            var aggregate = await LoadAggregateAsync(session, PoolSubject(appId), now, cancellationToken);
            var policy = _policyProvider.GetPolicy(account.PlanId, 0);
            return AppPoolUsage.Build(account.Redacted(), policy, aggregate, AppAccountRules.PooledQuotasEnabled);
        }
    }

    public sealed class AppPoolUsage
    {
        public AppAccountRecord Account { get; set; }
        public bool PoolActive { get; set; }
        public SubscriptionUsagePolicy Limits { get; set; }
        public SubscriptionUsageAggregate Usage { get; set; }
        /// <summary>Fraction of the monthly request limit used (0-1); null when unlimited.</summary>
        public double? MonthlyRequestsUsedFraction { get; set; }
        public double? DailyCallsUsedFraction { get; set; }

        public static AppPoolUsage Build(AppAccountRecord account, SubscriptionUsagePolicy limits, SubscriptionUsageAggregate usage, bool flagOn) => new()
        {
            Account = account, Limits = limits, Usage = usage,
            PoolActive = flagOn && account.PooledQuota && account.PlanId != "enterprise",
            MonthlyRequestsUsedFraction = limits.MonthlyRequestLimit > 0 ? (double)usage.MonthlyRequests / limits.MonthlyRequestLimit : null,
            DailyCallsUsedFraction = limits.DailyCallLimit > 0 ? (double)usage.DailyCalls / limits.DailyCallLimit : null
        };
    }
}
