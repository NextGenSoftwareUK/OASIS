using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    /// <summary>One day of usage read from the day ledger buckets.</summary>
    public sealed class UsageDay
    {
        /// <summary>yyyy-MM-dd (UTC).</summary>
        public string Date { get; set; }
        public long Requests { get; set; }
        /// <summary>Settled units (AI tokens) for the day.</summary>
        public long Units { get; set; }
        public decimal CostUsd { get; set; }
    }

    public sealed class UsageSeriesResult
    {
        public string Subject { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public List<UsageDay> Days { get; set; } = new List<UsageDay>();
        public long TotalRequests { get; set; }
        public long PeakDayRequests { get; set; }
        /// <summary>Requests allowed per day, when the plan has a daily limit (0 = none known).</summary>
        public long DailyLimit { get; set; }
        /// <summary>Requests allowed per month (0 = none known; negative plan values mean unlimited and are reported as 0).</summary>
        public long MonthlyLimit { get; set; }
        public long MonthRequests { get; set; }
        public long TodayRequests { get; set; }
        /// <summary>ok, warning (from the warning threshold) or exhausted (at or over the monthly limit); empty when no limit is known.</summary>
        public string Status { get; set; }
    }

    /// <summary>Reads daily usage buckets for one or more ledger subjects (a user id, or every shard of an app pool).</summary>
    public interface IUsageSeriesSource
    {
        Task<List<UsageDay>> GetDailySeriesAsync(IReadOnlyCollection<string> subjects, DateTime from, DateTime to, CancellationToken cancellation);
    }

    /// <summary>Pure series maths: range checks, zero-filling, shard summing and status. No storage, so it is fully unit-testable.</summary>
    public static class UsageSeries
    {
        public const int MaxDays = 92;
        public const int DefaultDays = 30;

        /// <summary>Parses optional yyyy-MM-dd bounds. Defaults to the last 30 days ending today; swaps a reversed range; rejects ranges over 92 days.</summary>
        public static (DateTime From, DateTime To) NormaliseRange(string from, string to, DateTime nowUtc)
        {
            var today = nowUtc.Date;
            var end = string.IsNullOrWhiteSpace(to) ? today : Parse(to, nameof(to));
            var start = string.IsNullOrWhiteSpace(from) ? end.AddDays(-(DefaultDays - 1)) : Parse(from, nameof(from));
            if (start > end) (start, end) = (end, start);
            if (end > today) end = today;
            if (start > end) start = end;
            if ((end - start).TotalDays + 1 > MaxDays) throw new ArgumentException($"A usage series covers at most {MaxDays} days.");
            return (start, end);
        }

        private static DateTime Parse(string value, string name)
        {
            if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
                throw new ArgumentException($"'{name}' must be a date like 2026-11-01.");
            return parsed.Date;
        }

        /// <summary>Every day from..to appears once, oldest first. Buckets for the same day (pool shards) are summed.</summary>
        public static List<UsageDay> Fill(IEnumerable<UsageDay> buckets, DateTime from, DateTime to)
        {
            var byDay = (buckets ?? Enumerable.Empty<UsageDay>()).Where(b => b != null && b.Date != null)
                .GroupBy(b => b.Date, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => new UsageDay { Date = g.Key, Requests = g.Sum(x => x.Requests), Units = g.Sum(x => x.Units), CostUsd = g.Sum(x => x.CostUsd) });
            var days = new List<UsageDay>();
            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                var key = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                days.Add(byDay.TryGetValue(key, out var found) ? found : new UsageDay { Date = key });
            }
            return days;
        }

        /// <summary>Status from the monthly figures: exhausted at or over the limit, warning from the threshold percentage, else ok.</summary>
        public static string Status(long monthRequests, long monthlyLimit, int warningPercent = 80)
        {
            if (monthlyLimit <= 0) return "";
            if (monthRequests >= monthlyLimit) return "exhausted";
            return monthRequests * 100 >= monthlyLimit * Math.Clamp(warningPercent, 1, 100) ? "warning" : "ok";
        }

        public static UsageSeriesResult Build(string subject, IEnumerable<UsageDay> buckets, DateTime from, DateTime to, long dailyLimit, long monthlyLimit, long monthRequests, DateTime nowUtc, int warningPercent = 80)
        {
            var days = Fill(buckets, from, to);
            var today = nowUtc.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return new UsageSeriesResult
            {
                Subject = subject,
                From = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                To = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Days = days,
                TotalRequests = days.Sum(d => d.Requests),
                PeakDayRequests = days.Count == 0 ? 0 : days.Max(d => d.Requests),
                DailyLimit = Math.Max(0, dailyLimit),
                MonthlyLimit = Math.Max(0, monthlyLimit),
                MonthRequests = monthRequests,
                TodayRequests = days.FirstOrDefault(d => d.Date == today)?.Requests ?? 0,
                Status = Status(monthRequests, Math.Max(0, monthlyLimit), warningPercent)
            };
        }
    }
}
