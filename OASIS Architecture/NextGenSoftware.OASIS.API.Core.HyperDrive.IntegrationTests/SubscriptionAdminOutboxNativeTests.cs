using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private sealed record SubscriptionAdminOutboxFixture(Dictionary<string, UsageOutboxOperation> Operations, string FinancialState);

    private static async Task<SubscriptionAdminOutboxFixture> PrepareSubscriptionAdminOutboxAsync(IMongoDatabase db, string actor)
    {
        var store = new MongoUsageOutboxStore(MongoUri, db.DatabaseNamespace.DatabaseName);
        var operations = new Dictionary<string, UsageOutboxOperation>();
        foreach (var service in new[] { "WEB5", "WEB6", "WEB7", "WEB8", "WEB9", "WEB10" })
            operations.Add(service, await CreateOwnedOutboxDeadletterAsync(store, service, actor));
        return new(operations, await SubscriptionAdminFinancialStateAsync(db));
    }

    private static async Task VerifySubscriptionAdminOutboxAsync(IMongoDatabase db, SubscriptionAdminOutboxFixture before, string actor)
    {
        var store = new MongoUsageOutboxStore(MongoUri, db.DatabaseNamespace.DatabaseName);
        Assert.Equal(6L, await db.GetCollection<BsonDocument>("subscription_usage_outbox_v1").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        foreach (var (service, original) in before.Operations)
        {
            var current = await store.GetAsync(original.Id, default);
            if (service != "WEB5")
            {
                Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(current));
                continue;
            }
            Assert.Equal("pending", current.State);
            var action = Assert.Single(current.AdministrativeActions);
            Assert.Equal(actor, action.Actor);
            Assert.Equal("requeue-deadletter", action.Kind);
            Assert.Equal("Owned browser requeue after service review", action.Reason);
            Assert.Equal(UsageCanonical.SettlementHash(original.Settlement), action.SettlementFingerprint);
            Assert.Equal(original.EvidenceRevision, current.EvidenceRevision);
            Assert.Equal(original.Fingerprint, current.Fingerprint);
            Assert.Equal(original.NoProviderExecutionReason, current.NoProviderExecutionReason);
            Assert.Equal(original.Attempts, current.Attempts);
            Assert.Equal(JsonSerializer.Serialize(original.Settlement), JsonSerializer.Serialize(current.Settlement));
            Assert.Empty(current.ProviderAttempts); Assert.Empty(current.Receipts);
        }
        Assert.Equal(before.FinancialState, await SubscriptionAdminFinancialStateAsync(db));
        Console.WriteLine("Subscription admin outbox native final state verified: six service-isolated real no-provider-execution deadletters in one shared override; only WEB5 pending with one original-admin action, exact settlement/fingerprint/evidence/attempt count conserved, five foreign operations and all financial records unchanged. No worker/provider execution or protected shared delivery touched.");
    }
}
