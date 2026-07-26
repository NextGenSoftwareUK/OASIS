using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public class SubscriptionRecord
    {
        public string UserId { get; set; }
        public string StripeCustomerId { get; set; }
        public string StripeSubscriptionId { get; set; }
        public string PlanId { get; set; } = "free";
        public string Status { get; set; } = "active"; // active, cancelled, past_due, trialing, free
        public bool PayAsYouGoEnabled { get; set; }
        public DateTime? CurrentPeriodStart { get; set; }
        public DateTime? CurrentPeriodEnd { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class UsageRecord
    {
        public string UserId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public long RequestCount { get; set; }
        public long OverageCount { get; set; }
        public double StorageUsedGB { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class OrderRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public string PlanId { get; set; }
        public string StripeInvoiceId { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = "paid"; // paid, open, void, uncollectible
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
