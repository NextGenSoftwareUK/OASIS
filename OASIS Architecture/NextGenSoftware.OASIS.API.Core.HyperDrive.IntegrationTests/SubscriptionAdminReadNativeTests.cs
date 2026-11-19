using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private sealed record SubscriptionAdminReadFixture(long Subscriptions, string FinancialState);

    private static async Task<SubscriptionAdminReadFixture> PrepareSubscriptionAdminReadsAsync(IMongoDatabase db)
    {
        // Owned existing-record INPUTS for real list pagination, not real customers or Stripe subscriptions.
        var rows = Enumerable.Range(0, 101).Select(_ => new BsonDocument {
            { "_id", Guid.NewGuid().ToString("D") }, { "PlanId", "free" }, { "Status", "active" },
            { "CurrentPeriodEnd", DateTime.UtcNow.AddYears(1) }
        }).ToArray();
        var records = db.GetCollection<BsonDocument>("subscription_records");
        await records.InsertManyAsync(rows);
        var count = await records.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        Assert.Equal(104L, count);
        Assert.Equal(2L, await db.GetCollection<BsonDocument>("subscription_orders").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        return new(count, await SubscriptionAdminFinancialStateAsync(db));
    }

    private static async Task<string> SubscriptionAdminFinancialStateAsync(IMongoDatabase db, params string[] excluded)
    {
        var rows = new List<string>();
        foreach (var name in new[] { "subscription_records", "subscription_orders", "subscription_usage_events",
            "subscription_usage_audit", "subscription_usage_buckets", "subscription_billing_audit",
            "subscription_overage_statements", "subscription_overage_adjustments" }.Except(excluded))
            rows.Add(name + ":" + new BsonArray(await db.GetCollection<BsonDocument>(name)
                .Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).ToJson());
        return string.Join("|", rows);
    }
}
