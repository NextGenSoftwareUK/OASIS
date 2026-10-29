using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public partial class MongoStorageIdentityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BoundOriginalReceiptRequiresActualNativeSourceWithoutRewritingIntent(bool bindOriginal)
    {
        await RunOwned(async (client, source, _) =>
        {
            var capture = await source.CaptureStorageIdentityAsync();
            Assert.False(capture.IsError, capture.Message + capture.Exception);
            var expected = capture.Result;
            var input = new Holon { Id = Guid.NewGuid(), Name = "private-original-receipt" };
            var enrollment = new HolonReplicationEnrollment("owned-historical-receipt", new[] { "target" }, bindOriginal ? expected : null);
            var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(),
                ProviderType.MongoDBOASIS, new[] { new HolonWriteCondition(input.Id, null) }, enrollment),
                ConditionalHolonOperation.Save, new[] { input }, saveChildren: false);
            var absent = await source.ReadBoundConditionalHolonWriteOutcomeAsync(request, expected);
            Assert.False(absent.IsError, absent.Message + absent.Exception);
            Assert.Equal(ConditionalHolonReceiptState.Absent, absent.Result.ReceiptState);
            var committed = await source.ExecuteReplicatingHolonWriteAsync(request, enrollment);
            Assert.False(committed.IsError, committed.Message + committed.Exception);
            var database = client.GetDatabase(First);
            var receipts = database.GetCollection<BsonDocument>("CosmicWriteReceipt");
            var receiptBefore = (await receipts.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).ToJson();
            var outboxBefore = (await database.GetCollection<BsonDocument>("CosmicHolonOutbox")
                .Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).ToJson();
            source.Database = null; // The activated repository owns the native handle.
            var verified = await source.ReadBoundConditionalHolonWriteOutcomeAsync(request, expected);
            Assert.False(verified.IsError, verified.Message + verified.Exception);
            Assert.Equal(ConditionalHolonReceiptState.Committed, verified.Result.ReceiptState);
            Assert.False(verified.IsSaved);
            Assert.Same(bindOriginal ? expected : null, request.Write.ReplicationEnrollment.SourceStorageIdentity);
            Assert.DoesNotContain(input.Name, System.Text.Json.JsonSerializer.Serialize(verified.Result));
            var wrong = new CosmicStorageIdentity(expected.Authority, expected.EndpointFingerprint, Guid.NewGuid());
            var rejected = await source.ReadBoundConditionalHolonWriteOutcomeAsync(request, wrong);
            Assert.True(rejected.IsError); Assert.Null(rejected.Result);
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            Assert.Equal("COSMIC_OPERATION_STATUS_CANCELLED",
                (await source.ReadBoundConditionalHolonWriteOutcomeAsync(request, expected, cancelled.Token)).ErrorCode);
            Assert.Equal(receiptBefore, (await receipts.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).ToJson());
            Assert.Equal(outboxBefore, (await database.GetCollection<BsonDocument>("CosmicHolonOutbox")
                .Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).ToJson());

            // Preserve receipt bytes while replacing the actual native collection.
            // The former store's proof must no longer verify this operation.
            await database.DropCollectionAsync("Holon");
            var missing = await source.ReadBoundConditionalHolonWriteOutcomeAsync(request, expected);
            Assert.True(missing.IsError); Assert.Null(missing.Result);
            using (var names = await database.ListCollectionNamesAsync())
                Assert.DoesNotContain("Holon", await names.ToListAsync());
            await database.CreateCollectionAsync("Holon");
            var replacement = await source.CaptureStorageIdentityAsync(); Assert.False(replacement.IsError);
            Assert.NotEqual(expected.BindingFingerprint, replacement.Result.BindingFingerprint);
            var replaced = await source.ReadBoundConditionalHolonWriteOutcomeAsync(request, expected);
            Assert.True(replaced.IsError); Assert.Null(replaced.Result);
            Assert.Equal(receiptBefore, (await receipts.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync()).ToJson());
            Assert.Equal(0L, await database.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        });
    }
}
