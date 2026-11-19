using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed partial class MongoSubscriptionUsageRepository : IUsageSeriesSource
    {
        /// <summary>Daily buckets (period type "day") for the given ledger subjects between two UTC dates inclusive. Read-only.</summary>
        public async Task<List<UsageDay>> GetDailySeriesAsync(IReadOnlyCollection<string> subjects, DateTime from, DateTime to, CancellationToken cancellation)
        {
            if (subjects == null || subjects.Count == 0) return new List<UsageDay>();
            var first = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var last = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var filter = Builders<SubscriptionUsageBucket>.Filter.In(x => x.UserId, subjects)
                & Builders<SubscriptionUsageBucket>.Filter.Eq(x => x.PeriodType, "day")
                & Builders<SubscriptionUsageBucket>.Filter.Gte(x => x.Period, first)
                & Builders<SubscriptionUsageBucket>.Filter.Lte(x => x.Period, last);
            var rows = await _buckets.Find(filter).ToListAsync(cancellation).ConfigureAwait(false);
            return rows.Select(b => new UsageDay { Date = b.Period, Requests = b.Requests, Units = b.SettledUnits, CostUsd = b.SettledCostUsd }).ToList();
        }
    }
}
