using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using Outbox = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoHolonReplicationOutbox;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class MongoBaselineCompletionTests
{
    [Fact]
    public async Task ExactBaselineReceiptCompletesHeldHeadAndAllowsNextVersionWithoutReplay()
    {
        const string uri = "mongodb://127.0.0.1:27039/?replicaSet=cosmicLinuxDemoRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
        var state = Path.Combine(root, "LocalState/COSMIC/MongoBaselineCompletion");
        Directory.CreateDirectory(state);
        using var lease = File.Open(Path.Combine(state, "fixture.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var journal = Path.Combine(state, "fixture.json");
        if (File.Exists(journal))
            Assert.True(JsonDocument.Parse(await File.ReadAllTextAsync(journal)).RootElement.GetProperty("cleaned").GetBoolean(), "Preserve unresolved original fixture.");
        var databases = new[] { "cosmic_baseline_completion_source", "cosmic_baseline_completion_target" };
        var client = new MongoClient(uri);
        using (var names = await client.ListDatabaseNamesAsync())
            Assert.Empty((await names.ToListAsync()).Intersect(databases));
        var actor = Guid.NewGuid(); var id = Guid.NewGuid(); var initialization = Guid.NewGuid();
        var owner = Guid.NewGuid().ToString("D");
        void Record(bool cleaned)
        {
            using (var file = new FileStream(journal + ".tmp", FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { JsonSerializer.Serialize(file, new { owner, databases, actor, id, initialization, cleaned }); file.Flush(true); }
            File.Move(journal + ".tmp", journal, true);
        }
        Record(false);
        foreach (var name in databases)
            await client.GetDatabase(name).GetCollection<BsonDocument>("CosmicOwnedFixture")
                .InsertOneAsync(new BsonDocument { { "_id", "baseline-completion-v1" }, { "owner", owner } });
        var source = new Provider(uri, databases[0]); var target = new Provider(uri, databases[1]);
        var complete = false;
        try
        {
            Assert.False((await source.ActivateProviderAsync()).IsError);
            Assert.False((await target.ActivateProviderAsync()).IsError);
            IHolon current = new Holon { Id = id, Name = "late enrollment" };
            async Task Save(int version)
            {
                var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor,
                    ProviderType.MongoDBOASIS, new[] { new HolonWriteCondition(id,
                        CosmicConcurrency.GetProviderRevisionId(current, ProviderType.MongoDBOASIS)) }),
                    ConditionalHolonOperation.Save, new[] { current }, saveChildren: false);
                var saved = version < 4 ? await source.ExecuteConditionalHolonWriteAsync(request)
                    : await source.ExecuteReplicatingHolonWriteAsync(request, new HolonReplicationEnrollment("source", new[] { "target" }));
                Assert.False(saved.IsError, saved.Message + saved.Exception);
                current = Assert.Single(saved.Result); Assert.Equal(version, current.Version);
            }
            for (var version = 1; version <= 4; version++) await Save(version);
            var outbox = Assert.IsType<Outbox>(source.GetHolonReplicationOutbox().Result);
            var claim = (await outbox.ClaimNextAsync("target", "fixture", TimeSpan.FromSeconds(30))).Result;
            var original = CommittedHolonReplicaPayload.Read(claim.PayloadJson);
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT", (await target.ApplyCommittedHolonReplicaAsync(original)).ErrorCode);
            Assert.True((await outbox.FinishWithRecoveryAsync(claim, HolonDeliveryOutcome.ReconciliationRequired,
                new HolonDeliveryRecoveryEvidence(false, Array.Empty<string>()), "COSMIC_REPLICA_LINEAGE_CONFLICT", "retained original failure")).Result);
            var preparation = new HolonReplicaInitializationPreparation("fixture", Guid.NewGuid(), "source", databases[0],
                ProviderType.MongoDBOASIS, outbox, "target", databases[1], ProviderType.MongoDBOASIS, new Policy(actor, id));
            var prepared = await preparation.PrepareAsync(actor, claim.Sequence, initialization);
            Assert.False(prepared.IsError, prepared.Message + prepared.Exception);
            var intent = prepared.Result;
            var baseline = new CommittedHolonReplicaRequest(initialization, original.SourceId, original.SourceAuthority,
                original.ActorId, original.Operation, original.Roots, original.SaveChildren, original.Recursive, original.MaxChildDepth);
            var absent = (await target.ReadReplicaInitializationOutcomeAsync(baseline)).Result;
            Assert.True((await outbox.CompleteInitializedBaselineAsync(intent, absent)).IsError);
            Assert.Equal(HolonDeliveryState.ReconciliationRequired, (await outbox.InspectAsync(claim.Sequence)).Result.State);
            Assert.False((await target.InitializeCommittedHolonReplicaAsync(baseline)).IsError);
            var receipt = (await target.ReadReplicaInitializationOutcomeAsync(baseline)).Result;
            var rows = client.GetDatabase(databases[0]).GetCollection<BsonDocument>("CosmicHolonOutbox");
            // A changed delivery attempt invalidates the reviewed source intent.
            // Alter only this owned fixture, then restore its exact observed count.
            var selected = new BsonDocument("Sequence", claim.Sequence);
            await rows.UpdateOneAsync(selected, new BsonDocument("$set", new BsonDocument("AttemptCount", intent.AttemptCount + 1)));
            Assert.True((await outbox.CompleteInitializedBaselineAsync(intent, receipt)).IsError);
            Assert.Equal(HolonDeliveryState.ReconciliationRequired, (await outbox.InspectAsync(claim.Sequence)).Result.State);
            await rows.UpdateOneAsync(selected, new BsonDocument("$set", new BsonDocument("AttemptCount", intent.AttemptCount)));
            Assert.True((await outbox.CompleteInitializedBaselineAsync(intent, receipt)).Result);
            var beforeReplay = await rows.Find(new BsonDocument("Sequence", claim.Sequence)).SingleAsync();
            Assert.Equal("retained original failure", beforeReplay["LastErrorMessage"].AsString);
            Assert.Equal(claim.OperationId.ToString("D"), beforeReplay["OperationId"].AsString);
            Assert.True((await outbox.CompleteInitializedBaselineAsync(intent, receipt)).Result);
            Assert.Equal(beforeReplay.ToBson(), (await rows.Find(new BsonDocument("Sequence", claim.Sequence)).SingleAsync()).ToBson());
            var wrongReceipt = new HolonReplicaInitializationOutcome(initialization, ProviderType.MongoDBOASIS,
                ConditionalHolonReceiptState.Committed, receipt.BaselineFingerprint, new string('0', 64));
            Assert.True((await outbox.CompleteInitializedBaselineAsync(intent, wrongReceipt)).IsError);

            // New work passes through ordinary ordered delivery. Initialization never
            // rewinds version four or fabricates a retry of its original operation.
            current.Name = "next version"; await Save(5);
            var next = (await outbox.ClaimNextAsync("target", "fixture", TimeSpan.FromSeconds(30))).Result;
            Assert.True(next.Sequence > claim.Sequence);
            Assert.False((await target.ApplyCommittedHolonReplicaAsync(CommittedHolonReplicaPayload.Read(next.PayloadJson))).IsError);
            Assert.True((await outbox.FinishAsync(next, HolonDeliveryOutcome.Completed)).Result);
            Assert.Equal(5, (await target.LoadHolonAsync(id, loadChildren: false)).Result.Version);
            Assert.Null((await outbox.PeekNextAsync("target")).Result);
            complete = true;
        }
        finally
        {
            await source.DeActivateProviderAsync(); await target.DeActivateProviderAsync();
            if (complete)
            {
                foreach (var name in databases)
                {
                    var marker = await client.GetDatabase(name).GetCollection<BsonDocument>("CosmicOwnedFixture")
                        .Find(new BsonDocument("_id", "baseline-completion-v1")).SingleAsync();
                    Assert.Equal(owner, marker["owner"].AsString); await client.DropDatabaseAsync(name);
                }
                using var names = await client.ListDatabaseNamesAsync();
                Assert.Empty((await names.ToListAsync()).Intersect(databases)); Record(true);
            }
        }
    }

    private sealed class Policy(Guid actor, Guid root) : IHolonReplicaInitializationPolicy
    {
        public string BindingIdentity => "owned-baseline-completion-v1";
        public Task<OASISResult<bool>> AuthorizeAsync(HolonReplicaInitializationAuthorizationContext context, CancellationToken token)
            => Task.FromResult(new OASISResult<bool>(context.OperatorId == actor &&
                (context.Mutation == null || context.Mutation.ActorId == actor && context.Mutation.Roots.Single().Id == root)));
    }
}
