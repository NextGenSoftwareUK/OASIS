using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private sealed record SubscriptionAdminGrantFixture(string ProtectedRecord, string OtherFinancialState, long AuditCount);

    private static async Task<SubscriptionAdminGrantFixture> PrepareSubscriptionAdminGrantAsync(IMongoDatabase db, string[] accounts)
    {
        // Explicit owned canonical-routing INPUT, never a Stripe-issued subscription.
        var records = db.GetCollection<BsonDocument>("subscription_records");
        var changed = await records.UpdateOneAsync(new BsonDocument("_id", accounts[1]),
            new BsonDocument("$set", new BsonDocument("StripeSubscriptionId", "owned-active-stripe-input")));
        Assert.Equal(1L, changed.MatchedCount);
        var protectedRecord = await records.Find(new BsonDocument("_id", accounts[1])).SingleAsync();
        return new(protectedRecord.ToJson(), await SubscriptionAdminFinancialStateAsync(db,
            "subscription_records", "subscription_billing_audit"), await db.GetCollection<BsonDocument>("subscription_billing_audit")
            .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    private static async Task VerifySubscriptionAdminGrantAsync(IMongoDatabase db, SubscriptionAdminGrantFixture before,
        string[] accounts, string target)
    {
        var records = db.GetCollection<BsonDocument>("subscription_records");
        Assert.Equal(3L, await records.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(before.ProtectedRecord, (await records.Find(new BsonDocument("_id", accounts[1])).SingleAsync()).ToJson());
        var audit = db.GetCollection<BsonDocument>("subscription_billing_audit");
        Assert.Equal(before.AuditCount + 2, await audit.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var grants = await audit.Find(new BsonDocument("Kind", "admin-plan-grant")).ToListAsync();
        Assert.Equal(2, grants.Count);
        foreach (var (user, plan, reason) in new[] {
            (target, "enterprise", "Owned browser enterprise grant"),
            (accounts[0], "gold", "Owned browser delayed grant") })
        {
            var row = await records.Find(new BsonDocument("_id", user)).SingleAsync();
            Assert.Equal(plan, row["PlanId"].AsString);
            Assert.Equal("active", row["Status"].AsString);
            Assert.False(row["PayAsYouGoEnabled"].AsBoolean);
            Assert.Equal(accounts[0], row["AdminGrantedBy"].AsString);
            Assert.Equal(reason, row["AdminGrantReason"].AsString);
            var entry = Assert.Single(grants.Where(value => value["UserId"].AsString == user));
            using var payload = JsonDocument.Parse(entry["PayloadJson"].AsString);
            Assert.Equal(accounts[0], payload.RootElement.GetProperty("Actor").GetString());
            Assert.Equal(plan, payload.RootElement.GetProperty("PlanId").GetString());
            Assert.Equal(reason, payload.RootElement.GetProperty("Reason").GetString());
            Assert.Equal(row["CurrentPeriodEnd"].ToUniversalTime(), payload.RootElement.GetProperty("ExpiresAtUtc").GetDateTime());
        }
        Assert.Equal(before.OtherFinancialState, await SubscriptionAdminFinancialStateAsync(db,
            "subscription_records", "subscription_billing_audit"));
        Console.WriteLine("Subscription admin grant native final state verified: two actual authorized grants and exactly two original-actor/reason/expiry audit records; active Stripe input unchanged; orders, usage, buckets, statements and adjustments unchanged. Ordinary/expired and conflict attempts did not grant. No actual Stripe payment or hosted Wizard acceptance.");
    }
}
