using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public partial class MongoStorageIdentityTests
{
    private static ConditionalHolonWriteRequest OutboxSourceWrite(CosmicStorageIdentity identity)
    {
        var row = new Holon { Id = Guid.NewGuid(), Name = "native-bound-outbox-source" };
        var enrollment = new HolonReplicationEnrollment("bound-outbox-source", new[] { "target" }, identity);
        return new(new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(), ProviderType.MongoDBOASIS,
            new[] { new HolonWriteCondition(row.Id, null) }, enrollment), ConditionalHolonOperation.Save,
            new[] { row }, saveChildren: false);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MongoBoundOutboxRejectsMissingOrReplacedSourceWithoutChangingRetainedLease(bool recreate)
    {
        await RunOwned(async (client, source, _) =>
        {
            var identity = await source.CaptureStorageIdentityAsync();
            Assert.False(identity.IsError, identity.Message);
            var request = OutboxSourceWrite(identity.Result);
            var saved = await source.ExecuteReplicatingHolonWriteAsync(request, request.Write.ReplicationEnrollment);
            Assert.False(saved.IsError, saved.Message + saved.Exception);
            var context = new MongoDbContext(Uri, First);
            var bound = new MongoHolonReplicationOutbox(context, identity.Result);
            var claimed = await bound.ClaimNextAsync("target", "original-worker", TimeSpan.FromMinutes(5));
            Assert.False(claimed.IsError, claimed.Message + claimed.Exception);
            Assert.NotNull(claimed.Result);
            var claim = claimed.Result;
            Assert.Equal(HolonDeliveryState.InFlight, (await bound.InspectAsync(claim.Sequence)).Result.State);
            Assert.False((await bound.RenewAsync(claim, TimeSpan.FromMinutes(1))).IsError);
            Assert.Single((await bound.ReadRouteHeadsAsync()).Result.Heads);
            Assert.NotNull((await bound.PeekNextAsync("target")).Result);

            var db = client.GetDatabase(First);
            var rows = db.GetCollection<BsonDocument>("CosmicHolonOutbox");
            var before = (await rows.Find(new BsonDocument("OperationId", request.Write.OperationId.ToString("D"))).SingleAsync()).ToBson();
            await db.DropCollectionAsync("Holon");
            if (recreate) await db.CreateCollectionAsync("Holon");
            // Context replacement cannot redirect a captured native outbox.
            context.MongoDB = client.GetDatabase(Ghost);
            void Rejected<T>(OASISResult<T> result)
            {
                Assert.True(result.IsError);
                Assert.Equal("COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH", result.ErrorCode);
                Assert.Equal(default(T), result.Result);
            }
            Rejected(await bound.InspectAsync(claim.Sequence));
            Rejected(await bound.PeekNextAsync("target"));
            Rejected(await bound.ReadRouteHeadsAsync());
            Rejected(await bound.ClaimNextAsync("target", "intruder", TimeSpan.FromMinutes(1)));
            Rejected(await bound.RenewAsync(claim, TimeSpan.FromMinutes(1)));
            Rejected(await bound.FinishAsync(claim, HolonDeliveryOutcome.Completed));
            Rejected(await bound.FinishWithRecoveryAsync(claim, HolonDeliveryOutcome.ReconciliationRequired,
                new HolonDeliveryRecoveryEvidence(true, new[] { "original-receipt" }), "UNKNOWN"));
            Assert.Equal(before, (await rows.Find(new BsonDocument("OperationId", request.Write.OperationId.ToString("D"))).SingleAsync()).ToBson());
            Assert.Equal(1L, await db.GetCollection<BsonDocument>("CosmicWriteReceipt").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            using var names = await db.ListCollectionNamesAsync();
            Assert.Equal(recreate, (await names.ToListAsync()).Contains("Holon"));
        });
    }

    [Fact]
    public async Task MongoBoundOutboxClaimPinsNamespaceThroughNativeLeaseCommit()
    {
        await RunOwned(async (client, source, _) =>
        {
            var identity = await source.CaptureStorageIdentityAsync();
            Assert.False(identity.IsError, identity.Message);
            var request = OutboxSourceWrite(identity.Result);
            var saved = await source.ExecuteReplicatingHolonWriteAsync(request, request.Write.ReplicationEnrollment);
            Assert.False(saved.IsError, saved.Message);
            using var released = new ManualResetEventSlim(false);
            var reached = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestId = 0;
            var timedOut = false;
            BsonDocument admission = null;
            var settings = MongoClientSettings.FromConnectionString(Uri);
            settings.ClusterConfigurator = builder =>
            {
                builder.Subscribe<CommandStartedEvent>(command =>
                {
                    if (command.CommandName == "find" && command.Command.Contains("collectionUUID"))
                    {
                        admission = command.Command.DeepClone().AsBsonDocument;
                        Volatile.Write(ref requestId, command.RequestId);
                    }
                });
                builder.Subscribe<CommandSucceededEvent>(command =>
                {
                    if (command.RequestId == Volatile.Read(ref requestId))
                    {
                        reached.TrySetResult(true);
                        timedOut = !released.Wait(TimeSpan.FromSeconds(20));
                    }
                });
            };
            var monitored = new MongoClient(settings);
            var context = new MongoDbContext(Uri, First) { MongoClient = monitored, MongoDB = monitored.GetDatabase(First) };
            var bound = new MongoHolonReplicationOutbox(context, identity.Result);
            var claiming = bound.ClaimNextAsync("target", "original-worker", TimeSpan.FromMinutes(5));
            try
            {
                await reached.Task.WaitAsync(TimeSpan.FromSeconds(15));
                Assert.NotNull(admission);
                Assert.True(admission["startTransaction"].AsBoolean);
                Assert.False(admission["autocommit"].AsBoolean);
                var refused = await Assert.ThrowsAsync<MongoExecutionTimeoutException>(() => client.GetDatabase(First)
                    .RunCommandAsync<BsonDocument>(new BsonDocument { { "drop", "Holon" }, { "maxTimeMS", 500 } }));
                Assert.Equal(50, refused.Code);
                context.MongoDB = client.GetDatabase(Ghost);
                context.MongoClient = client;
            }
            finally { released.Set(); }
            var claimed = await claiming;
            Assert.False(timedOut, "the test must explicitly release native admission");
            Assert.False(claimed.IsError, claimed.Message + claimed.Exception);
            Assert.NotNull(claimed.Result);
            Assert.Equal(request.Write.OperationId, claimed.Result.OperationId);
            Assert.False((await bound.RenewAsync(claimed.Result, TimeSpan.FromMinutes(1))).IsError);
            Assert.True((await bound.FinishAsync(claimed.Result, HolonDeliveryOutcome.Completed)).Result);
            Assert.Equal(HolonDeliveryState.Completed, (await bound.InspectAsync(claimed.Result.Sequence)).Result.State);
        });
    }
}
