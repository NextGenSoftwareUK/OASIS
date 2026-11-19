using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed partial class MongoSubscriptionUsageRepository
    {
        /// <summary>Upper bound on pool shards; reads always cover every possible shard so the shard count can be changed safely.</summary>
        public const int MaxPoolShards = 16;
        public const double PoolWarningFraction = 0.8;

        /// <summary>
        /// Number of documents a pooled app's usage is spread across (env OASIS_POOL_SHARDS, 1 to 16, default 1).
        /// Bucket writes are distributed across shards. Admission still writes the app authorization version
        /// in its transaction, so the aggregate limit remains exact across every shard layout.
        /// </summary>
        public static int PoolShardCount
        {
            get
            {
                var raw = Environment.GetEnvironmentVariable("OASIS_POOL_SHARDS");
                return int.TryParse(raw, out var n) ? Math.Clamp(n, 1, MaxPoolShards) : 1;
            }
        }

        /// <summary>Shard 0 is the original "app:{appId}" subject so existing data is unchanged; shards 1+ are "app:{appId}#k".</summary>
        public static string PoolShardSubject(string appId, int shard) => shard == 0 ? PoolSubject(appId) : PoolSubject(appId) + "#" + shard;

        /// <summary>Deterministic by operation id, so reserve, settle and corrections of one operation always hit the same shard.</summary>
        public static string PoolWriteSubject(string appId, string operationId, int shardCount)
        {
            if (shardCount <= 1) return PoolSubject(appId);
            uint h = 2166136261;
            foreach (var c in operationId ?? "") h = unchecked((h ^ c) * 16777619);
            return PoolShardSubject(appId, (int)(h % (uint)shardCount));
        }

        /// <summary>Sums every shard's month and day buckets in one read.</summary>
        private async Task<SubscriptionUsageAggregate> LoadPoolAggregateAsync(IClientSessionHandle session, string appId, DateTime time, CancellationToken ct)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            string Id(int shard, string type) => PoolShardSubject(appId, shard) + ":" + type + ":" + time.ToString(type == "month" ? "yyyy-MM" : "yyyy-MM-dd", culture);
            var ids = Enumerable.Range(0, MaxPoolShards).SelectMany(s => new[] { Id(s, "month"), Id(s, "day") }).ToList();
            var buckets = await _buckets.Find(session, x => ids.Contains(x.Id)).ToListAsync(ct);
            var month = buckets.Where(b => b.PeriodType == "month").ToList();
            var day = buckets.Where(b => b.PeriodType == "day").ToList();
            var all = month.Concat(day).ToList();
            return new SubscriptionUsageAggregate {
                Id = PoolSubject(appId) + ":month:" + time.ToString("yyyy-MM", culture), UserId = PoolSubject(appId),
                Month = time.ToString("yyyy-MM", culture), Day = time.ToString("yyyy-MM-dd", culture),
                MonthlyRequests = month.Sum(b => b.Requests), DailyCalls = day.Sum(b => b.Requests), DailyTokens = day.Sum(b => b.SettledUnits),
                ReservedUnits = day.Sum(b => b.ReservedUnits), ReservedCostUsd = month.Sum(b => b.ReservedCostUsd), SettledCostUsd = month.Sum(b => b.SettledCostUsd),
                ReservedOverageUsd = month.Sum(b => b.ReservedOverageUsd), SettledOverageUsd = month.Sum(b => b.SettledOverageUsd),
                UpdatedAtUtc = all.Count == 0 ? time : all.Max(b => b.UpdatedAtUtc)
            };
        }

        /// <summary>
        /// For a pooled app, also admit the request against the app's shared pool (app plan limits, karma 0).
        /// A refusal uses the normal code with an APP_ prefix so a client can tell "you are out" from "the app is out".
        /// Emits an 80% and a 100% threshold metric when this request is the one that crosses it.
        /// No-op for requests without a pooled app, so every other flow is unchanged.
        /// </summary>
        private async Task EnsurePoolAdmissionAsync(IClientSessionHandle session, SubscriptionUsageEvent usageEvent, int karma, CancellationToken ct)
        {
            if (usageEvent.PooledAppId == null) return;
            // Every pooled admission must share a write with other admissions and app mutations.
            // Snapshot reads across distinct buckets alone permit write skew and quota overshoot.
            // The transaction driver retries a conflicting writer against a fresh app/pool snapshot.
            var account = await AppAccounts.Find(session, x => x.AppId == usageEvent.PooledAppId).FirstOrDefaultAsync(ct)
                ?? throw new SubscriptionUsageLimitException("APP_ACCOUNT_NOT_FOUND", "The app account no longer exists.", 404);
            if (account.Status != "active")
                throw new SubscriptionUsageLimitException("APP_ACCOUNT_INACTIVE", "The app account is not active.", 403);
            if (!AppAccountRules.UsesPool(account))
                throw new SubscriptionUsageLimitException("APP_POOL_NOT_ACTIVE", "The app no longer uses an enabled shared quota.", 409);
            if (usageEvent.AppId != account.AppId || usageEvent.PooledAppPlanId != account.PlanId ||
                usageEvent.AppPlanId != AppAccountRules.PerUserPlan(account))
                throw new SubscriptionUsageLimitException("APP_ACCOUNT_CHANGED", "The app quota configuration changed; refresh the app authorization before reserving usage.", 409);
            await AppAccounts.UpdateOneAsync(session, x => x.AppId == account.AppId,
                Builders<AppAccountRecord>.Update.Set(x => x.AuthorizationVersion, checked(account.AuthorizationVersion + 1)),
                cancellationToken: ct);
            var pool = await LoadPoolAggregateAsync(session, usageEvent.PooledAppId, usageEvent.AuthorizedAtUtc, ct);
            var policy = _policyProvider.GetPolicy(usageEvent.PooledAppPlanId, 0);
            try
            {
                SubscriptionUsagePolicyEvaluator.EnsureAuthorized(policy, pool,
                    usageEvent.MeterCategory == "ai.tokens" ? usageEvent.RequestedUnits : 0, usageEvent.ReservedCostUsd);
            }
            catch (SubscriptionUsageLimitException ex)
            {
                SubscriptionLedgerMetrics.PoolThreshold.Add(1, new KeyValuePair<string, object>("app_id", usageEvent.PooledAppId),
                    new KeyValuePair<string, object>("threshold", "100"), new KeyValuePair<string, object>("limit", "denied"));
                throw new SubscriptionUsageLimitException("APP_" + ex.Code, "The app's shared quota is used up: " + ex.Message, ex.StatusCode);
            }
            EmitThresholdCrossings(usageEvent.PooledAppId, "monthly_requests", pool.MonthlyRequests, policy.MonthlyRequestLimit);
            EmitThresholdCrossings(usageEvent.PooledAppId, "daily_calls", pool.DailyCalls, policy.DailyCallLimit);
        }

        /// <summary>The thresholds (80, 100) that one more request moves the pool across.</summary>
        public static IReadOnlyList<int> ThresholdsCrossed(long before, long limit)
        {
            var crossed = new List<int>();
            if (limit <= 0) return crossed;
            long after = before + 1;
            if ((double)before / limit < PoolWarningFraction && (double)after / limit >= PoolWarningFraction) crossed.Add(80);
            if (before < limit && after >= limit) crossed.Add(100);
            return crossed;
        }

        private static void EmitThresholdCrossings(string appId, string limitName, long before, long limit)
        {
            foreach (var threshold in ThresholdsCrossed(before, limit))
                SubscriptionLedgerMetrics.PoolThreshold.Add(1, new KeyValuePair<string, object>("app_id", appId),
                    new KeyValuePair<string, object>("threshold", threshold.ToString()), new KeyValuePair<string, object>("limit", limitName));
        }

        /// <summary>Administrator view of an app pool: this month's and today's usage against the plan.</summary>
        public async Task<AppPoolUsage> GetAppPoolUsageAsync(string appId, CancellationToken cancellationToken = default)
        {
            AppAccountRules.ValidateAppId(appId);
            var account = await AppAccounts.Find(x => x.AppId == appId).FirstOrDefaultAsync(cancellationToken)
                ?? throw new KeyNotFoundException("The app account was not found.");
            using var session = await _client.StartSessionAsync(cancellationToken: cancellationToken);
            var now = _time.GetUtcNow().UtcDateTime;
            var aggregate = await LoadPoolAggregateAsync(session, appId, now, cancellationToken);
            var policy = _policyProvider.GetPolicy(account.PlanId, 0);
            return AppPoolUsage.Build(account.Redacted(), policy, aggregate, AppAccountRules.PooledQuotasEnabled);
        }

        /// <summary>The avatars using the most of a pooled app's quota this UTC month, busiest first (limit 1 to 100, default 10).</summary>
        public async Task<IReadOnlyList<AppPoolTopUser>> GetAppTopUsersAsync(string appId, int limit = 10, CancellationToken cancellationToken = default)
        {
            AppAccountRules.ValidateAppId(appId);
            limit = Math.Clamp(limit, 1, 100);
            if (!await AppAccounts.Find(x => x.AppId == appId).AnyAsync(cancellationToken))
                throw new KeyNotFoundException("The app account was not found.");
            var now = _time.GetUtcNow().UtcDateTime;
            var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1);
            // Audit deltas are the same net values applied to the pool buckets. Counting event projections
            // includes expired reservations and ignores corrections to settled units/cost.
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument { { "PooledAppId", appId },
                    { "AuthorizedAtUtc", new BsonDocument { { "$gte", start }, { "$lt", end } } } }),
                new BsonDocument("$lookup", new BsonDocument { { "from", _audit.CollectionNamespace.CollectionName },
                    { "localField", "OperationId" }, { "foreignField", "OperationId" }, { "as", "deltas" } }),
                new BsonDocument("$unwind", "$deltas"),
                new BsonDocument("$group", new BsonDocument { { "_id", "$UserId" },
                    { "Requests", new BsonDocument("$sum", "$deltas.RequestsDelta") },
                    { "Units", new BsonDocument("$sum", "$deltas.SettledUnitsDelta") },
                    { "CostUsd", new BsonDocument("$sum", "$deltas.SettledCostDeltaUsd") } }),
                new BsonDocument("$match", new BsonDocument("$or", new BsonArray {
                    new BsonDocument("Requests", new BsonDocument("$ne", 0)),
                    new BsonDocument("Units", new BsonDocument("$ne", 0)),
                    new BsonDocument("CostUsd", new BsonDocument("$ne", 0)) })),
                new BsonDocument("$sort", new BsonDocument { { "Requests", -1 }, { "_id", 1 } }),
                new BsonDocument("$limit", limit)
            };
            var rows = await _events.Aggregate<BsonDocument>(pipeline, cancellationToken: cancellationToken)
                .ToListAsync(cancellationToken);
            return rows.Select(row => new AppPoolTopUser { UserId = row["_id"].AsString,
                Requests = row["Requests"].ToInt64(), Units = row["Units"].ToInt64(), SettledCostUsd = row["CostUsd"].ToDecimal() }).ToList();
        }
    }

    public sealed class AppPoolTopUser
    {
        public string UserId { get; set; }
        public long Requests { get; set; }
        public long Units { get; set; }
        public decimal SettledCostUsd { get; set; }
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
        /// <summary>ok below 80% of any limit, warning from 80%, exhausted at 100%.</summary>
        public string Status { get; set; }

        public static AppPoolUsage Build(AppAccountRecord account, SubscriptionUsagePolicy limits, SubscriptionUsageAggregate usage, bool flagOn)
        {
            double? monthly = limits.MonthlyRequestLimit > 0 ? (double)usage.MonthlyRequests / limits.MonthlyRequestLimit : null;
            double? daily = limits.DailyCallLimit > 0 ? (double)usage.DailyCalls / limits.DailyCallLimit : null;
            var worst = Math.Max(monthly ?? 0, daily ?? 0);
            return new AppPoolUsage
            {
                Account = account, Limits = limits, Usage = usage,
                PoolActive = flagOn && account.PooledQuota && account.PlanId != "enterprise",
                MonthlyRequestsUsedFraction = monthly, DailyCallsUsedFraction = daily,
                Status = worst >= 1 ? "exhausted" : worst >= MongoSubscriptionUsageRepository.PoolWarningFraction ? "warning" : "ok"
            };
        }
    }
}
