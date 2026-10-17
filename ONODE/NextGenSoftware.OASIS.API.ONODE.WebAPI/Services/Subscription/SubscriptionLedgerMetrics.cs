using System.Diagnostics.Metrics;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public static class SubscriptionLedgerMetrics
    {
        public static readonly Meter Meter = new("OASIS.WEB4.SubscriptionLedger", "1.0.0");
        public static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("subscription.ledger.operation.duration", "ms");
        public static readonly Counter<long> Denied = Meter.CreateCounter<long>("subscription.ledger.denied");
        public static readonly Counter<long> Conflicts = Meter.CreateCounter<long>("subscription.ledger.conflicts");
        public static readonly Counter<long> Duplicates = Meter.CreateCounter<long>("subscription.ledger.duplicates");
        public static readonly Counter<long> Failures = Meter.CreateCounter<long>("subscription.ledger.failures");
        public static readonly Counter<long> RaceRetries = Meter.CreateCounter<long>("subscription.ledger.unique_race_retries");
        public static readonly Counter<long> ReservationOverruns = Meter.CreateCounter<long>("subscription.ledger.reservation_overruns");
    }
}
