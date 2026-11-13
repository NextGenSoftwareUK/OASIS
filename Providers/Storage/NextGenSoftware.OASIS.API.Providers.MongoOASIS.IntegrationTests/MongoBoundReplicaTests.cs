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
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public partial class MongoStorageIdentityTests
{
    // Coherent source-protocol fixture: native receiver transaction proof only.
    private static CommittedHolonReplicaRequest BoundFixture()
    {
        var row = new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid(),
            Name = "native-bound-mongo", IsActive = true, CreatedByAvatarId = Guid.NewGuid() };
        CosmicConcurrency.SetProviderRevisionId(row, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        return new(Guid.NewGuid(), "bound-mongo-" + Guid.NewGuid().ToString("N"), ProviderType.SQLLiteDBOASIS,
            row.CreatedByAvatarId, ConditionalHolonOperation.Save, new[] { row }, saveChildren: false);
    }

    private static async Task<long[]> BoundCounts(IMongoDatabase database) => new[] {
        await database.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty),
        await database.GetCollection<BsonDocument>("CosmicWriteReceipt").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty),
        await database.GetCollection<BsonDocument>("CosmicReplicaHead").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MongoBoundReplicaRejectsNativeUuidReplacementEvenWithPriorReceipt(bool synchronous)
    {
        await RunOwned(async (client, receiver, _) =>
        {
            var db = client.GetDatabase(First);
            var captured = await receiver.CaptureStorageIdentityAsync();
            Assert.False(captured.IsError, captured.Message + captured.Exception);
            var expected = captured.Result;
            var request = BoundFixture();
            async Task<NextGenSoftware.OASIS.Common.OASISResult<System.Collections.Generic.IEnumerable<IHolon>>> Apply(CosmicStorageIdentity identity)
                => synchronous ? receiver.ApplyBoundCommittedHolonReplica(request, identity)
                    : await receiver.ApplyBoundCommittedHolonReplicaAsync(request, identity);
            foreach (var invalid in new[] {
                new CosmicStorageIdentity(expected.Authority, expected.EndpointFingerprint, Guid.NewGuid()),
                new CosmicStorageIdentity(expected.Authority, expected.EndpointFingerprint == new string('A', 64)
                    ? new string('B', 64) : new string('A', 64), expected.StoreId),
                new CosmicStorageIdentity(ProviderType.Neo4jOASIS, expected.EndpointFingerprint, expected.StoreId) })
            {
                Assert.Equal("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", (await Apply(invalid)).ErrorCode);
                Assert.Equal(new long[] { 0, 0, 0 }, await BoundCounts(db));
            }
            var first = await Apply(expected);
            Assert.False(first.IsError, first.Message + first.Exception);
            var replay = await Apply(expected);
            Assert.False(replay.IsError, replay.Message + replay.Exception);
            Assert.Equal(CosmicConcurrency.GetProviderRevisionId(first.Result.Single(), expected.Authority),
                CosmicConcurrency.GetProviderRevisionId(replay.Result.Single(), expected.Authority));
            Assert.Equal(new long[] { 1, 1, 1 }, await BoundCounts(db));

            // This is a real owned collection recreation, not a changed descriptor.
            // Keep receipt/head intact: they cannot authorize a replacement store.
            await db.DropCollectionAsync("Holon");
            Assert.Equal("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", (await Apply(expected)).ErrorCode);
            using (var names = await db.ListCollectionNamesAsync()) Assert.DoesNotContain("Holon", await names.ToListAsync());
            await db.CreateCollectionAsync("Holon");
            var replacement = await receiver.CaptureStorageIdentityAsync();
            Assert.False(replacement.IsError, replacement.Message + replacement.Exception);
            Assert.NotEqual(expected.StoreId, replacement.Result.StoreId);
            Assert.Equal("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", (await Apply(expected)).ErrorCode);
            Assert.Equal(new long[] { 0, 1, 1 }, await BoundCounts(db));
        });
    }

    [Fact]
    public async Task MongoBoundTransactionPinsNativeNamespaceAndAllHelperDatabaseHandles()
    {
        await RunOwned(async (client, receiver, _) =>
        {
            using var released = new ManualResetEventSlim(false);
            var reached = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestId = 0;
            var enabled = false;
            var timedOut = false;
            BsonDocument admission = null;
            var settings = MongoClientSettings.FromConnectionString(Uri);
            settings.ClusterConfigurator = builder => {
                builder.Subscribe<CommandStartedEvent>(command => {
                    if (enabled && command.CommandName == "find" && command.Command.Contains("collectionUUID"))
                    { admission = command.Command.DeepClone().AsBsonDocument; Volatile.Write(ref requestId, command.RequestId); }
                });
                builder.Subscribe<CommandSucceededEvent>(command => {
                    if (enabled && command.RequestId == Volatile.Read(ref requestId))
                    { reached.TrySetResult(true); timedOut = !released.Wait(TimeSpan.FromSeconds(20)); }
                });
            };
            var monitored = new MongoClient(settings);
            // Keep native address identical; instrument real driver commands only.
            var context = receiver.Database;
            context.MongoDB = monitored.GetDatabase(First);
            context.MongoClient = monitored;
            var captured = await receiver.CaptureStorageIdentityAsync();
            Assert.False(captured.IsError, captured.Message + captured.Exception);
            var mutation = BoundFixture();
            enabled = true;
            var applying = receiver.ApplyBoundCommittedHolonReplicaAsync(mutation, captured.Result);
            try
            {
                await reached.Task.WaitAsync(TimeSpan.FromSeconds(15));
                Assert.NotNull(admission);
                Assert.True(admission["startTransaction"].AsBoolean);
                Assert.False(admission["autocommit"].AsBoolean);
                Assert.True(admission.Contains("lsid") && admission.Contains("txnNumber"));
                // A separate connection cannot drop the native collection after
                // UUID admission while that same transaction still owns it.
                var blocked = await Assert.ThrowsAsync<MongoExecutionTimeoutException>(() => client.GetDatabase(First)
                    .RunCommandAsync<BsonDocument>(new BsonDocument { { "drop", "Holon" }, { "maxTimeMS", 500 } }));
                Assert.Equal(50, blocked.Code);
                // Repoint the mutable context during the exact awaited boundary.
                // Graph, fence, receipt and head must all keep their captured DB.
                context.MongoDB = client.GetDatabase(Second);
                context.MongoClient = client;
            }
            finally { released.Set(); }
            var result = await applying;
            Assert.False(timedOut, "The test must explicitly release the transaction.");
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(new long[] { 1, 1, 1 }, await BoundCounts(client.GetDatabase(First)));
            Assert.Equal(1L, await client.GetDatabase(First).GetCollection<BsonDocument>("CosmicOperationFence")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            using var databases = await client.ListDatabaseNamesAsync();
            Assert.DoesNotContain(Second, await databases.ToListAsync());
        });
    }
    [Fact]
    public async Task MongoPrimarySourceRecoveryRejectsActualCollectionReplacementWithOriginalReceipt()
    {
        await RunOwned(async (client,source,_)=>{
            var capture=await source.CaptureStorageIdentityAsync();Assert.False(capture.IsError);
            var row=new Holon{Id=Guid.NewGuid(),Name="bound-primary-source"};
            var enrollment=new HolonReplicationEnrollment("bound-primary-source",new[]{"target"},capture.Result);
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),ProviderType.MongoDBOASIS,
                new[]{new HolonWriteCondition(row.Id,null)},enrollment),ConditionalHolonOperation.Save,new[]{row},saveChildren:false);
            var root=new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while(root!=null && !System.IO.File.Exists(System.IO.Path.Combine(root.FullName,"Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1")))root=root.Parent;
            Assert.NotNull(root);
            var journal=System.IO.Path.Combine(root.FullName,"LocalState/COSMIC/OwnedRoutingCurrent/source-binding-physical.pending.json");
            Assert.False(System.IO.File.Exists(journal));
            await System.IO.File.WriteAllTextAsync(journal,System.Text.Json.JsonSerializer.Serialize(new{request.Write.OperationId,row.Id,identity=capture.Result}));
            var committed=await source.ExecuteReplicatingHolonWriteAsync(request,enrollment);
            Assert.False(committed.IsError,committed.Message+committed.Exception);
            source.Database=null; // Public descriptor is not the activated repository owner.
            var capturedOutbox=source.GetHolonReplicationOutbox();Assert.False(capturedOutbox.IsError,capturedOutbox.Message+capturedOutbox.Exception);
            var head=await capturedOutbox.Result.PeekNextAsync("target");Assert.False(head.IsError,head.Message+head.Exception);
            Assert.Equal(request.Write.OperationId,head.Result.OperationId);
            var original=await source.ReadConditionalHolonWriteOutcomeAsync(request);Assert.False(original.IsError,original.Message+original.Exception);
            Assert.Equal(ConditionalHolonReceiptState.Committed,original.Result.ReceiptState);
            var db=client.GetDatabase(First);
            await db.DropCollectionAsync("Holon");
            var missing=await source.ExecuteReplicatingHolonWriteAsync(request,enrollment);
            Assert.Equal("COSMIC_SOURCE_STORAGE_BINDING_MISMATCH",missing.ErrorCode);
            using(var names=await db.ListCollectionNamesAsync())Assert.DoesNotContain("Holon",await names.ToListAsync());
            await db.CreateCollectionAsync("Holon");
            var replacement=await source.CaptureStorageIdentityAsync();Assert.False(replacement.IsError);
            Assert.NotEqual(capture.Result.StoreId,replacement.Result.StoreId);
            Assert.Equal("COSMIC_SOURCE_STORAGE_BINDING_MISMATCH",(await source.ExecuteReplicatingHolonWriteAsync(request,enrollment)).ErrorCode);
            var rejected=await source.ReadConditionalHolonWriteOutcomeAsync(request);
            Assert.True(rejected.IsError);Assert.False(rejected.IsLoaded);Assert.Null(rejected.Result);
            Assert.Equal(0L,await db.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal(1L,await db.GetCollection<BsonDocument>("CosmicWriteReceipt").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal(1L,await db.GetCollection<BsonDocument>("CosmicHolonOutbox").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            System.IO.File.Delete(journal);
        });
    }

}
