using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    /// <summary>Pure maths for one app pool's monthly pay-as-you-go statement (requests above the plan allowance x rate, limited to the owner's cap).</summary>
    public static class AppOverageStatementMath
    {
        public const int MaxMonthsBack = 3;

        /// <summary>Cents to invoice for a closed month, from the net requests in the pool's month bucket.</summary>
        public static (long OverRequests, decimal ChargedUsd, long AmountMinorUsd) Compute(long monthRequests, long includedLimit, decimal ratePerRequestUsd, decimal capUsd)
        {
            var estimate = AppOverageEstimate.Compute(monthRequests, includedLimit, ratePerRequestUsd, capUsd);
            return (estimate.OverRequests, estimate.ChargedUsd, PaidOveragePricing.MinorUnits(estimate.ChargedUsd));
        }

        /// <summary>Closed months (yyyy-MM) from the month of consent up to the previous month, most recent three at most.</summary>
        public static IReadOnlyList<string> ClosedMonths(DateTime consentUtc, DateTime nowUtc)
        {
            var months = new List<string>();
            var first = new DateTime(consentUtc.Year, consentUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var current = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            for (var m = current.AddMonths(-1); m >= first && months.Count < MaxMonthsBack; m = m.AddMonths(-1))
                months.Add(m.ToString("yyyy-MM", CultureInfo.InvariantCulture));
            months.Reverse();
            return months;
        }

        public static string StatementId(string appId, string month)
            => "ovg_app_" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("app:" + appId + ":" + month + ":monthly")));
    }

    public sealed partial class MongoSubscriptionUsageRepository
    {
        public static string AppStatementUser(string appId) => "app:" + appId;

        /// <summary>
        /// Called inside pool admission when the pool's monthly request allowance is used up. Admits the request as billable
        /// overage only when the app has agreed to pay (consent, cap, a price, a verified owner Stripe account), the owner has not
        /// failed a payment, every OTHER limit still passes, and this request would not take the month's charge above the cap.
        /// Any doubt means a normal denial.
        /// </summary>
        private async Task<bool> TryAdmitPoolOverageAsync(IClientSessionHandle session, SubscriptionUsageEvent usageEvent, SubscriptionUsagePolicy policy,
            SubscriptionUsageAggregate pool, CancellationToken ct)
        {
            try
            {
                if (!(OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.UsageVisibility?.AppPayAsYouGoEnabled ?? true)) return false;
                var appId = usageEvent.PooledAppId;
                var account = await AppAccounts.Find(session, x => x.AppId == appId).FirstOrDefaultAsync(ct);
                var payg = account?.Payg;
                if (account == null || payg == null || !payg.Enabled || string.IsNullOrWhiteSpace(payg.BillingAvatarId) || payg.MonthlyCapUsd <= 0) return false;
                var rate = NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.AppOveragePrices.Load().Rate(account.PlanId);
                if (rate <= 0) return false;
                if (await OverageStatements.Find(session, x => x.UserId == AppStatementUser(appId) && x.PaymentFailed && x.State != "paid").AnyAsync(ct)) return false;
                var owner = await Subscriptions.Find(session, x => x.UserId == payg.BillingAvatarId).FirstOrDefaultAsync(ct);
                if (owner == null || owner.OveragePaymentBlocked || string.IsNullOrWhiteSpace(owner.StripeCustomerId) || string.IsNullOrWhiteSpace(owner.StripeSubscriptionId)) return false;
                var over = Math.Max(0, pool.MonthlyRequests + 1 - policy.MonthlyRequestLimit);
                if (over * rate > payg.MonthlyCapUsd) return false;
                // The monthly request limit is the only one this lifts; daily, token and budget limits still apply.
                var others = new SubscriptionUsagePolicy { MonthlyRequestLimit = -1, DailyCallLimit = policy.DailyCallLimit, DailyTokenLimit = policy.DailyTokenLimit, MonthlyBudgetUsd = policy.MonthlyBudgetUsd };
                SubscriptionUsagePolicyEvaluator.EnsureAuthorized(others, pool, usageEvent.MeterCategory == "ai.tokens" ? usageEvent.RequestedUnits : 0, usageEvent.ReservedCostUsd);
                SubscriptionLedgerMetrics.PoolThreshold.Add(1, new KeyValuePair<string, object>("app_id", appId),
                    new KeyValuePair<string, object>("threshold", "100"), new KeyValuePair<string, object>("limit", "overage-admitted"));
                return true;
            }
            catch (Exception) { return false; }
        }

        /// <summary>
        /// Prepares the monthly statement for every consenting app whose closed month went over the allowance. Idempotent: the statement id
        /// is derived from the app and month, so a repeat run changes nothing. The existing overage worker then collects it through Stripe.
        /// </summary>
        public async Task<int> PrepareAppStatementsAsync(DateTime now, CancellationToken token)
        {
            var apps = await AppAccounts.Find(x => x.Payg != null && x.Payg.ConsentAtUtc != null).Limit(200).ToListAsync(token);
            var created = 0;
            foreach (var app in apps)
            {
                var consent = app.Payg.ConsentAtUtc.Value;
                foreach (var month in AppOverageStatementMath.ClosedMonths(consent, now))
                {
                    try { if (await PrepareAppStatementAsync(app.AppId, month, now, token)) created++; }
                    catch (Exception ex) when (ex is not OperationCanceledException) { /* retried on the next cycle */ }
                }
            }
            return created;
        }

        public Task<bool> PrepareAppStatementAsync(string appId, string month, DateTime now, CancellationToken token) =>
            TransactAsync(async (session, ct) =>
            {
                var id = AppOverageStatementMath.StatementId(appId, month);
                if (await OverageStatements.Find(session, x => x.Id == id).AnyAsync(ct)) return false;
                var account = await AppAccounts.Find(session, x => x.AppId == appId).FirstOrDefaultAsync(ct);
                var payg = account?.Payg;
                if (account == null || payg == null || payg.ConsentAtUtc == null || string.IsNullOrWhiteSpace(payg.BillingAvatarId)) return false;
                var start = MonthStart(month);
                var pool = await LoadPoolAggregateAsync(session, appId, start, ct);
                var policy = _policyProvider.GetPolicy(account.PlanId, 0);
                var rate = NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.AppOveragePrices.Load().Rate(account.PlanId);
                var (over, charged, cents) = AppOverageStatementMath.Compute(pool.MonthlyRequests, policy.MonthlyRequestLimit, rate, payg.MonthlyCapUsd);
                if (cents <= 0) return false;
                var owner = await Subscriptions.Find(session, x => x.UserId == payg.BillingAvatarId).FirstOrDefaultAsync(ct)
                    ?? throw new KeyNotFoundException("The billing owner's subscriber account was not found.");
                if (string.IsNullOrWhiteSpace(owner.StripeCustomerId) || string.IsNullOrWhiteSpace(owner.StripeSubscriptionId))
                    throw new SubscriptionUsageConflictException("A verified Stripe billing account is required for the billing owner.");
                var statement = new PaidOverageStatement
                {
                    Id = id, UserId = AppStatementUser(appId), Month = month, Scope = OverageScope,
                    StripeCustomerId = owner.StripeCustomerId, StripeSubscriptionId = owner.StripeSubscriptionId,
                    RequestChargesUsd = charged, AiChargesUsd = 0, AmountMinorUsd = cents, PricingVersions = new[] { payg.PricingVersion ?? "" },
                    CreatedAtUtc = now, UpdatedAtUtc = now, NextAttemptAtUtc = now
                };
                await OverageStatements.InsertOneAsync(session, statement, cancellationToken: ct);
                await BillingAudit.InsertOneAsync(session, new BsonDocument
                {
                    { "_id", "app-overage-statement:" + id }, { "Kind", "app-overage-statement" }, { "UserId", "app:" + appId }, { "OccurredAtUtc", now },
                    { "PayloadJson", JsonSerializer.Serialize(new { appId, month, over, charged, cents, rate, cap = payg.MonthlyCapUsd, billingAvatarId = payg.BillingAvatarId, statementId = id }) }
                }, cancellationToken: ct);
                return true;
            }, token);

        public Task<IReadOnlyList<PaidOverageStatement>> GetAppStatementsAsync(string appId, CancellationToken token) =>
            GetStatementsAsync(AppStatementUser(appId), 24, token);
    }

    /// <summary>Prepares app pay-as-you-go statements once an hour; the existing overage worker collects them through Stripe.</summary>
    public sealed class AppOverageStatementWorker : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly MongoSubscriptionUsageRepository _repository;
        private readonly Microsoft.Extensions.Logging.ILogger<AppOverageStatementWorker> _logger;
        public AppOverageStatementWorker(MongoSubscriptionUsageRepository repository, Microsoft.Extensions.Logging.ILogger<AppOverageStatementWorker> logger) { _repository = repository; _logger = logger; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await _repository.PrepareAppStatementsAsync(DateTime.UtcNow, stoppingToken); }
                catch (Exception ex) when (ex is not OperationCanceledException) { _logger.LogError(ex, "APP_OVERAGE_STATEMENT_CYCLE_FAILED"); }
                try { await Task.Delay(TimeSpan.FromHours(1), stoppingToken); } catch (OperationCanceledException) { break; }
            }
        }
    }
}
