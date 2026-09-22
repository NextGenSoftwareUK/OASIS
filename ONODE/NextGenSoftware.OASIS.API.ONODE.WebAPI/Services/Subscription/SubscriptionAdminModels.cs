using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed class SubscriptionAdminSnapshot
    {
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
        public long Customers { get; set; }
        public long ActiveSubscriptions { get; set; }
        public long UsageOperations { get; set; }
        public long FailedOperations { get; set; }
        public long SettledUnits { get; set; }
        public decimal UsageCostUsd { get; set; }
        public decimal RevenueUsd { get; set; }
        public decimal GrossMarginUsd => RevenueUsd - UsageCostUsd;
        public IReadOnlyDictionary<string, long> SubscriptionsByPlan { get; set; }
        public IReadOnlyDictionary<string, long> OperationsByProvider { get; set; }
        public IReadOnlyDictionary<string, long> OperationsByModel { get; set; }
        public IReadOnlyDictionary<string, long> OperationsByStatus { get; set; }
    }

    public sealed class SubscriptionAdminPage<T>
    {
        public IReadOnlyList<T> Items { get; set; }
        public int Limit { get; set; }
        public string NextCursor { get; set; }
    }
}
