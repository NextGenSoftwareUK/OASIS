using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public async Task AdministrativeOutboxQueriesAndRequeuesBindPersistedServiceAcrossDatabaseLayouts()
    {
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != MongoUri)
            throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
        var mongo = new MongoClient(MongoUri);
        var defaults = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            [UsageOutboxConfiguration.ConnectionSetting] = MongoUri }).Build();
        Assert.Equal(MongoUri, UsageOutboxConfiguration.ConnectionString(defaults));
        var firstName = UsageOutboxConfiguration.DatabaseName(defaults, "WEB5");
        var secondName = UsageOutboxConfiguration.DatabaseName(defaults, "WEB6");
        Assert.Equal("oasis_web5_subscription_outbox", firstName);
        Assert.Equal("oasis_web6_subscription_outbox", secondName);
        var sharedName = "subscription_outbox_" + Guid.NewGuid().ToString("N");
        var owned = new List<string>();
        try
        {
            var existing = await (await mongo.ListDatabaseNamesAsync()).ToListAsync();
            foreach (var name in new[] { firstName, secondName, sharedName }) Assert.DoesNotContain(name, existing);
            // Exact default names are owned only after proving absence in this fresh, isolated CI server.
            owned.AddRange([firstName, secondName, sharedName]);
            var user = Guid.NewGuid().ToString("D");
            var first = new MongoUsageOutboxStore(MongoUri, firstName);
            var second = new MongoUsageOutboxStore(MongoUri, secondName);
            var separate5 = await CreateOwnedOutboxDeadletterAsync(first, "WEB5", user);
            var separate6 = await CreateOwnedOutboxDeadletterAsync(second, "WEB6", user);
            Assert.Equal(separate5.Id, Assert.Single(await first.GetAdministrativeSnapshotAsync("WEB5", "deadletter", 1, default)).Id);
            Assert.Empty(await first.GetAdministrativeSnapshotAsync("WEB6", null, 100, default));
            Assert.Equal(separate6.Id, Assert.Single(await second.GetAdministrativeSnapshotAsync("WEB6", "deadletter", 1, default)).Id);
            var sharedConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
                [UsageOutboxConfiguration.ConnectionSetting] = MongoUri,
                [UsageOutboxConfiguration.DatabaseSetting] = sharedName }).Build();
            Assert.Equal(sharedName, UsageOutboxConfiguration.DatabaseName(sharedConfig, "WEB5"));
            Assert.Equal(sharedName, UsageOutboxConfiguration.DatabaseName(sharedConfig, "WEB6"));
            var shared = new MongoUsageOutboxStore(UsageOutboxConfiguration.ConnectionString(sharedConfig), sharedName);
            var shared5 = await CreateOwnedOutboxDeadletterAsync(shared, "WEB5", user);
            var shared6 = await CreateOwnedOutboxDeadletterAsync(shared, "WEB6", user);
            Assert.Equal(shared5.Id, Assert.Single(await shared.GetAdministrativeSnapshotAsync("WEB5", "deadletter", 1, default)).Id);
            Assert.Equal(shared6.Id, Assert.Single(await shared.GetAdministrativeSnapshotAsync("WEB6", null, 1, default)).Id);
            var collection = mongo.GetDatabase(sharedName).GetCollection<BsonDocument>("subscription_usage_outbox_v1");
            async Task<string> State() => new BsonArray(await collection.Find(FilterDefinition<BsonDocument>.Empty)
                .Sort(new BsonDocument("_id", 1)).ToListAsync()).ToJson();
            var before = await State();
            var action = Guid.NewGuid().ToString("D");
            var denied = await Assert.ThrowsAsync<UsageProtocolException>(() => shared.RequeueDeadletterAsync(
                "WEB6", shared5.Id, action, user, "Owned service binding check", default));
            Assert.Equal("OUTBOX_SERVICE_CONFLICT", denied.Code);
            Assert.Equal(before, await State());
            await shared.RequeueDeadletterAsync("WEB5", shared5.Id, action, user, "Owned service binding check", default);
            await shared.RequeueDeadletterAsync("WEB5", shared5.Id, action, user, "Owned service binding check", default);
            var after = await shared.GetAsync(shared5.Id, default);
            Assert.Equal("pending", after.State);
            Assert.Single(after.AdministrativeActions);
            Assert.Equal(JsonSerializer.Serialize(shared5.Settlement), JsonSerializer.Serialize(after.Settlement));
            Assert.Equal(shared5.Fingerprint, after.Fingerprint);
            Assert.Equal(shared5.EvidenceRevision, after.EvidenceRevision);
            Assert.Empty(after.ProviderAttempts); Assert.Empty(after.Receipts);
            Assert.Empty(await shared.GetAdministrativeSnapshotAsync("WEB5", "deadletter", 100, default));
            Assert.Single(await shared.GetAdministrativeSnapshotAsync("WEB5", "pending", 100, default));
            Assert.Equal(JsonSerializer.Serialize(shared6), JsonSerializer.Serialize(await shared.GetAsync(shared6.Id, default)));
            var replayBefore = await State();
            denied = await Assert.ThrowsAsync<UsageProtocolException>(() => shared.RequeueDeadletterAsync(
                "WEB6", shared5.Id, action, user, "Owned service binding check", default));
            Assert.Equal("OUTBOX_SERVICE_CONFLICT", denied.Code);
            Assert.Equal(replayBefore, await State());
            Console.WriteLine("Administrative outbox native service binding verified: default separate and explicit shared databases, server-side service/state filtering before limit, wrong-service mutation and replay refusal without writes, one audited requeue and unchanged original no-provider-execution settlement/evidence/foreign operation.");
        }
        finally
        {
            foreach (var name in owned) await mongo.DropDatabaseAsync(name);
            var existing = await (await mongo.ListDatabaseNamesAsync()).ToListAsync();
            foreach (var name in owned) Assert.DoesNotContain(name, existing);
            Console.WriteLine("Owned administrative outbox default/shared database absence verified.");
        }
    }

    private static async Task<UsageOutboxOperation> CreateOwnedOutboxDeadletterAsync(MongoUsageOutboxStore store, string service, string user)
    {
        await store.InitializeAsync(default);
        var operation = new UsageOutboxOperation {
            Id = Guid.NewGuid().ToString("D"), UserId = user, Service = service,
            Fingerprint = new string('a', 64), Endpoint = "POST /owned-outbox",
            ExecutionOwner = Guid.NewGuid().ToString("D"), State = "executing",
            Policy = new UsageEndpointPolicy { ReservedUnits = 1, MeterCategory = "Requests", RequiresProviderMeasurement = true },
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow,
            LeaseUntilUtc = DateTime.UtcNow.AddMinutes(1), NextAttemptAtUtc = DateTime.UtcNow
        };
        Assert.True(await store.TryBeginAsync(operation, default));
        await store.RecordNoProviderExecutionAsync(operation.Id, operation.ExecutionOwner, "Owned rejection before any provider attempt", default);
        await store.RecordObservedOutcomeAsync(operation.Id, operation.ExecutionOwner, "failed", default);
        var captured = await store.GetAsync(operation.Id, default);
        await store.EnqueueAsync(operation.Id, operation.ExecutionOwner, UsageCanonical.Aggregate(captured, "failed"), captured.EvidenceRevision, default);
        var worker = Guid.NewGuid().ToString("D");
        Assert.NotNull(await store.ClaimOperationAsync(operation.Id, worker, DateTime.UtcNow, TimeSpan.FromMinutes(1), default));
        await store.RetryAsync(operation.Id, worker, DateTime.UtcNow, "Owned permanent delivery rejection", true, default);
        var deadletter = await store.GetAsync(operation.Id, default);
        Assert.Equal("deadletter", deadletter.State); Assert.NotNull(deadletter.Settlement);
        Assert.Equal(0m, deadletter.Settlement.ActualCostUsd);
        Assert.Empty(deadletter.ProviderAttempts); Assert.Empty(deadletter.Receipts);
        return deadletter;
    }
}
