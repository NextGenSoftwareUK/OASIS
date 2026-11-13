using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed class UsageAlert
    {
        /// <summary>user or app.</summary>
        public string Scope { get; set; }
        public string AppId { get; set; }
        public int ThresholdPercent { get; set; }
        public int UsedPercent { get; set; }
        public long Used { get; set; }
        public long Limit { get; set; }
        /// <summary>warning below 100%, exhausted at or over 100%.</summary>
        public string Level { get; set; }
        public string Message { get; set; }
    }

    /// <summary>Pure threshold maths for usage warnings (no storage, no sending).</summary>
    public static class UsageAlerts
    {
        public const int MaxThresholds = 4;

        /// <summary>Validates and orders thresholds: 1-4 distinct whole percents between 1 and 100, ascending. Null/empty falls back to the defaults.</summary>
        public static List<int> NormaliseThresholds(IEnumerable<int> requested, IEnumerable<int> defaults)
        {
            var list = (requested ?? Enumerable.Empty<int>()).ToList();
            if (list.Count == 0) list = (defaults ?? new[] { 80, 100 }).ToList();
            if (list.Any(p => p < 1 || p > 100)) throw new ArgumentException("Thresholds must be whole percents from 1 to 100.");
            list = list.Distinct().OrderBy(p => p).ToList();
            if (list.Count > MaxThresholds) throw new ArgumentException($"Choose at most {MaxThresholds} thresholds.");
            return list;
        }

        /// <summary>The highest threshold reached, or null when none is (or the limit is unlimited).</summary>
        public static UsageAlert Evaluate(string scope, string appId, long used, long limit, IEnumerable<int> thresholds)
        {
            if (limit <= 0 || used < 0) return null;
            var usedPercent = (int)Math.Min(1000, used * 100 / limit);
            var crossed = (thresholds ?? new[] { 80, 100 }).Where(t => usedPercent >= t).DefaultIfEmpty(0).Max();
            if (crossed == 0) return null;
            var level = usedPercent >= 100 ? "exhausted" : "warning";
            return new UsageAlert
            {
                Scope = scope, AppId = appId, ThresholdPercent = crossed, UsedPercent = usedPercent, Used = used, Limit = limit, Level = level,
                Message = level == "exhausted"
                    ? $"{(scope == "app" ? "The app pool" : "Your plan")} has used all {limit:N0} requests this month."
                    : $"{(scope == "app" ? "The app pool" : "Your plan")} has used {usedPercent}% ({used:N0} of {limit:N0}) of this month's requests."
            };
        }
    }
}
